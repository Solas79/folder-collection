using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Collections;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Querying;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;

namespace FolderCollections
{
    /// <summary>
    /// Täglicher Scan: erzeugt/aktualisiert Sammlungen basierend auf der Ordnerstruktur.
    /// Arbeitet rein über Jellyfins Library (AncestorIds), nicht über Dateisystem-Mapping.
    /// Stabil für Jellyfin 10.10.7.
    /// </summary>
    public class FolderCollectionsTask : IScheduledTask
    {
        private readonly ILogger<FolderCollectionsTask> _logger;

        // Lazy über Plugin.Instance aufgelöste Services (kein DI im ctor notwendig)
        private ICollectionManager? _collections;
        private ILibraryManager? _library;

        public FolderCollectionsTask(ILogger<FolderCollectionsTask> logger)
        {
            _logger = logger;
        }

        public string Name => "Folder Collections: täglicher Scan";
        public string Key => "FolderCollections.DailyScan";
        public string Description => "Erstellt/aktualisiert Sammlungen basierend auf der Ordnerstruktur.";
        public string Category => "Library";

        public async Task ExecuteAsync(IProgress<double>? progress, CancellationToken cancellationToken)
        {
            // Services sicherstellen
            _collections ??= Plugin.Instance?.ApplicationHost?.Resolve<ICollectionManager>();
            _library     ??= Plugin.Instance?.ApplicationHost?.Resolve<ILibraryManager>();

            if (_collections is null || _library is null)
                throw new InvalidOperationException("Jellyfin-Services nicht verfügbar (ICollectionManager/ILibraryManager).");

            var cfg = Plugin.Instance?.Configuration ?? new PluginConfiguration();

            _logger.LogInformation(
                "FolderCollectionsTask gestartet. IncludeMovies={IncludeMovies}, IncludeSeries={IncludeSeries}, MinItems={MinItems}, Prefix='{Prefix}', Suffix='{Suffix}', UseBasename={UseBase}, Scan={Hour:D2}:{Minute:D2}",
                cfg.IncludeMovies, cfg.IncludeSeries, cfg.MinItems, cfg.Prefix, cfg.Suffix, cfg.UseBasenameAsCollectionName, cfg.ScanHour, cfg.ScanMinute);

            progress?.Report(0);

            // 1) Verzeichnisse sammeln
            var roots = cfg.PathPrefixes ?? Array.Empty<string>();
            var ignore = PrepareIgnoreMatchers(cfg.IgnorePatterns ?? Array.Empty<string>());
            var allDirs = new List<string>();

            foreach (var root in roots)
            {
                if (string.IsNullOrWhiteSpace(root)) continue;

                try
                {
                    if (!Directory.Exists(root))
                    {
                        _logger.LogWarning("Konfigurierter Pfad existiert nicht: {Root}", root);
                        continue;
                    }
                    CollectDirectoriesRecursive(root, allDirs, ignore, cancellationToken);
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Fehler beim Durchlaufen von Root {Root}", root);
                }
            }

            progress?.Report(40);

            // 2) Für jeden Ordner den Collection-Namen bilden & Collection anlegen/füllen
            var total = allDirs.Count;
            var done = 0;

            foreach (var dir in allDirs)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var name = BuildCollectionName(dir, cfg);
                if (string.IsNullOrWhiteSpace(name))
                {
                    _logger.LogDebug("Überspringe leeren Namen für Pfad: {Dir}", dir);
                    continue;
                }

                try
                {
                    await EnsureCollectionAsync(name, dir, cfg, cancellationToken);
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Fehler bei Collection '{Name}' für Pfad '{Dir}'", name, dir);
                }

                done++;
                if (done % 20 == 0 || done == total)
                {
                    var pct = 40 + (int)Math.Round(55.0 * done / Math.Max(1, total));
                    progress?.Report(Math.Min(95, pct));
                }
            }

            progress?.Report(100);
            _logger.LogInformation("FolderCollectionsTask beendet. Verarbeitete Ordner: {Count}", total);
        }

        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
        {
            // Standardmäßig keine Auto-Trigger; Zeitplan im Dashboard konfigurieren
            return Array.Empty<TaskTriggerInfo>();
        }

        // ---------- Implementierung ----------

        /// <summary>
        /// Sucht/erstellt eine Collection und füllt sie mit Items, die in Jellyfins Library unterhalb des Ordners liegen.
        /// </summary>
        private async Task EnsureCollectionAsync(string collectionName, string sourceDir, PluginConfiguration cfg, CancellationToken ct)
        {
            // Ordner im Jellyfin-Index finden
            var folderItem = _library!.FindByPath(sourceDir, null);
            if (folderItem == null)
            {
                _logger.LogWarning("Ordner nicht in der Jellyfin-Library gefunden (übersprungen): {Dir}", sourceDir);
                return;
            }

            // Collection finden/erstellen
            var boxSet = FindBoxSetByName(collectionName);
            if (boxSet == null)
            {
                await _collections!.CreateCollection(new List<Guid>(), collectionName, null, ct);
                _logger.LogInformation("Collection erstellt: {Name}", collectionName);

                boxSet = FindBoxSetByName(collectionName);
                if (boxSet == null)
                {
                    _logger.LogWarning("Collection nach Erstellung nicht auffindbar: {Name}", collectionName);
                    return;
                }
            }

            // Items via InternalItemsQuery (AncestorIds)
            var itemIds = GetLibraryItemIdsUnderFolder(folderItem, cfg);

            // MinItems prüfen
            if (itemIds.Count < Math.Max(0, cfg.MinItems))
            {
                _logger.LogDebug("Zu wenige Items ({Count}/{Min}) für '{Name}' – überspringe.", itemIds.Count, cfg.MinItems, collectionName);
                return;
            }

            if (itemIds.Count > 0)
            {
                await _collections!.AddToCollection(boxSet.Id, itemIds, ct);
                _logger.LogInformation("Collection '{Name}' aktualisiert (+{Count} Items).", collectionName, itemIds.Count);
            }
        }

