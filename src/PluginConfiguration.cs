using System;
using MediaBrowser.Model.Plugins; // <- WICHTIG: Hier kommt BasePluginConfiguration her

namespace FolderCollections
{
    public class PluginConfiguration : BasePluginConfiguration
    {
        public bool IncludeMovies { get; set; } = true;
        public bool IncludeSeries { get; set; } = true;

        public int MinItems { get; set; } = 2;

        public string? Prefix { get; set; }
        public string? Suffix { get; set; }

        // Checkbox "Basename als Sammlungsname"
        public bool UseBasenameAsCollectionName { get; set; } = true;

        public int ScanHour   { get; set; } = 4;
        public int ScanMinute { get; set; } = 0;

        public string[] PathPrefixes   { get; set; } = Array.Empty<string>();
        public string[] IgnorePatterns { get; set; } = Array.Empty<string>();
    }
}
