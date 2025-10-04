using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.CollectionsByFolder.Configuration;
using MediaBrowser.Controller.Collections;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;
using MediaBrowser.Controller.Entities.Movies; // BoxSet
using Jellyfin.Data.Enums;                     // BaseItemKind

namespace Jellyfin.Plugin.CollectionsByFolder.Services;

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
        var roots = cfg.LibraryRoots.Where(r => !string.IsNullOrWhiteSpace(r))
            .Select(NormalizePath).ToArray();
        var blacklist = new HashSet<string>(cfg.Blacklist, StringComparer.OrdinalIgnoreCase);
        int minItems = Math.Max(1, cfg.MinItemsPerFolder);

        var query = new InternalItemsQuery
        {
             IncludeItemTypes = new[] { BaseItemKind.Movie },
             IsVirtualItem = false
        };

        var allMovies = _libraryManager.GetItemList(query).OfType<BaseItem>().ToList();
        _logger.LogInformation("CollectionsByFolder: Gefundene Filme: {Count}", allMovies.Count);

        if (roots.Length > 0)
        {
            allMovies = allMovies
                .Where(m => m.Path is not null && roots.Any(r => NormalizePath(m.Path!).StartsWith(r, StringComparison.OrdinalIgnoreCase)))
                .ToList();
        }

        var groups = allMovies
            .Where(m => !string.IsNullOrEmpty(m.Path))
            .GroupBy(m => SafeFolderName(Path.GetFileName(Path.GetDirectoryName(m.Path!) ?? string.Empty)))
            .Where(g => !string.IsNullOrWhiteSpace(g.Key))
            .ToList();

        int affectedCollections = 0;

        foreach (var grp in groups)
        {
            ct.ThrowIfCancellationRequested();

            var folderName = grp.Key;
            if (blacklist.Contains(folderName))
            {
                _logger.LogDebug("Übersprungen (Blacklist): {Folder}", folderName);
                continue;
            }

            var items = grp.Distinct().ToList();
            if (items.Count < minItems)
            {
                _logger.LogDebug("Übersprungen (zu wenige Items {Count} < {Min}): {Folder}", items.Count, minItems, folderName);
                continue;
            }

            var collName = string.Concat(cfg.Prefix ?? string.Empty, folderName, cfg.Suffix ?? string.Empty);

            var collection = await EnsureCollectionAsync(collName, ct);

            await _collectionManager.AddToCollectionAsync(collection.Id, items.Select(i => i.Id).ToArray());
            _logger.LogInformation("Collection '{Name}' aktualisiert: {Count} Einträge", collName, items.Count);
            affectedCollections++;
        }

        return affectedCollections;
    }

    private async Task<BoxSet> EnsureCollectionAsync(string name, CancellationToken ct)
    {
        var existing = _libraryManager.RootFolder
            .GetRecursiveChildren(i => i is BoxSet)
            .Cast<BoxSet>()

            .FirstOrDefault(b => string.Equals(b.Name, name, StringComparison.OrdinalIgnoreCase));

        if (existing is not null)
            return existing;

        var options = new CollectionCreationOptions
        {
            Name = name
        };
        var created = await _collectionManager.CreateCollection(options).ConfigureAwait(false);
        return created;
    }

    private static string NormalizePath(string p) => p.Replace('\\', '/');

    private static string SafeFolderName(string s) => (s ?? string.Empty).Trim();
}
