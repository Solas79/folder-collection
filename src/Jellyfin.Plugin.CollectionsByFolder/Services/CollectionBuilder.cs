using System;
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

// Hole oder erstelle Collection
var collection = await EnsureCollectionAsync(collName, ct);

// Füge Items hinzu (dedupliziert)
await _collectionManager.AddToCollectionAsync(collection.Id, items.Select(i => i.Id).ToArray());
_logger.LogInformation("Collection '{Name}' aktualisiert: {Count} Einträge", collName, items.Count);
affectedCollections++;
}

return affectedCollections;
}

private async Task<BoxSet> EnsureCollectionAsync(string name, CancellationToken ct)
{
// Versuche vorhandene zu finden
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

private static string NormalizePath(string p)
=> p.Replace('\\', '/');

private static string SafeFolderName(string s)
=> (s ?? string.Empty).Trim();
}
