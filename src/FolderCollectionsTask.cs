using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Querying;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;
using Jellyfin.Data.Enums; // BaseItemKind

namespace FolderCollections
{
    public class FolderCollectionsTask : IScheduledTask
    {
        private readonly ILogger<FolderCollectionsTask> _logger;
        private readonly ILibraryManager _library;
        // Jellyfin 10.10.7 hat ICollectionManager – wir referenzieren ihn, speichern aber als object für Reflection-Flexibilität
        private readonly object _collectionManager;

        public FolderCollectionsTask(
            ILogger<FolderCollectionsTask> logger,
            ILibraryManager libraryManager,
            MediaBrowser.Controller.Collections.ICollectionManager collectionManager)
        {
            _logger = logger;
            _library = libraryManager;
            _collectionManager = collectionManager; // as object
        }

        public string Name => "Folder Collections: täglicher Scan";
        public string Key => "FolderCollections.DailyScan";
        public string Description => "Erstellt/aktualisiert Sammlungen basierend auf der Ordnerstruktur.";
        public string Category => "Library";

        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
            => Array.Empty<TaskTriggerInfo>();

        public async Task ExecuteAsync(IProgress<double>? progress, CancellationToken cancellationToken)
        {
            var cfg = Plugin.Instance?.Configuration ?? new PluginConfiguration();

            _logger.LogInformation(
                "FolderCollectionsTask gestartet. IncludeMovies={IncludeMovies}, IncludeSeries={IncludeSeries}, MinItems={MinItems}, Prefix='{Prefix}', Suffix='{Suffix}', UseBasename={UseBasename}, Scan={Hour:D2}:{Minute:D2}",
                cfg.IncludeMovies, cfg.IncludeSeries, cfg.MinItems, cfg.Prefix, cfg.Suffix, cfg.UseBasenameAsCollectionName, cfg.ScanHour, cfg.ScanMinute);

            progress?.Report(1);

            // 1) Roots/Ignore vorbereiten
            var roots = (cfg.PathPrefixes ?? Array.Empty<string>())
                        .Where(s => !string.IsNullOrWhiteSpace(s))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToArray();

            var ignore = PrepareIgnoreMatchers(cfg.IgnorePatterns ?? Array.Empty<string>());

            _logger.LogInformation("FolderCollections: konfigurierte Roots: {Count} -> {Roots}",
                roots.Length, string.Join(" | ", roots));

            // 2) Verzeichnisse sammeln (Root + rekursiv)
            var allDirs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var root in roots)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!Directory.Exists(root))
                {
                    _logger.LogWarning("Konfigurierter Pfad existiert nicht (übersprungen): {Root}", root);
                    continue;
                }

                if (!IsIgnored(root, ignore))
                    allDirs.Add(Path.TrimEndingDirectorySeparator(root));

