using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Collections;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Querying;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.CollectionsByFolder.Services
{
    /// <summary>
    /// Baut/aktualisiert Collections anhand der gespeicherten Plugin-Konfiguration.
    /// </summary>
    public sealed class CollectionBuilder
    {
        private readonly ILibraryManager _library;
        private readonly ICollectionManager _collections;
        private readonly ILogger<CollectionBuilder> _log;

        public CollectionBuilder(
            ILibraryManager library,
            ICollectionManager collections,
            ILogger<CollectionBuilder> log)
        {
            _library = library;
            _collections = collections;
            _log = log;
        }

        public async Task<(int created, int updated)> RunOnceAsync(
            PluginConfiguration cfg,
            CancellationToken ct = default)
        {
            // defensiv
            var wl = (cfg.Whitelist ?? new List<string>())
                .Select(NormalizePathEndSlash)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var bl = (cfg.Blacklist ?? new List<string>())
                .Select(NormalizePathEndSlash)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var min = Math.Max(1, cfg.MinFiles);
            var prefix = cfg.Prefix ?? string.Empty;
            var suffix = cfg.Suffix ?? string.Empty;

            _log.LogInformation("[CBF] Scan start: WL={W} BL={B} Min={Min} Prefix='{P}' Suffix='{S}'",
                wl.Count, bl.Count, min, prefix, suffix);

            // 1) Alle Movies holen (mit Pfaden)
            var movies = _library.GetItemList(new InternalItemsQuery
            {
                IncludeItemTypes = new[] { nameof(Movie) },
                Recursive = true,
                DtoOptions = new DtoOptions(true)
                {
                    Fields = new[] { ItemFields.Path }
                }
            }).OfType<Movie>().ToList();

            _log.LogInformation("[CBF] Movies gesamt: {N}", movies.Count);

            // 2) Whitelist/Blacklist-Filter
            bool IsInWhitelist(string path) =>
                wl.Count == 0 || wl.Any(w => path.StartsWith(w, StringComparison.OrdinalIgnoreCase));
            bool IsInBlacklist(string path) =>
                bl.Any(b => path.StartsWith(b, StringComparison.OrdinalIgnoreCase));

            var filtered = movies.Where(m =>
            {
                var p = m.Path ?? string.Empty;
                return IsInWhitelist(p) && !IsInBlacklist(p);
            }).ToList();

            _log.LogInformation("[CBF] Movies nach WL/BL: {N}", filtered.Count);

            // 3) Nach Parent-Ordner gruppieren
            var groups = filtered
                .GroupBy(m =>
                {
                    var dir = Path.GetDirectoryName(m.Path) ?? string.Empty;
                    return dir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                })
                .Where(g => g.Any())
                .ToList();

            _log.LogInformation("[CBF] Ordner-Gruppen gesamt: {N}", groups.Count);

            int created = 0, updated = 0;

            // 4) Pro Ordner ggf. Collection anlegen/auffüllen
            foreach (var g in groups)
            {
                ct.ThrowIfCancellationRequested();

                var dirPath = g.Key;
                var itemCount = g.Count();

                if (itemCount < min)
                {
                    _log.LogDebug("[CBF] Skip '{Dir}' (Count={C} < Min={Min})", dirPath, itemCount, min);
                    continue;
                }

                var folderName = Path.GetFileName(dirPath);
                if (string.IsNullOrWhiteSpace(folderName))
                {
                    _log.LogDebug("[CBF] Skip '{Dir}' (kein Ordnername)", dirPath);
                    continue;
                }

                var name = $"{prefix}{folderName}{suffix}";
                var items = g.Cast<BaseItem>().ToList();

                // Gibt es bereits ein BoxSet mit dem Namen?
                var existing = _library.GetItemList(new InternalItemsQuery
                {
                    IncludeItemTypes = new[] { nameof(BoxSet) },
                    Name = name
                }).OfType<BoxSet>().FirstOrDefault();

                if (existing == null)
                {
                    // Neu anlegen
                    _log.LogInformation("[CBF] Create Collection '{Name}' (Items={C})", name, items.Count);
                    var box = await _collections.CreateCollection(name, items, null).ConfigureAwait(false);
                    created++;
                }
                else
                {
                    // Auffüllen (nur fehlende hinzufügen)
                    var existingIds = new HashSet<Guid>(
                        existing.GetLinkedChildren().Select(x => x.Id));
                    var toAdd = items.Where(x => !existingIds.Contains(x.Id)).ToList();

                    if (toAdd.Count > 0)
                    {
                        _log.LogInformation("[CBF] Update Collection '{Name}' (+{C} Items)", name, toAdd.Count);
                        await _collections.AddToCollection(existing, toAdd).ConfigureAwait(false);
                        updated++;
                    }
                    else
                    {
                        _log.LogDebug("[CBF] Collection '{Name}' ist bereits aktuell.", name);
                    }
                }
            }

            _log.LogInformation("[CBF] Scan fertig: created={Cr} updated={Up}", created, updated);
            return (created, updated);
        }

        private static string NormalizePathEndSlash(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return string.Empty;
            s = s.Trim();
            if (!s.EndsWith(Path.DirectorySeparatorChar) && !s.EndsWith(Path.AltDirectorySeparatorChar))
                s += Path.DirectorySeparatorChar;
            return s;
        }
    }
}
