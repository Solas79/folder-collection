// src/PluginConfiguration.cs
using MediaBrowser.Model.Plugins;

namespace FolderCollections;

public class PluginConfiguration : BasePluginConfiguration
{
    public bool IncludeMovies { get; set; } = false;
    public bool IncludeSeries { get; set; } = false;
    public int MinItems { get; set; } = 2;
    public string? Prefix { get; set; }
    public string? Suffix { get; set; }
    public int ScanHour { get; set; } = 4;
    public int ScanMinute { get; set; } = 0;
    public string[] PathPrefixes { get; set; } = System.Array.Empty<string>();
    public string[] IgnorePatterns { get; set; } = System.Array.Empty<string>();
}
