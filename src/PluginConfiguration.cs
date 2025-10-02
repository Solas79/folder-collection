using MediaBrowser.Model.Plugins;

namespace FolderCollections;

public class PluginConfiguration : BasePluginConfiguration
{
    public bool IncludeMovies { get; set; } = false;
    public bool IncludeSeries { get; set; } = false;

    public int MinItems { get; set; } = 2;

    public string? Prefix { get; set; }
    public string? Suffix { get; set; }

    // tägliche Uhrzeit
    public int ScanHour { get; set; } = 4;
    public int ScanMinute { get; set; } = 0;

    // Whitelist der Pfad-Präfixe (eine pro Zeile im UI)
    public string[] PathPrefixes { get; set; } = System.Array.Empty<string>();

    // Regex-Ignore-Patterns (eine pro Zeile im UI)
    public string[] IgnorePatterns { get; set; } = System.Array.Empty<string>();
}
