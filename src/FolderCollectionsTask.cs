using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Collections;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.FolderCollections.GUI;

public class FolderCollectionsTask : IScheduledTask, IPluginInstance
{
private readonly ILibraryManager _library;
private readonly ICollectionManager _collections;
private readonly ILogger<FolderCollectionsTask> _logger;

public FolderCollectionsTask(ILibraryManager library, ICollectionManager collections, ILogger<FolderCollectionsTask> logger)
{
_library = library;
_collections = collections;
_logger = logger;
}

public string Name => "Folder Collections (per directory)";
public string Description => "Create/Update BoxSets based on parent folders.";
public string Category => "Library";

public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
{
// täglich um 04:00
return new[] { new TaskTriggerInfo { Type = TaskTriggerInfo.TriggerDaily, TimeOfDayTicks = TimeSpan.FromHours(4).Ticks } };
}

public async Task Execute(CancellationToken cancellationToken, IProgress<double> progress)
{
var plugin = (Plugin)Plugin.Instance!;
var cfg = plugin.Configuration;
_logger.LogInformation("[FolderCollections] Start. IncludeMovies={Movies}, IncludeSeries={Series}", cfg.IncludeMovies, cfg.IncludeSeries);

var ignore = cfg.IgnorePatterns
.Where(s => !string.IsNullOrWhiteSpace(s))
.Select(s => new Regex(s, RegexOptions.IgnoreCase | RegexOptions.Compiled))
.ToList();

// Alle Bibliotheken durchsuchen
var roots = _library.GetUserRootFolder();
var items = new List<BaseItem>();

var allowedTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
if (cfg.IncludeMovies) allowedTypes.Add("Movie");
if (cfg.IncludeSeries) allowedTypes.Add("Series");

foreach (var lib in roots.GetChildren())
{
// nur echte Bibliotheken (Folder)
var q = lib.GetRecursiveChildren()
.Where(i => allowedTypes.Contains(i.GetType().Name));
items.AddRange(q);
}

// Gruppieren nach Parent-Folder
var groups = new Dictionary<string, List<BaseItem>>(StringComparer.OrdinalIgnoreCase);
}
