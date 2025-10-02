public class PluginConfiguration : BasePluginConfiguration
{
    public bool IncludeMovies { get; set; } = false;
    public bool IncludeSeries { get; set; } = false;
    public int MinItems { get; set; } = 2;
    public string? Prefix { get; set; }
    public string? Suffix { get; set; }
    public int ScanHour { get; set; } = 4;
    public int ScanMinute { get; set; } = 0;  // <-- ergänzt
    public string[] PathPrefixes { get; set; } = Array.Empty<string>();
    public string[] IgnorePatterns { get; set; } = Array.Empty<string>();
}