                CollectDirectoriesRecursive(root, allDirs, ignore, cancellationToken);
            }

            _logger.LogInformation("FolderCollections: gesammelte Verzeichnisse: {Count}", allDirs.Count);
            progress?.Report(20);

            // 3) Für jedes Verzeichnis die Items ermitteln und ggf. Collection erzeugen
            int processed = 0, created = 0, updated = 0, skipped = 0;
            foreach (var dir in allDirs.OrderBy(s => s, StringComparer.OrdinalIgnoreCase))
            {
                cancellationToken.ThrowIfCancellationRequested();

                processed++;

                // Filme/Serien unterhalb dieses Ordners holen (wir mappen Pfade -> BaseItems)
                var filePaths = EnumerateMediaFiles(dir, cfg.IncludeMovies, cfg.IncludeSeries);
                if (filePaths.Count < cfg.MinItems)
                {
                    skipped++;
                    continue;
                }

                // Collection-Name aufbauen
                string name = cfg.UseBasenameAsCollectionName
                    ? GetBasename(dir)
                    : dir;

                if (!string.IsNullOrWhiteSpace(cfg.Prefix)) name = cfg.Prefix + name;
                if (!string.IsNullOrWhiteSpace(cfg.Suffix)) name = name + cfg.Suffix;

                // Library-Items auflösen
                var items = new List<BaseItem>();
                foreach (var p in filePaths)
                {
                    var bi = _library.FindByPath(p, null); // null => jede Library
                    if (bi != null) items.Add(bi);
                }

                if (items.Count < cfg.MinItems)
                {
                    _logger.LogDebug("Ordner {Dir}: zu wenige indizierte Items ({Count} < {Min}), übersprungen.", dir, items.Count, cfg.MinItems);
                    skipped++;
                    continue;
                }

                // Collection sicherstellen
                var box = await EnsureCollectionAsync(name, cancellationToken);
                if (box == null)
                {
                    _logger.LogWarning("Collection konnte nicht erstellt werden (Create/Ensure nicht gefunden): \"{Name}\"", name);
                    continue;
                }

                var ok = await AddItemsToCollectionAsync(box, items, cancellationToken);
                if (!ok)
                {
                    _logger.LogWarning("Items konnten nicht zur Collection hinzugefügt werden (AddToCollection* nicht gefunden): \"{Name}\"", name);
                    continue;
                }

                // Erfolg grob klassifizieren
                if (box.DateCreatedUtc.AddMinutes(1) > DateTime.UtcNow) created++;
                else updated++;
            }

            _logger.LogInformation("FolderCollectionsTask beendet. Verarbeitete Ordner: {Processed}, erstellt: {Created}, aktualisiert: {Updated}, übersprungen: {Skipped}",
                processed, created, updated, skipped);

            progress?.Report(100);
        }

        // ===== Helpers =====

        private static string GetBasename(string path)
        {
            var t = path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var name = Path.GetFileName(t);
            return string.IsNullOrWhiteSpace(name) ? t : name;
        }

        private static void CollectDirectoriesRecursive(
            string root,
            HashSet<string> bag,
            List<Func<string, bool>> ignore,
            CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                foreach (var sub in Directory.GetDirectories(root))
                {
                    ct.ThrowIfCancellationRequested();
                    if (IsIgnored(sub, ignore)) continue;

                    var cleaned = Path.TrimEndingDirectorySeparator(sub);
                    if (bag.Add(cleaned))
                        CollectDirectoriesRecursive(sub, bag, ignore, ct);
                }
            }
            catch (UnauthorizedAccessException) { }
            catch (PathTooLongException) { }
            catch (DirectoryNotFoundException) { }
        }

        private static bool IsIgnored(string path, List<Func<string, bool>> matchers)
        {
            foreach (var m in matchers)
                if (m(path)) return true;
            return false;
        }

        private static List<Func<string, bool>> PrepareIgnoreMatchers(IEnumerable<string> patterns)
        {
            var list = new List<Func<string, bool>>();

            foreach (var raw in patterns.Where(p => !string.IsNullOrWhiteSpace(p)))
            {
                var p = raw.Trim();

                if (p.StartsWith("re:", StringComparison.OrdinalIgnoreCase))
                {
                    var rx = new Regex(p.Substring(3), RegexOptions.IgnoreCase | RegexOptions.Compiled);
                    list.Add(s => rx.IsMatch(s));
                }
                else
                {
                    // Wildcards * ? -> Regex
                    var esc = Regex.Escape(p).Replace(@"\*", ".*").Replace(@"\?", ".");
                    var rx = new Regex("^" + esc + "$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
                    list.Add(s => rx.IsMatch(s));
                }
            }

            return list;
        }

        private static readonly string[] MovieExt =
        {
            ".mkv",".mp4",".m4v",".mov",".avi",".wmv",".mpg",".mpeg",".ts",".m2ts",".webm",".flv",".3gp"
        };

        private static readonly string[] EpisodeExt = MovieExt; // falls Episoden – gleiche Exts

        private static List<string> EnumerateMediaFiles(string dir, bool includeMovies, bool includeSeries)
        {
            var files = new List<string>();
            try
            {
                // einfache Heuristik: alle Video-Dateien im Baum
                foreach (var f in Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories))
                {
                    var ext = Path.GetExtension(f);
                    if (string.IsNullOrEmpty(ext)) continue;

                    if (includeMovies && MovieExt.Contains(ext, StringComparer.OrdinalIgnoreCase))
                        files.Add(f);
                    else if (includeSeries && EpisodeExt.Contains(ext, StringComparer.OrdinalIgnoreCase))
                        files.Add(f);
                }
            }
            catch (Exception) { /* ignorieren */ }

            return files;
        }

        private BoxSet? FindBoxSetByName(string name)
        {
            var q = new InternalItemsQuery
            {
                IncludeItemTypes = new[] { BaseItemKind.BoxSet }, // statt string[]
                Name = name,
                Recursive = true,
            };
            var list = _library.GetItemList(q);
            return list.OfType<BoxSet>().FirstOrDefault();
        }


        private async Task<BoxSet?> EnsureCollectionAsync(string collectionName, CancellationToken ct)
        {
            // bereits vorhanden?
            var existing = FindBoxSetByName(collectionName);
            if (existing != null) return existing;

            var cmType = _collectionManager.GetType();
            var method = cmType
                .GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .FirstOrDefault(m =>
                {
                    var n = m.Name;
                    if (!(n.Contains("CreateCollection", StringComparison.OrdinalIgnoreCase)
                          || n.Contains("EnsureCollection", StringComparison.OrdinalIgnoreCase)))
                        return false;

                    var ps = m.GetParameters();
                    return ps.Length >= 1 && ps[0].ParameterType == typeof(string);
                });

            if (method == null)
            {
                _logger.LogWarning("Keine Create*/Ensure*-Methode am ICollectionManager gefunden.");
                return null;
            }

            object? result;
            var ps = method.GetParameters();
            var args = new object?[ps.Length];
            args[0] = collectionName;

            for (int i = 1; i < ps.Length; i++)
            {
                var p = ps[i].ParameterType;
                if (p == typeof(CancellationToken)) args[i] = ct;
                else if (!p.IsValueType) args[i] = null;
                else args[i] = Activator.CreateInstance(p);
            }

            try
            {
                if (typeof(Task).IsAssignableFrom(method.ReturnType))
                {
                    var task = (Task)method.Invoke(_collectionManager, args)!;
                    await task.ConfigureAwait(false);
                    var retProp = method.ReturnType.GetProperty("Result");
                    result = retProp != null ? retProp.GetValue(task) : null;
                }
                else
                {
                    result = method.Invoke(_collectionManager, args);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Erstellen der Collection '{Name}' via {Method} fehlgeschlagen.", collectionName, method.Name);
                return null;
            }

            if (result is BoxSet bs) return bs;

            // Nach Anlage erneut suchen
            return FindBoxSetByName(collectionName);
        }

        private async Task<bool> AddItemsToCollectionAsync(BoxSet collection, IEnumerable<BaseItem> items, CancellationToken ct)
        {
            var list = items?.Where(i => i != null).ToList() ?? new List<BaseItem>();
            if (list.Count == 0) return true;

            var cmType = _collectionManager.GetType();
            var methods = cmType.GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .Where(m => m.Name.Contains("AddToCollection", StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var m in methods)
            {
                var ps = m.GetParameters();

                bool matchA = ps.Length >= 2
                              && typeof(BoxSet).IsAssignableFrom(ps[0].ParameterType)
                              && typeof(IEnumerable<BaseItem>).IsAssignableFrom(ps[1].ParameterType);

                bool matchB = ps.Length >= 2
                              && ps[0].ParameterType == typeof(Guid)
                              && typeof(IEnumerable<Guid>).IsAssignableFrom(ps[1].ParameterType);

                if (!matchA && !matchB) continue;

                var args = new object?[ps.Length];
                if (matchA)
                {
                    args[0] = collection;
                    args[1] = list;
                }
                else
                {
                    args[0] = collection.Id;
                    args[1] = list.Select(i => i.Id).ToList();
                }

                for (int i = 2; i < ps.Length; i++)
                {
                    var p = ps[i].ParameterType;
                    if (p == typeof(CancellationToken)) args[i] = ct;
                    else if (!p.IsValueType) args[i] = null;
                    else args[i] = Activator.CreateInstance(p);
                }

                try
                {
                    if (typeof(Task).IsAssignableFrom(m.ReturnType))
                    {
                        var t = (Task)m.Invoke(_collectionManager, args)!;
                        await t.ConfigureAwait(false);
                    }
                    else
                    {
                        m.Invoke(_collectionManager, args);
                    }

                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "AddToCollection via {Method} fehlgeschlagen – versuche nächste Variante.", m.Name);
                }
            }

            _logger.LogWarning("Keine passende AddToCollection*-Methode auf ICollectionManager gefunden.");
            return false;
        }
    }
}
