using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Collections;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;
using Jellyfin.Data.Enums;

namespace Jellyfin.Plugin.FolderCollections.GUI
{
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

        // NEU: Pflicht-Property für Jellyfin
        public string Key => "FolderCollectionsTask";

        public string Name => "Folder Collections (per directory)";
        public string Description => "Create/Update BoxSets based on parent folders.";
        public string Category => "Library";

        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
        {
            // täglich um 04:00
            return new[]
            {
                new TaskTriggerInfo
                {
                    Type = TaskTriggerInfo.TriggerDaily,
                    TimeOfDayTicks = TimeSpan.FromHours(4).Ticks
                }
            };
        }

        // NEU: Signatur entsprechend Jellyfin (Progress zuerst, dann CancellationToken)
        public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
        {
            var cfg = Plugin.Instance?.Configuration ?? new PluginConfiguration();
            _logger.LogInformation(
                "[FolderCollections] Start. IncludeMovies={Movies}, IncludeSeries={Series}",
                cfg.IncludeMovies, cfg.IncludeSeries);

            // Ignore-Regexe kompilieren
            var ignore = (cfg.IgnorePatterns ?? new List<string>())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => new Regex(s, RegexOptions.IgnoreCase | RegexOptions.Compiled))
                .ToList();

            // Items sammeln über InternalItemsQuery (Enum-Varianten!)
            var kinds = new List<BaseItemKind>();
            if (cfg.IncludeMovies) kinds.Add(BaseItemKind.Movie);
            if (cfg.IncludeSeries) kinds.Add(BaseItemKind.Series);

            var query = new MediaBrowser.Controller.Entities.InternalItemsQuery
            {
                IncludeItemTypes = kinds.ToArray(),
                Recursive = true
            };

var allItems = _library.GetItemList(query).ToList();


            // gruppieren nach Parent-Ordner (mit Präfix-Whitelist & Ignore)
            var groups = new Dictionary<string, List<BaseItem>>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in allItems)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var path = item.Path;
                if (string.IsNullOrWhiteSpace(path)) continue;

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

                // Ignore
                var ignored = false;
                foreach (var rx in ignore)
                {
                    if (rx.IsMatch(path)) { ignored = true; break; }
                }
                if (ignored) continue;

                var parent = System.IO.Path.GetDirectoryName(
                    path.TrimEnd(System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar));
                if (string.IsNullOrEmpty(parent)) continue;

                if (!groups.TryGetValue(parent, out var list))
                {
                    list = new List<BaseItem>();
                    groups[parent] = list;
                }
                list.Add(item);
            }

            // Mindestanzahl anwenden
            var minItems = Math.Max(1, cfg.MinimumItemsPerFolder);
            var filtered = new Dictionary<string, List<BaseItem>>(StringComparer.OrdinalIgnoreCase);
            foreach (var kv in groups)
            {
                if (kv.Value.Count >= minItems) filtered[kv.Key] = kv.Value;
            }

            // Collections erzeugen/aktualisieren (Variante A)
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

                try
                {
                    // über LibraryManager nach bestehender BoxSet suchen
                    var q = new MediaBrowser.Controller.Entities.InternalItemsQuery
                    {
                        IncludeItemTypes = new[] { BaseItemKind.BoxSet },
                        Recursive = true,
                        SearchTerm = name
                    };
                    var existing = _library.GetItemList(q)
                        .FirstOrDefault(i => string.Equals(i.Name, name, StringComparison.OrdinalIgnoreCase));

                    var ids = items.Select(i => i.Id).ToArray();

                    if (existing == null)
                    {
                        // Legt die Collection an, wenn sie nicht existiert, und fügt Items hinzu
                        await _collections.AddToCollectionAsync(name, ids, cancellationToken).ConfigureAwait(false);
                        _logger.LogInformation("[FolderCollections] Created '{Name}' with {Count} items", name, ids.Length);
                    }
                    else
                    {
                        // Setzt die Items exakt (überschreibt Inhalt)
                        await _collections.SetCollectionItemsAsync(existing.Id, ids, cancellationToken).ConfigureAwait(false);
                        _logger.LogInformation("[FolderCollections] Updated '{Name}' with {Count} items", name, ids.Length);
                    }
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
}
