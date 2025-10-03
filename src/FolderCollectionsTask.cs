using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;                         // BaseItemKind
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Querying;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;

namespace FolderCollections
{
    public class FolderCollectionsTask : IScheduledTask
    {
        private readonly ILogger<FolderCollectionsTask> _logger;
        private readonly ILibraryManager _library;
        // bewusst als konkretes Interface – existiert in 10.10.x
        private readonly MediaBrowser.Controller.Collections.ICollectionManager _collectionManager;

        public FolderCollectionsTask(
            ILogger<FolderCollectionsTask> logger,
            ILibraryManager libraryManager,
            MediaBrowser.Controller.Collections.ICollectionManager collectionManager)
        {
            _logger = logger;
            _library = libraryManager;
            _collectionManager = collectionManager;
        }

        public string Name => "Folder Collections: täglicher Scan";
        public string Key => "FolderCollections.DailyScan";
        public string Description => "Erstellt/aktualisiert Sammlungen basierend auf der Ordnerstruktur.";
        public string Category => "Library";

        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers() => Array.Empty<TaskTriggerInfo>();

        public async Task ExecuteAsync(IProgress<double>? progress, CancellationToken cancellationToken)
        {
            var cfg = Plugin.Instance?.Configuration ?? new PluginConfiguration();

            _logger.LogInformation(
                "FolderCollectionsTask gestartet. IncludeMovies={IncludeMovies}, IncludeSeries={IncludeSeries}, MinItems={MinItems}, Prefix='{Prefix}', Suffix='{Suffix}', UseBasename={UseBasename}, Scan={Hour:D2}:{Minute:D2}",
                cfg.IncludeMovies, cfg.IncludeSeries, cfg.MinItems, cfg.Prefix, cfg.Suffix, cfg.UseBasenameAsCollectionName, cfg.ScanHour, cfg.ScanMinute);

            progress?.Report(1);

            var roots = (cfg.PathPrefixes ?? Array.Empty<string>())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            var ignore = PrepareIgnoreMatchers(cfg.IgnorePatterns ?? Array.Empty<string>());

            if (roots.Length == 0)
            {
                _logger.LogWarning("Keine Wurzelpfade konfiguriert – nichts zu tun.");
                progress?.Report(100);
                return;
            }

            _logger.LogInformation("Konfigurierte Wurzelpfade ({Count}): {Roots}", roots.Length, string.Join(" | ", roots));

            // Verzeichnisse sammeln
            var allDirs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var root in roots)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!Directory.Exists(root))
                {
                    _logger.LogWarning("Pfad existiert nicht (übersprungen): {Root}", root);
                    continue;
                }

                var cleaned = Path.TrimEndingDirectorySeparator(root);
                if (!IsIgnored(cleaned, ignore)) allDirs.Add(cleaned);

