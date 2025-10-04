// Datei: src/Jellyfin.Plugin.CollectionsByFolder/Services/CollectionBuilder.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.Plugin.CollectionsByFolder.Services
{
    /// <summary>
    /// Kümmert sich um das Einlesen/Normalisieren der Plugin-Konfiguration
    /// und liefert die Kandidaten-Ordner (pro Library-Root).
    /// Die eigentliche Erstellung/Aktualisierung der Collections
    /// kannst du in BuildCollectionsAsync() an Jellyfin anbinden.
    /// </summary>
    public class CollectionBuilder
    {
        public sealed class FolderCandidate
        {
            public string Root { get; init; } = string.Empty;
            public string FolderPath { get; init; } = string.Empty;
            public string CollectionName { get; init; } = string.Empty;
            public int ItemCount { get; init; }
        }

        /// <summary>
        /// Einstieg: scannt alle konfigurierten Roots und gibt die geplanten Collections zurück.
        /// </summary>
        public async Task<IReadOnlyList<FolderCandidate>> BuildCollectionsAsync(CancellationToken ct = default)
        {
            var cfg = Plugin.Instance.Configuration;

            // ---- Typen richtig coalescen: immer List<string> auf beiden Seiten ----
            var roots = cfg.FolderPaths ?? new List<string>();
            var blacklist = cfg.Blacklist ?? new List<string>();

            // Leer? -> nichts zu tun
            if (roots.Count == 0)
                return Array.Empty<FolderCandidate>();

            // Normalisierung
            var normalizedRoots = NormalizePaths(roots);
            var normalizedBlacklist = NormalizeBlacklist(blacklist);

            var minItems = cfg.MinItemCount <= 0 ? 1 : cfg.MinItemCount;
            var prefix = cfg.Prefix ?? string.Empty;
            var suffix = cfg.Suffix ?? string.Empty;

            var results = new List<FolderCandidate>();

            foreach (var root in normalizedRoots)
            {
                ct.ThrowIfCancellationRequested();

                if (!Directory.Exists(root))
                    continue;

                // nur direkte Unterordner des Root betrachten
                IEnumerable<string> subdirs = SafeEnumDirectories(root);
                foreach (var dir in subdirs)
                {
                    ct.ThrowIfCancellationRequested();

                    var lastName = new DirectoryInfo(dir).Name;

                    // Blacklist-Filter (exakt oder contains, je nach Bedarf)
                    if (IsBlacklisted(lastName, normalizedBlacklist))
                        continue;

                    // Zähle Dateien (rudimentär; passe ggf. auf Mediendateien an)
                    int itemCount = SafeCountMediaFiles(dir);
                    if (itemCount < minItems)
                        continue;

                    var collName = BuildCollectionName(lastName, prefix, suffix);

                    results.Add(new FolderCandidate
                    {
                        Root = root,
                        FolderPath = dir,
                        ItemCount = itemCount,
                        CollectionName = collName
                    });
                }
            }

            // künstlich async, falls du später IO-gebundene Aufgaben einhängst
            await Task.CompletedTask;
            return results;
        }

        /// <summary>
        /// Aus "Ordner" + Präfix/Suffix wird der endgültige Collection-Name.
        /// </summary>
        public static string BuildCollectionName(string folderName, string prefix, string suffix)
        {
            var p = string.IsNullOrWhiteSpace(prefix) ? string.Empty : prefix.Trim();
            var s = string.IsNullOrWhiteSpace(suffix) ? string.Empty : suffix.Trim();

            // Leerzeichen sauber setzen
            if (!string.IsNullOrEmpty(p) && !p.EndsWith(" ", StringComparison.Ordinal))
                p += " ";
            if (!string.IsNullOrEmpty(s) && !s.StartsWith(" ", StringComparison.Ordinal))
                s = " " + s;

            return $"{p}{folderName}{s}";
        }

        private static List<string> NormalizePaths(List<string> roots)
        {
            // trims + entfernt Duplikate + normalisiert Directory-Separators
            return roots
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Select(p => p.Trim())
                .Select(NormalizeOnePath)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static string NormalizeOnePath(string path)
        {
            // vereinheitliche auf OS-Separator
            var normalized = path.Replace('\\', Path.DirectorySeparatorChar)
                                 .Replace('/', Path.DirectorySeparatorChar)
                                 .Trim();

            // ohne trailing Separator (außer Root-Laufwerk)
            if (normalized.Length > 2 && normalized.EndsWith(Path.DirectorySeparatorChar))
                normalized = normalized.TrimEnd(Path.DirectorySeparatorChar);

            return normalized;
        }

        private static List<string> NormalizeBlacklist(List<string> blacklist)
        {
            return blacklist
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static bool IsBlacklisted(string folderName, List<string> blacklist)
        {
            // Exakter Match ODER Teilstring – passe nach Bedarf an
            return blacklist.Any(b =>
                folderName.Equals(b, StringComparison.OrdinalIgnoreCase) ||
                folderName.Contains(b, StringComparison.OrdinalIgnoreCase));
        }

        private static IEnumerable<string> SafeEnumDirectories(string root)
        {
            try
            {
                return Directory.EnumerateDirectories(root);
            }
            catch
            {
                return Array.Empty<string>();
            }
        }

        private static int SafeCountMediaFiles(string folder)
        {
            try
            {
                // Minimal: alle Dateien zählen. Optional: Filter auf bekannte Media-Extensions.
                return Directory.EnumerateFiles(folder, "*", SearchOption.TopDirectoryOnly).Count();
            }
            catch
            {
                return 0;
            }
        }
    }
}