        /// <summary>
        /// Findet ein BoxSet mit exakt passendem Namen.
        /// </summary>
        private BaseItem? FindBoxSetByName(string name)
        {
            var result = _library!.GetItemList(new InternalItemsQuery
            {
                IncludeItemTypes = new[] { nameof(BoxSet) },
                Name = name,
                Limit = 1
            });

            return result.FirstOrDefault();
        }

        /// <summary>
        /// Liefert IDs aller Movies/Series unterhalb eines Library-Ordners (konfigurierbar).
        /// Episoden werden als Serie hinzugefügt, nicht einzeln.
        /// </summary>
        private List<Guid> GetLibraryItemIdsUnderFolder(BaseItem folderItem, PluginConfiguration cfg)
        {
            var ids = new HashSet<Guid>();

            if (cfg.IncludeMovies)
            {
                var movies = _library!.GetItemList(new InternalItemsQuery
                {
                    IncludeItemTypes = new[] { nameof(Movie) },
                    AncestorIds = new[] { folderItem.Id },
                    Recursive = true
                }).OfType<Movie>();

                foreach (var m in movies)
                    ids.Add(m.Id);
            }

            if (cfg.IncludeSeries)
            {
                var series = _library!.GetItemList(new InternalItemsQuery
                {
                    IncludeItemTypes = new[] { nameof(Series) },
                    AncestorIds = new[] { folderItem.Id },
                    Recursive = true
                }).OfType<Series>();

                foreach (var s in series)
                    ids.Add(s.Id);

                // Falls du stattdessen Episoden direkt sammeln willst, ersetze den obigen Block durch:
                // var episodes = _library!.GetItemList(new InternalItemsQuery
                // {
                //     IncludeItemTypes = new[] { nameof(Episode) },
                //     AncestorIds = new[] { folderItem.Id },
                //     Recursive = true
                // }).OfType<Episode>();
                // foreach (var e in episodes) ids.Add(e.Id);
            }

            return ids.ToList();
        }

        /// <summary>
        /// Baut den Sammlungsnamen aus Basename/vollem Pfad + Prefix/Suffix.
        /// </summary>
        private static string BuildCollectionName(string directoryPath, PluginConfiguration cfg)
        {
            if (string.IsNullOrWhiteSpace(directoryPath)) return string.Empty;

            var trimmed = Path.TrimEndingDirectorySeparator(directoryPath);
            var baseName = Path.GetFileName(trimmed);

            var name = cfg.UseBasenameAsCollectionName
                ? (string.IsNullOrEmpty(baseName) ? trimmed : baseName)
                : trimmed;

            if (!string.IsNullOrWhiteSpace(cfg.Prefix)) name = $"{cfg.Prefix}{name}";
            if (!string.IsNullOrWhiteSpace(cfg.Suffix)) name = $"{name}{cfg.Suffix}";
            return name;
        }

        /// <summary>
        /// Rekursiv Unterordner einsammeln, unter Berücksichtigung von Ignore-Mustern.
        /// </summary>
        private static void CollectDirectoriesRecursive(string root, List<string> bag, List<Func<string, bool>> ignore, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                foreach (var sub in Directory.GetDirectories(root))
                {
                    ct.ThrowIfCancellationRequested();

                    if (IsIgnored(sub, ignore))
                        continue;

                    bag.Add(sub);
                    CollectDirectoriesRecursive(sub, bag, ignore, ct);
                }
            }
            catch (UnauthorizedAccessException)
            {
                // Überspringen
            }
            catch (PathTooLongException)
            {
                // Überspringen
            }
        }

        private static bool IsIgnored(string path, List<Func<string, bool>> ignore)
        {
            foreach (var f in ignore)
            {
                try { if (f(path)) return true; }
                catch { /* ignorieren */ }
            }
            return false;
        }

        /// <summary>
        /// Erzeugt Matcher für IgnorePatterns: Wildcards oder Regex (mit "re:").
        /// </summary>
        private static List<Func<string, bool>> PrepareIgnoreMatchers(IEnumerable<string> patterns)
        {
            var list = new List<Func<string, bool>>();
            foreach (var raw in patterns)
            {
                var p = (raw ?? "").trim();
                if (string.IsNullOrEmpty(p)) continue;

                if (p.StartsWith("re:", StringComparison.OrdinalIgnoreCase))
                {
                    var re = p.Substring(3);
                    var rx = new Regex(re, RegexOptions.Compiled | RegexOptions.IgnoreCase);
                    list.Add(path => rx.IsMatch(path));
                }
                else
                {
                    var esc = Regex.Escape(p).Replace(@"\*", ".*").Replace(@"\?", ".");
                    var rx = new Regex("^" + esc + "$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
                    list.Add(path => rx.IsMatch(path));
                }
            }
            return list;
        }
    }
}
