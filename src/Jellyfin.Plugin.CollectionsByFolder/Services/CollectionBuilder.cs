using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;                        // BaseItemKind
using MediaBrowser.Controller.Collections;        // ICollectionManager, CollectionCreationOptions
using MediaBrowser.Controller.Entities;           // BaseItem
using MediaBrowser.Controller.Entities.Movies;    // BoxSet
using MediaBrowser.Controller.Library;            // ILibraryManager, InternalItemsQuery
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.CollectionsByFolder.Services
{
    public class CollectionBuilder
    {
        private readonly ILibraryManager _libraryManager;
        private readonly ICollectionManager _collectionManager;
        private readonly ILogger<CollectionBuilder> _logger;

        public CollectionBuilder(
            ILibraryManager libraryManager,
            ICollectionManager collectionManager,
            ILogger<CollectionBuilder> logger)
        {
            _libraryManager = libraryManager;
            _collectionManager = collectionManager;
            _logger = logger;
        }

        public async Task<int> RunAsync(PluginConfiguration cfg, CancellationToken ct)
        {
            // Eingaben vorbereiten
            var roots = (Plugin.Instance.Configuration.FolderPaths != null && Plugin.Instance.Configuration.FolderPaths.Count > 0)
            ? Plugin.Instance.Configuration.FolderPaths
            : new List<string>();


            var blacklist = new HashSet<string>(cfg.Blacklist ?? Array.Empty<string>(),
                                                StringComparer.OrdinalIgnoreCase);

            var minItems = Math.Max(1, cfg.MinItemCount);

            // Alle Filme aus der Library (optional auf Wurzelpfade filtern)
            var query = new InternalItemsQuery
            {
                IncludeItemTypes = new[] { BaseItemKind.Movie },
                IsVirtualItem = false
            };

            var allMovies = _libraryManager.GetItemList(query).OfType<BaseItem>().ToList();
            _logger.LogInformation("[CollectionsByFolder] Filme gesamt: {Count}", allMovies.Count);

            if (roots.Length > 0)
            {
                allMovies = allMovies
                    .Where(m => m.Path is not null &&
                                roots.Any(r => NormalizePath(m.Path!).StartsWith(r, StringComparison.OrdinalIgnoreCase)))
                    .ToList();
            }

            // Gruppieren nach letztem Ordnernamen
            var groups = allMovies
                .Where(m => !string.IsNullOrEmpty(m.Path))
                .GroupBy(m =>
                {
                    var parent = Path.GetDirectoryName(m.Path!) ?? string.Empty;
                    return SafeFolderName(Path.GetFileName(parent));
                })
                .Where(g => !string.IsNullOrWhiteSpace(g.Key))
                .ToList();

            int updated = 0;

            foreach (var grp in groups)
            {
                ct.ThrowIfCancellationRequested();

                var folderName = grp.Key;

                if (blacklist.Contains(folderName))
                {
                    _logger.LogDebug("[CollectionsByFolder] Übersprungen (Blacklist): {Folder}", folderName);
                    continue;
                }

                var items = grp.Distinct().ToList();

                if (items.Count < minItems)
                {
                    _logger.LogDebug("[CollectionsByFolder] Übersprungen (zu wenige Items {Count} < {Min}): {Folder}",
                        items.Count, minItems, folderName);
                    continue;
                }

                var collectionName = string.Concat(cfg.Prefix ?? string.Empty, folderName, cfg.Suffix ?? string.Empty);

                var collection = await EnsureCollectionAsync(collectionName).ConfigureAwait(false);

                await _collectionManager.AddToCollectionAsync(collection.Id, items.Select(i => i.Id).ToArray())
                                        .ConfigureAwait(false);

                _logger.LogInformation("[CollectionsByFolder] Collection '{Name}' aktualisiert: {Count} Einträge",
                    collectionName, items.Count);
                updated++;
            }

            return updated;
        }

        private async Task<BoxSet> EnsureCollectionAsync(string name)
        {
            var existing = _libraryManager.RootFolder
                .GetRecursiveChildren(i => i is BoxSet)
                .Cast<BoxSet>()
                .FirstOrDefault(b => string.Equals(b.Name, name, StringComparison.OrdinalIgnoreCase));

            if (existing is not null)
                return existing;

            var options = new CollectionCreationOptions { Name = name };
            var created = await _collectionManager.CreateCollectionAsync(options).ConfigureAwait(false);
            return created;
        }

        private static string NormalizePath(string p) => (p ?? string.Empty).Replace('\\', '/').Trim();
        private static string SafeFolderName(string s) => (s ?? string.Empty).Trim();
    }
}
