using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection; // <- hierher, nicht ans Dateiende!

using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Collections;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.FolderCollections.GUI
{
    /// <summary>
    /// Scheduled Task: erzeugt/aktualisiert Collections (BoxSets) je Eltern-Ordner.
    /// </summary>
    public sealed class FolderCollectionsTask : IScheduledTask
    {
        private readonly ILibraryManager _library;
        private readonly ICollectionManager _collections;
        private readonly ILogger<FolderCollectionsTask> _logger;

        public FolderCollectionsTask(
            ILibraryManager library,
            ICollectionManager collections,
            ILogger<FolderCollectionsTask> logger)
        {
            _library = library;
            _collections = collections;
            _logger = logger;
        }

        public string Key => "FolderCollectionsTask";
        public string Name => "Folder Collections (per directory)";
        public string Description => "Create / Update BoxSets based on parent folders.";
        public string Category => "Library";

        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
        {
            // täglich 04:00
            return new[]
            {
                new TaskTriggerInfo
                {
                    Type = TaskTriggerInfo.TriggerDaily,
                    TimeOfDayTicks = TimeSpan.FromHours(4).Ticks
                }
            };
        }

        // Signatur für neuere ABIs: Progress zuerst, dann CancellationToken
        public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
        {
            var cfg = Plugin.Instance?.Configuration ?? new PluginConfiguration();

            _logger.LogInformation(
                "[FolderCollections] Start. IncludeMovies={Movies}, IncludeSeries={Series}",
                cfg.IncludeMovies, cfg.IncludeSeries);

            // Ignore-Regexe vorbereiten
            var ignore = (cfg.IgnorePatterns ?? new List<string>())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => new Regex(s, RegexOptions.IgnoreCase | RegexOptions.Compiled))
                .ToList();

            // Enum-Itemtypen
            var kinds = new List<BaseItemKind>();
            if (cfg.IncludeMovies) kinds.Add(BaseItemKind.Movie);
            if (cfg.IncludeSeries) kinds.Add(BaseItemKind.Series);

            // Items per InternalItemsQuery einsammeln (rekursiv)
            var query = new InternalItemsQuery
            {
                IncludeItemTypes = kinds.ToArray(),
                Recursive = true
            };
            var allItems = _library.GetItemList(query).ToList();

            // Gruppieren nach Eltern-Ordner (mit Präfix-Whitelist & Ignore)
            var groups = new Dictionary<string, List<BaseItem>>(StringComparer.OrdinalIgnoreCase);

            foreach (var item in allItems)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var path = item.Path;
                if (string.IsNullOrWhiteSpace(path))
                    continue;

                // Präfix-Whitelist
                if (cfg.LibraryPathPrefixes != null && cfg.LibraryPathPrefixes.Count > 0)
                {
                    var ok = false;
                    foreach (var pref in cfg.LibraryPathPrefixes)
                    {
                        if (!string.IsNullOrWhiteSpace(pref) &&
                            path.StartsWith(pref, StringComparison.OrdinalIgnoreCase))
                        {
                            ok = true;
                            break;
                        }
                    }
                    if (!ok) continue;
                }

                // Ignore-Patterns
                var ignored = false;
                foreach (var rx in ignore)
                {
                    if (rx.IsMatch(path)) { ignored = true; break; }
                }
                if (ignored) continue;

                var trimmed = path.TrimEnd(
                    System.IO.Path.DirectorySeparatorChar,
                    System.IO.Path.AltDirectorySeparatorChar);

                var parent = System.IO.Path.GetDirectoryName(trimmed);
                if (string.IsNullOrEmpty(parent))
                    continue;

                if (!groups.TryGetValue(parent, out var list))
                {
                    list = new List<BaseItem>();
                    groups[parent] = list;
                }
                list.Add(item);
            }

            // Mindestanzahl je Ordner
            var minItems = Math.Max(1, cfg.MinimumItemsPerFolder);
            var filtered = new Dictionary<string, List<BaseItem>>(StringComparer.OrdinalIgnoreCase);
            foreach (var kv in groups)
            {
                if (kv.Value.Count >= minItems)
                    filtered[kv.Key] = kv.Value;
            }

            // Collections erzeugen/aktualisieren (ABI-sicher via Reflection)
            var done = 0;
            var total = filtered.Count == 0 ? 1 : filtered.Count;

            foreach (var kv in filtered)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var folder = kv.Key;
                var items = kv.Value;

                var baseName = cfg.UseBasenameForCollection
                    ? System.IO.Path.GetFileName(folder.TrimEnd(
                        System.IO.Path.DirectorySeparatorChar,
                        System.IO.Path.AltDirectorySeparatorChar))
                    : folder;

                var name = (cfg.NamePrefix ?? string.Empty) + baseName + (cfg.NameSuffix ?? string.Empty);
                var ids = items.Select(i => i.Id).ToArray();

                try
                {
                    await CollectionCompat.UpsertAsync(_collections, _library, name, ids, cancellationToken)
                        .ConfigureAwait(false);

                    _logger.LogInformation(
                        "[FolderCollections] Upsert '{Name}' with {Count} items",
                        name, ids.Length);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[FolderCollections] Failed for '{Name}'", name);
                }

                done++;
                progress.Report(done * 100.0 / total);
            }

            _logger.LogInformation("[FolderCollections] Finished. Collections processed: {Count}", done);
        }
    }

    /// <summary>
    /// ABI-kompatibler Helfer für ICollectionManager – probiert mehrere Methoden-Signaturen.
    /// </summary>
    static class CollectionCompat
    {
        /// <summary>
        /// Legt eine Collection an (falls nötig) und setzt exakt die Items – je nach vorhandener API.
        /// Reihenfolge:
        /// 1) AddToCollectionAsync(string, Guid[], CancellationToken)
        /// 2) EnsureCollectionAsync(string, CancellationToken) + SetCollectionItemsAsync(Guid, Guid[], CancellationToken)
        /// 3) CreateCollection(string, Guid[]) + SetCollectionItems(Guid, Guid[])
        /// 4) AddToCollection(string, Guid[])
        /// </summary>
        public static async Task UpsertAsync(
            ICollectionManager collections,
            ILibraryManager library,
            string name,
            Guid[] itemIds,
            CancellationToken ct)
        {
            // 1) AddToCollectionAsync
            var m = Find(collections, "AddToCollectionAsync",
                         new[] { typeof(string), typeof(Guid[]), typeof(CancellationToken) });
            if (m != null)
            {
                await (Task)m.Invoke(collections, new object[] { name, itemIds, ct })!;
                return;
            }

            // 2) EnsureCollectionAsync + SetCollectionItemsAsync
            var ensure = Find(collections, "EnsureCollectionAsync",
                              new[] { typeof(string), typeof(CancellationToken) });
            var setAsync = Find(collections, "SetCollectionItemsAsync",
                                new[] { typeof(Guid), typeof(Guid[]), typeof(CancellationToken) });
            if (ensure != null && setAsync != null)
            {
                var t = (Task)ensure.Invoke(collections, new object[] { name, ct })!;
                await t.ConfigureAwait(false);

                var boxSet = GetTaskResult(t); // Task<T> mit Property "Id" (Guid)
                var idProp = boxSet.GetType().GetProperty("Id");
                var colId = (Guid)idProp!.GetValue(boxSet)!;

                var t2 = (Task)setAsync.Invoke(collections, new object[] { colId, itemIds, ct })!;
                await t2.ConfigureAwait(false);
                return;
            }

            // 3) CreateCollection + SetCollectionItems (sync)
            var create = Find(collections, "CreateCollection", new[] { typeof(string), typeof(Guid[]) });
            var set = Find(collections, "SetCollectionItems", new[] { typeof(Guid), typeof(Guid[]) });
            if (create != null && set != null)
            {
                var boxSet = create.Invoke(collections, new object[] { name, itemIds })!;
                var idProp = boxSet.GetType().GetProperty("Id");
                var colId = (Guid)idProp!.GetValue(boxSet)!;
                set.Invoke(collections, new object[] { colId, itemIds });
                return;
            }

            // 4) AddToCollection (sync)
            var add = Find(collections, "AddToCollection", new[] { typeof(string), typeof(Guid[]) });
            if (add != null)
            {
                add.Invoke(collections, new object[] { name, itemIds });
                return;
            }

            throw new MissingMethodException("Keine kompatible ICollectionManager-Methode gefunden.");
        }

        private static MethodInfo? Find(object obj, string name, Type[] signature)
        {
            return obj.GetType()
                      .GetMethods(BindingFlags.Instance | BindingFlags.Public)
                      .FirstOrDefault(m => string.Equals(m.Name, name, StringComparison.Ordinal) &&
                                           m.GetParameters().Select(p => p.ParameterType).SequenceEqual(signature));
        }

        private static dynamic GetTaskResult(Task t)
        {
            var tp = t.GetType();
            if (tp.IsGenericType) // Task<T>
            {
                return tp.GetProperty("Result")!.GetValue(t)!;
            }
            throw new InvalidOperationException("Task hat kein Result.");
        }
    }
}