                CollectDirectoriesRecursive(cleaned, allDirs, ignore, cancellationToken);
            }

            _logger.LogInformation("Gesammelte Verzeichnisse: {Count}", allDirs.Count);
            progress?.Report(15);

            int processed = 0, created = 0, updated = 0, skipped = 0;

            foreach (var dir in allDirs.OrderBy(s => s, StringComparer.OrdinalIgnoreCase))
            {
                cancellationToken.ThrowIfCancellationRequested();
                processed++;

                // Dateien einsammeln
                var filePaths = EnumerateMediaFiles(dir, cfg.IncludeMovies, cfg.IncludeSeries);
                if (filePaths.Count < cfg.MinItems)
                {
                    skipped++;
                    continue;
                }

                // Collection-Name
                string name = cfg.UseBasenameAsCollectionName ? GetBasename(dir) : dir;
                if (!string.IsNullOrWhiteSpace(cfg.Prefix)) name = cfg.Prefix + name;
                if (!string.IsNullOrWhiteSpace(cfg.Suffix)) name = name + cfg.Suffix;

                // Pfade -> Library-Items
                var items = new List<BaseItem>();
                foreach (var p in filePaths)
                {
                    var bi = _library.FindByPath(p, null); // null => in jeder Library suchen
                    if (bi != null) items.Add(bi);
                }

                if (items.Count < cfg.MinItems)
                {
                    _logger.LogDebug("Ordner {Dir}: {Count} indizierte Items < MinItems {Min} – übersprungen.", dir, items.Count, cfg.MinItems);
                    skipped++;
                    continue;
                }

                // existierte bereits?
                var existedBefore = FindBoxSetByName(name) != null;

                // anlegen/sicherstellen
                var box = await EnsureCollectionAsync(name, cancellationToken);
                if (box == null)
                {
                    _logger.LogWarning("Collection konnte nicht erstellt/gefunden werden: \"{Name}\"", name);
                    continue;
                }

                // Items hinzufügen
                var ok = await AddItemsToCollectionAsync(box, items, cancellationToken);
                if (!ok)
                {
                    _logger.LogWarning("Items konnten nicht zur Collection hinzugefügt werden: \"{Name}\"", name);
                    continue;
                }

                if (existedBefore) updated++; else created++;
            }

            _logger.LogInformation("FolderCollectionsTask beendet. Verarbeitet: {Processed}, erstellt: {Created}, aktualisiert: {Updated}, übersprungen: {Skipped}",
                processed, created, updated, skipped);

            progress?.Report(100);
        }

        // ====== Helpers ======

        private static string GetBasename(string path)
        {
            var t = path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var name = Path.GetFileName(t);
            return string.IsNullOrWhiteSpace(name) ? t : name;
        }

        private static readonly string[] VideoExt =
        {
            ".mkv",".mp4",".m4v",".mov",".avi",".wmv",".mpg",".mpeg",".ts",".m2ts",".webm",".flv",".3gp"
        };

        private static List<string> EnumerateMediaFiles(string dir, bool includeMovies, bool includeSeries)
        {
            var files = new List<string>();
            if (!includeMovies && !includeSeries) return files;

            try
            {
                foreach (var f in Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories))
                {
                    var ext = Path.GetExtension(f);
                    if (string.IsNullOrEmpty(ext)) continue;
                    if (VideoExt.Contains(ext, StringComparer.OrdinalIgnoreCase))
                    {
                        // einfache Heuristik: gleiche Exts für Filme/Episoden
                        files.Add(f);
                    }
                }
            }
            catch (UnauthorizedAccessException) { }
            catch (DirectoryNotFoundException) { }
            catch (PathTooLongException) { }

            return files;
        }

        private static bool IsIgnored(string path, List<Func<string, bool>> matchers)
        {
            foreach (var m in matchers)
                if (m(path)) return true;
            return false;
        }

        private static void CollectDirectoriesRecursive(
            string root, HashSet<string> bag, List<Func<string, bool>> ignore, CancellationToken ct)
        {
            try
            {
                foreach (var sub in Directory.GetDirectories(root))
                {
                    ct.ThrowIfCancellationRequested();
                    if (IsIgnored(sub, ignore)) continue;

                    var cleaned = Path.TrimEndingDirectorySeparator(sub);
                    if (bag.Add(cleaned))
                        CollectDirectoriesRecursive(cleaned, bag, ignore, ct);
                }
            }
            catch (UnauthorizedAccessException) { }
            catch (DirectoryNotFoundException) { }
            catch (PathTooLongException) { }
        }

        private static List<Func<string, bool>> PrepareIgnoreMatchers(IEnumerable<string> patterns)
        {
            var list = new List<Func<string, bool>>();
            foreach (var raw in patterns.Where(p => !string.IsNullOrWhiteSpace(p)))
            {
                var p = raw.Trim();
                if (p.StartsWith("re:", StringComparison.OrdinalIgnoreCase))
                {
                    var rx = new Regex(p[3..], RegexOptions.IgnoreCase | RegexOptions.Compiled);
                    list.Add(s => rx.IsMatch(s));
                }
                else
                {
                    var esc = Regex.Escape(p).Replace(@"\*", ".*").Replace(@"\?", ".");
                    var rx = new Regex("^" + esc + "$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
                    list.Add(s => rx.IsMatch(s));
                }
            }
            return list;
        }

        // ----- Collections -----

        private BoxSet? FindBoxSetByName(string name)
        {
            var q = new InternalItemsQuery
            {
                IncludeItemTypes = new[] { BaseItemKind.BoxSet }, // WICHTIG: Enum statt string
                Name = name,
                Recursive = true
            };
            var list = _library.GetItemList(q);
            return list.OfType<BoxSet>().FirstOrDefault();
        }

        private async Task<BoxSet?> EnsureCollectionAsync(string collectionName, CancellationToken ct)
        {
            var existing = FindBoxSetByName(collectionName);
            if (existing != null) return existing;

            // Reflection, um Create*/Ensure*-Varianten zu finden
            var cm = _collectionManager;
            var cmType = cm.GetType();
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
                    var task = (Task)method.Invoke(cm, args)!;
                    await task.ConfigureAwait(false);
                    var retProp = method.ReturnType.GetProperty("Result");
                    result = retProp != null ? retProp.GetValue(task) : null;
                }
                else
                {
                    result = method.Invoke(cm, args);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Erstellen der Collection '{Name}' via {Method} fehlgeschlagen.", collectionName, method.Name);
                return null;
            }

            if (result is BoxSet bs) return bs;

            // Falls Methode nichts zurückgibt: danach suchen
            return FindBoxSetByName(collectionName);
        }

        private async Task<bool> AddItemsToCollectionAsync(BoxSet collection, IEnumerable<BaseItem> items, CancellationToken ct)
        {
            var list = items?.Where(i => i != null).ToList() ?? new List<BaseItem>();
            if (list.Count == 0) return true;

            var cm = _collectionManager;
            var cmType = cm.GetType();
            var methods = cmType.GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .Where(m => m.Name.Contains("AddToCollection", StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var m in methods)
            {
                var ps = m.GetParameters();

                // Variante A: (BoxSet, IEnumerable<BaseItem>, ..., CancellationToken?)
                bool matchA = ps.Length >= 2
                    && typeof(BoxSet).IsAssignableFrom(ps[0].ParameterType)
                    && typeof(IEnumerable<BaseItem>).IsAssignableFrom(ps[1].ParameterType);

                // Variante B: (Guid, IEnumerable<Guid>, ..., CancellationToken?)
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
                        var t = (Task)m.Invoke(cm, args)!;
                        await t.ConfigureAwait(false);
                    }
                    else
                    {
                        m.Invoke(cm, args);
                    }
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "AddToCollection via {Method} fehlgeschlagen – versuche nächste Variante.", m.Name);
                }
            }

            _logger.LogWarning("Keine passende AddToCollection*-Methode gefunden – Items nicht hinzugefügt.");
            return false;
        }
    }
}
