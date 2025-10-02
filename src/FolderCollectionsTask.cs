using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Collections;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Entities;            // <- CollectionFolder ist hier
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;
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
                .Where(r => r != null)
                .Cast<Regex>()
                .ToArray();

            var types = new List<string>();
            if (cfg.IncludeMovies) types.Add(nameof(BaseItemKind.Movie));
            if (cfg.IncludeSeries) types.Add(nameof(BaseItemKind.Series));

            if (types.Count == 0)
            {
                _logger.LogInformation("Weder Filme noch Serien gewünscht – nichts zu tun.");
                return;
            }

            var items = _library.GetItemList(new InternalItemsQuery
            {
                IncludeItemTypes = types.ToArray(),
                Recursive = true,
                IsVirtualItem = false
            });

            var groups = items
                .Select(i => new { Item = i, FolderPath = SafeParentDir(i.Path) })
                .Where(x => !string.IsNullOrWhiteSpace(x.FolderPath))
                .Where(x => PassesWhitelist(x.FolderPath!, allowRoots))
                .Where(x => !ignoreRegexes.Any(r => r!.IsMatch(x.FolderPath!)))
                .GroupBy(x => x.FolderPath!, StringComparer.OrdinalIgnoreCase)
                .ToList();

            int created = 0, updated = 0, skipped = 0;
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

                    await _collections.AddToCollectionAsync(collection.Id, itemIds, cancellationToken);

                    if (collection.DateCreatedUtc > DateTime.UtcNow.AddMinutes(-2))
                        created++;
                    else
                        updated++;

                    _logger.LogInformation("Collection '{Name}' -> {Count} Items", collectionName, itemIds.Length);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Fehler bei Collection '{Name}'", collectionName);
                }

                progress?.Report(Percent(++done, total));
            }

            _logger.LogInformation("FolderCollectionsTask beendet. Created={Created}, Updated={Updated}, Skipped={Skipped}",
                created, updated, skipped);
        }

        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers() => Array.Empty<TaskTriggerInfo>();

        // Helpers
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

        private async Task<CollectionFolder> EnsureCollectionAsync(string name, CancellationToken ct)
        {
            var exist = _collections.FindCollectionByName(name);
            if (exist != null) return exist;

            return await _collections.CreateCollectionAsync(name, ct);
        }

        private static double Percent(int done, int total) => total == 0 ? 100 : (done * 100.0 / total);
    }
}
