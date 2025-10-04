using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.Plugin.CollectionsByFolder.Services
{
    public class CollectionBuilder
    {
        public sealed class FolderCandidate
        {
            public string Root { get; init; } = string.Empty;
            public string FolderPath { get; init; } = string.Empty;
            public string CollectionName { get; init; } = string.Empty;
            public int ItemCount { get; init; }
        }

        public async Task<IReadOnlyList<FolderCandidate>> BuildCollectionsAsync(CancellationToken ct = default)
        {
            var cfg = Plugin.Instance.Configuration;

            // Whitelist bevorzugen, sonst FolderPaths
            var roots = (cfg.Whitelist?.Count > 0 ? cfg.Whitelist : cfg.FolderPaths) ?? new List<string>();
            var blacklist = cfg.Blacklist ?? new List<string>();

            if (roots.Count == 0)
                return Array.Empty<FolderCandidate>();

            var normalizedRoots = NormalizePaths(roots);
            var normalizedBlacklist = NormalizeList(blacklist);

            var minItems = cfg.MinItemCount <= 0 ? 1 : cfg.MinItemCount;
            var prefix = cfg.Prefix ?? string.Empty;
            var suffix = cfg.Suffix ?? string.Empty;

            var results = new List<FolderCandidate>();

            foreach (var root in normalizedRoots)
            {
                ct.ThrowIfCancellationRequested();
                if (!Directory.Exists(root))
                    continue;

                foreach (var dir in SafeEnumDirectories(root))
                {
                    ct.ThrowIfCancellationRequested();

                    var lastName = new DirectoryInfo(dir).Name;
                    if (IsBlacklisted(lastName, normalizedBlacklist))
                        continue;

                    var itemCount = SafeCountMediaFiles(dir);
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

            await Task.CompletedTask;
            return results;
        }

        public static string BuildCollectionName(string folderName, string prefix, string suffix)
        {
            var p = string.IsNullOrWhiteSpace(prefix) ? string.Empty : prefix.Trim();
            var s = string.IsNullOrWhiteSpace(suffix) ? string.Empty : suffix.Trim();

            if (!string.IsNullOrEmpty(p) && !p.EndsWith(" ", StringComparison.Ordinal))
                p += " ";
            if (!string.IsNullOrEmpty(s) && !s.StartsWith(" ", StringComparison.Ordinal))
                s = " " + s;

            return $"{p}{folderName}{s}";
        }

        private static List<string> NormalizePaths(List<string> roots)
            => roots.Where(p => !string.IsNullOrWhiteSpace(p))
                    .Select(p => p.Trim())
                    .Select(NormalizeOnePath)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

        private static string NormalizeOnePath(string path)
        {
            var normalized = path.Replace('\\', Path.DirectorySeparatorChar)
                                 .Replace('/', Path.DirectorySeparatorChar)
                                 .Trim();
            if (normalized.Length > 2 && normalized.EndsWith(Path.DirectorySeparatorChar))
                normalized = normalized.TrimEnd(Path.DirectorySeparatorChar);
            return normalized;
        }

        private static List<string> NormalizeList(List<string> list)
            => list.Where(s => !string.IsNullOrWhiteSpace(s))
                   .Select(s => s.Trim())
                   .Distinct(StringComparer.OrdinalIgnoreCase)
                   .ToList();

        private static bool IsBlacklisted(string folderName, List<string> blacklist)
            => blacklist.Any(b =>
                   folderName.Equals(b, StringComparison.OrdinalIgnoreCase) ||
                   folderName.Contains(b, StringComparison.OrdinalIgnoreCase));

        private static IEnumerable<string> SafeEnumDirectories(string root)
        {
            try { return Directory.EnumerateDirectories(root); }
            catch { return Array.Empty<string>(); }
        }

        private static int SafeCountMediaFiles(string folder)
        {
            try { return Directory.EnumerateFiles(folder, "*", SearchOption.TopDirectoryOnly).Count(); }
            catch { return 0; }
        }
    }
}
