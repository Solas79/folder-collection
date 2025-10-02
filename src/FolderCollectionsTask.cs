using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using FolderCollections;
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

        // Pflicht für neuere Jellyfin-ABIs
        public string Key => "FolderCollectionsTask";

        public string Name => "Folder Collections (per directory)";
        public string Description => "Create / Update BoxSets based on parent folders.";
        public string Category => "Library";

        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
        {
            // Uhrzeit aus der Plugin-Konfiguration lesen (Fallback 04:00)
            var cfg = Plugin.Instance?.Configuration ?? new PluginConfiguration();
            var hour = Math.Clamp(cfg.ScanHour, 0, 23);
            var minute = Math.Clamp(cfg.ScanMinute, 0, 59);

            return new[]
            {
                new TaskTriggerInfo
                {
                    Type = TaskTriggerInfo.TriggerDaily,
                    TimeOfDayTicks = TimeSpan.FromHours(hour).Add(TimeSpan.FromMinutes(minute)).Ticks
                }
            };
        }

        // Signatur für aktuelle ABIs: Progress zuerst, dann CancellationToken
        public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
        {
            var cfg = Plugin.Instance?.Configuration ?? new PluginConfiguration();

            _logger.LogInformation("[FolderCollections] Start. IncludeMovies={Movies}, IncludeSeries={Series}",
                cfg.IncludeMovies, cfg.IncludeSeries);

            // --- Ignore-Regexe ---
            var ignore = (cfg.IgnorePatterns ?? new List<string>())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => new Regex(s, RegexOptions.IgnoreCase | RegexOptions.Compiled))
                .ToList();

            // --- Typen zusammenstellen ---
            var kinds = new List<BaseItemKind>();
            if (cfg.IncludeMovies) kinds.Add(BaseItemKind.Movie);
            if (cfg.IncludeSeries) kinds.Add(BaseItemKind.Series);

            if (kinds.Count == 0)
            {
                _logger.LogWarning("[FolderCollections] No item types enabled (Movies/Series). Aborting.");
                return;
            }

            // --- Items via InternalItemsQuery einsammeln (rekursiv) ---
            var query = new InternalItemsQuery
            {
                IncludeItemTypes = kinds.ToArray(),
                Recursive = true
            };

            var allItems = _library.GetItemList(query)
                // Doppelt filtern zur Sicherheit
                .Where(i =>
                    (cfg.IncludeMovies && string.Equals(i.GetType().Name, "Movie", StringComparison.Ordinal)) ||
                    (cfg.IncludeSeries && string.Equals(i.GetType().Name, "Series", StringComparison.Ordinal)))
                .ToList();

            // --- Gruppieren nach Eltern-Ordner (mit Präfix-Whitelist & Ignore) ---
            var groups = new Dictionary<string, List<BaseItem>>(StringComparer.OrdinalIgnoreCase);

            foreach (var item in allItems)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var path = item.Path;
                if (string.IsNullOrWhiteSpace(path))
                    continue;

                // Präfix-Whitelist (falls gesetzt)
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

            // --- Mindestanzahl je Ordner ---
            var minItems = Math.Max(1, cfg.MinimumItemsPerFolder);
            var filtered = new Dictionary<string, List<BaseItem>>(StringComparer.OrdinalIgnoreCase);
            foreach (var kv in groups)
            {
                if (kv.Value.Count >= minItems)
                    filtered[kv.Key] = kv.Value;
            }

            // --- Collections erzeugen/aktualisieren (ABI-sicher via Reflection) ---
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
                    await CollectionCompat.UpsertAsync(_collections, name, ids, cancellationToken)
                        .ConfigureAwait(false);

                    _logger.LogInformation("[FolderCollections] Upsert '{Name}' with {Count} items", name, ids.Length);
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
    /// ABI-kompatibler Helfer für ICollectionManager – probiert diverse Methoden- und Parametervarianten.
    /// </summary>
    static class CollectionCompat
    {
        public static async Task UpsertAsync(
            ICollectionManager collections,
            string name,
            Guid[] itemIds,
            CancellationToken ct)
        {
            // Wir versuchen mehrere API-Varianten und mehrere Parameterformen (Guid[], List<Guid>, IEnumerable<Guid>, IReadOnlyCollection<Guid>)
            var list = new List<Guid>(itemIds);
            IEnumerable<Guid> ienum = list;
            IReadOnlyCollection<Guid> iread = list;

            // 1) AddToCollectionAsync(name, items, ct)
            if (await TryInvokeAsync(collections, "AddToCollectionAsync", new object[] { name, itemIds, ct }) ||
                await TryInvokeAsync(collections, "AddToCollectionAsync", new object[] { name, list,   ct }) ||
                await TryInvokeAsync(collections, "AddToCollectionAsync", new object[] { name, ienum,  ct }) ||
                await TryInvokeAsync(collections, "AddToCollectionAsync", new object[] { name, iread,  ct }))
            {
                return;
            }

            // 2) EnsureCollectionAsync(name, ct) + SetCollectionItemsAsync(id, items, ct)
            var ensure = FindMethod(collections, "EnsureCollectionAsync",  new[] { typeof(string), typeof(CancellationToken) });
            var setA   = FindMethodFlexible(collections, "SetCollectionItemsAsync", new[] { typeof(Guid), typeof(IEnumerable<Guid>), typeof(CancellationToken) });

            if (ensure != null && setA != null)
            {
                var t = (Task)ensure.Invoke(collections, new object[] { name, ct })!;
                await t.ConfigureAwait(false);

                var boxSet = GetTaskResult(t); // Task<T> mit Property Id
                var idProp = boxSet.GetType().GetProperty("Id");
                var colId  = (Guid)idProp!.GetValue(boxSet)!;

                if (await TryInvokeAsync(collections, "SetCollectionItemsAsync", new object[] { colId, itemIds, ct }) ||
                    await TryInvokeAsync(collections, "SetCollectionItemsAsync", new object[] { colId, list,   ct }) ||
                    await TryInvokeAsync(collections, "SetCollectionItemsAsync", new object[] { colId, ienum,  ct }) ||
                    await TryInvokeAsync(collections, "SetCollectionItemsAsync", new object[] { colId, iread,  ct }))
                {
                    return;
                }
            }

            // 3) CreateCollection(name, items)  (sync)
            if (TryInvoke(collections, "CreateCollection", out _, new object[] { name, itemIds }) ||
                TryInvoke(collections, "CreateCollection", out _, new object[] { name, list })   ||
                TryInvoke(collections, "CreateCollection", out _, new object[] { name, ienum })  ||
                TryInvoke(collections, "CreateCollection", out _, new object[] { name, iread }))
            {
                return;
            }

            // 4) AddToCollection(name, items)  (sync)
            if (TryInvoke(collections, "AddToCollection", out _, new object[] { name, itemIds }) ||
                TryInvoke(collections, "AddToCollection", out _, new object[] { name, list })   ||
                TryInvoke(collections, "AddToCollection", out _, new object[] { name, ienum })  ||
                TryInvoke(collections, "AddToCollection", out _, new object[] { name, iread }))
            {
                return;
            }

            // 5) SetCollectionItems(id, items) (ohne Ensure/Create macht das wenig Sinn; einige ABIs setzen implizit an)
            // -> absichtlich weggelassen, da wir ohne Id nicht sinnvoll aufrufen können.

            throw new MissingMethodException("Keine kompatible ICollectionManager-Methode gefunden.");
        }

        // ---------- Reflection-Utilities ----------

        private static MethodInfo? FindMethod(object obj, string name, Type[] signature)
        {
            return obj.GetType()
                      .GetMethods(BindingFlags.Instance | BindingFlags.Public)
                      .FirstOrDefault(m => string.Equals(m.Name, name, StringComparison.Ordinal) &&
                                           m.GetParameters().Select(p => p.ParameterType).SequenceEqual(signature));
        }

        private static MethodInfo? FindMethodFlexible(object obj, string name, Type[] wanted)
        {
            return obj.GetType()
                      .GetMethods(BindingFlags.Instance | BindingFlags.Public)
                      .FirstOrDefault(m =>
                      {
                          if (!string.Equals(m.Name, name, StringComparison.Ordinal)) return false;
                          var pars = m.GetParameters();
                          if (pars.Length != wanted.Length) return false;

                          for (int i = 0; i < pars.Length; i++)
                          {
                              var have = pars[i].ParameterType;
                              var need = wanted[i];
                              if (need == typeof(IEnumerable<Guid>))
                              {
                                  // akzeptiere IEnumerable<Guid>, IReadOnlyCollection<Guid>, ICollection<Guid>, List<Guid>, Guid[]
                                  if (!(have == typeof(Guid[]) ||
                                        (have.IsGenericType && typeof(IEnumerable<>).IsAssignableFrom(have.GetGenericTypeDefinition())) ||
                                        (have.IsGenericType && typeof(IReadOnlyCollection<>).IsAssignableFrom(have.GetGenericTypeDefinition())) ||
                                        (have.IsGenericType && typeof(ICollection<>).IsAssignableFrom(have.GetGenericTypeDefinition()))))
                                      return false;
                              }
                              else if (have != need) return false;
                          }
                          return true;
                      });
        }

        private static bool TryInvoke(object obj, string name, out object? result, object[] args)
        {
            result = null;
            var candidates = obj.GetType()
                                .GetMethods(BindingFlags.Instance | BindingFlags.Public)
                                .Where(m => m.Name == name && m.GetParameters().Length == args.Length);
            foreach (var mi in candidates)
            {
                try { result = mi.Invoke(obj, args); return true; } catch { /* next */ }
            }
            return false;
        }

        private static async Task<bool> TryInvokeAsync(object obj, string name, object[] args)
        {
            var candidates = obj.GetType()
                                .GetMethods(BindingFlags.Instance | BindingFlags.Public)
                                .Where(m => m.Name == name && m.GetParameters().Length == args.Length);
            foreach (var mi in candidates)
            {
                try { var t = (Task)mi.Invoke(obj, args)!; await t.ConfigureAwait(false); return true; }
                catch { /* next */ }
            }
            return false;
        }

        private static dynamic GetTaskResult(Task t)
        {
            var tp = t.GetType();
            if (tp.IsGenericType) return tp.GetProperty("Result")!.GetValue(t)!;
            throw new InvalidOperationException("Task hat kein Result.");
        }
    }
}
