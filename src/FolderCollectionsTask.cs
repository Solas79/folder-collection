using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;                         // BaseItemKind (Movie, Series, BoxSet)
using MediaBrowser.Controller.Collections;        // ICollectionManager
using MediaBrowser.Controller.Entities;           // CollectionFolder
using MediaBrowser.Controller.Library;            // ILibraryManager
using MediaBrowser.Model.Querying;                // InternalItemsQuery
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;

namespace FolderCollections
{
    public class FolderCollectionsTask : IScheduledTask
    {
        private readonly ILogger<FolderCollectionsTask> _logger;
        private readonly ILibraryManager _library;
        private readonly ICollectionManager _collections;

        public FolderCollectionsTask(
            ILogger<FolderCollectionsTask> logger,
            ILibraryManager library,
            ICollectionManager collections)
        {
            _logger = logger;
            _library = library;
            _collections = collections;
        }

        public string Name => "Folder Collections: täglicher Scan";
        public string Key => "FolderCollections.DailyScan";
        public string Description => "Erstellt/aktualisiert Sammlungen basierend auf der Ordnerstruktur.";
        public string Category => "Library";

        public async Task ExecuteAsync(IProgress<double>? progress, CancellationToken cancellationToken)
        {
            var cfg = Plugin.Instance?.Configuration ?? new PluginConfiguration();

            _logger.LogInformation(
                "FolderCollectionsTask gestartet. IncludeMovies={IncludeMovies}, IncludeSeries={IncludeSeries}, MinItems={MinItems}, Prefix='{Prefix}', Suffix='{Suffix}', Scan={Hour:D2}:{Minute:D2}",
                cfg.IncludeMovies, cfg.IncludeSeries, cfg.MinItems, cfg.Prefix, cfg.Suffix, cfg.ScanHour, cfg.ScanMinute);

            var allowRoots = (cfg.PathPrefixes ?? Array.Empty<string>())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Trim())
                .ToArray();

            var ignoreRegexes = (cfg.IgnorePatterns ?? Array.Empty<string>())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(p =>
                {
                    try { return new Regex(p, RegexOptions.IgnoreCase | RegexOptions.Compiled); }
                    catch { _logger.LogWarning("Ignore-Pattern ungültig: {Pattern}", p); return null; }
                })
                .Where(r => r != null)!
                .Cast<Regex>()
                .ToArray();

            // Medientypen bestimmen (Jellyfin 10.10.x – BaseItemKind)
            var kinds = new List<BaseItemKind>();
            if (cfg.IncludeMovies) kinds.Add(BaseItemKind.Movie);
            if (cfg.IncludeSeries) kinds.Add(BaseItemKind.Series);
            if (kinds.Count == 0)
            {
                _logger.LogInformation("Weder Filme noch Serien gewünscht – nichts zu tun.");
                return;
            }

            // Medien abrufen
            var items = _library.GetItemList(new InternalItemsQuery
            {
                IncludeItemTypes = kinds.ToArray(),   // BaseItemKind[]
                Recursive = true,
                IsVirtualItem = false
            });

            // Nach Elternordner gruppieren
            var groups = items
                .Select(i => new { Item = i, FolderPath = SafeParentDir(i.Path) })
                .Where(x => !string.IsNullOrWhiteSpace(x.FolderPath))
                .Where(x => PassesWhitelist(x.FolderPath!, allowRoots))
                .Where(x => !ignoreRegexes.Any(r => r.IsMatch(x.FolderPath!)))
                .GroupBy(x => x.FolderPath!, StringComparer.OrdinalIgnoreCase)
                .ToList();

            int changed = 0, skipped = 0;
            int done = 0, total = groups.Count;

            foreach (var g in groups)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var itemIds = g.Select(x => x.Item.Id).Distinct().ToArray();
                if (itemIds.Length < Math.Max(2, cfg.MinItems))
                {
                    skipped++;
                    progress?.Report(Percent(++done, total));
                    continue;
                }

                var folderName = new DirectoryInfo(g.Key).Name;
                var collectionName = BuildName(cfg.Prefix, folderName, cfg.Suffix);

