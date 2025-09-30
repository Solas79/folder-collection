using MediaBrowser.Model.Plugins;
using System.Collections.Generic;

namespace Jellyfin.Plugin.FolderCollections.GUI;

public class PluginConfiguration : BasePluginConfiguration
{
public bool IncludeMovies { get; set; } = true;
public bool IncludeSeries { get; set; } = true;
public List<string> LibraryPathPrefixes { get; set; } = new();
public List<string> IgnorePatterns { get; set; } = new(); // regex
public int MinimumItemsPerFolder { get; set; } = 2;
public bool UseBasenameForCollection { get; set; } = true;
public string NamePrefix { get; set; } = string.Empty;
public string NameSuffix { get; set; } = string.Empty;
}