                try
                {
                    var collection = await EnsureCollectionAsync(collectionName, cancellationToken);

                    // Items hinzufügen – beide geläufigen Overloads abdecken
                    var ok = await TryAddById(collection, itemIds, cancellationToken)
                             || await TryAddByFolder(collection, itemIds, cancellationToken);

                    if (ok)
                    {
                        changed++;
                        _logger.LogInformation("Collection '{Name}' -> {Count} Items", collectionName, itemIds.Length);
                    }
                    else
                    {
                        _logger.LogWarning("Items konnten nicht zu '{Name}' hinzugefügt werden (keine passende Overload).", collectionName);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Fehler bei Collection '{Name}'", collectionName);
                }

                progress?.Report(Percent(++done, total));
            }

            _logger.LogInformation("FolderCollectionsTask beendet. Collections geändert={Changed}, übersprungen={Skipped}",
                changed, skipped);
        }

        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers() => Array.Empty<TaskTriggerInfo>();

        // ===== Helpers =====

        private static string SafeParentDir(string? path)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(path)) return string.Empty;
                var dir = Path.GetDirectoryName(path);
                return dir ?? string.Empty;
            }
            catch { return string.Empty; }
        }

        private static bool PassesWhitelist(string folderPath, string[] allowRoots)
        {
            if (allowRoots.Length == 0) return true;
            return allowRoots.Any(p => folderPath.StartsWith(p, StringComparison.OrdinalIgnoreCase));
        }

        private static string BuildName(string prefix, string core, string suffix)
        {
            var left = string.IsNullOrWhiteSpace(prefix) ? "" : prefix.Trim();
            var right = string.IsNullOrWhiteSpace(suffix) ? "" : suffix.Trim();
            return string.Join(" ", new[] { left, core, right }.Where(s => !string.IsNullOrWhiteSpace(s)));
        }

        /// <summary>Bestehende Collection per ILibraryManager finden (nach Name), sonst null.</summary>
        private CollectionFolder? FindCollectionByName(string name)
        {
            try
            {
                // Primär gezielt nach BoxSets suchen
                var q = new InternalItemsQuery
                {
                    IncludeItemTypes = new[] { BaseItemKind.BoxSet },
                    Recursive = true,
                    SearchTerm = name
                };

                return _library.GetItemList(q)
                    .OfType<CollectionFolder>()
                    .FirstOrDefault(i => string.Equals(i.Name, name, StringComparison.OrdinalIgnoreCase));
            }
            catch
            {
                // Fallback: ohne Type-Filter
                var q2 = new InternalItemsQuery
                {
                    Recursive = true,
                    SearchTerm = name
                };

                return _library.GetItemList(q2)
                    .OfType<CollectionFolder>()
                    .FirstOrDefault(i => string.Equals(i.Name, name, StringComparison.OrdinalIgnoreCase));
            }
        }

        /// <summary>Sichert, dass die Collection existiert (finden oder neu erstellen).</summary>
        private async Task<CollectionFolder> EnsureCollectionAsync(string name, CancellationToken ct)
        {
            var exist = FindCollectionByName(name);
            if (exist != null)
                return exist;

            return await _collections.CreateCollectionAsync(name, ct);
        }

        // Overload A: (Guid, IEnumerable<Guid>)
        private async Task<bool> TryAddById(CollectionFolder folder, IReadOnlyCollection<Guid> itemIds, CancellationToken ct)
        {
            try
            {
                await _collections.AddToCollectionAsync(folder.Id, itemIds);
                return true;
            }
            catch { return false; }
        }

        // Overload B: (CollectionFolder, IEnumerable<Guid>)
        private async Task<bool> TryAddByFolder(CollectionFolder folder, IReadOnlyCollection<Guid> itemIds, CancellationToken ct)
        {
            try
            {
                await _collections.AddToCollectionAsync(folder, itemIds);
                return true;
            }
            catch { return false; }
        }

        private static double Percent(int done, int total) => total == 0 ? 100 : (done * 100.0 / total);
    }
}
