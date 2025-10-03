using System;

namespace FolderCollections
{
    public class PluginConfiguration
    {
        public bool IncludeMovies { get; set; } = true;
        public bool IncludeSeries { get; set; } = false;
        public int  MinItems { get; set; } = 2;

        public string? Prefix { get; set; } = "";
        public string? Suffix { get; set; } = "";

        public int ScanHour   { get; set; } = 4;
        public int ScanMinute { get; set; } = 0;

        public string[] PathPrefixes   { get; set; } = Array.Empty<string>();
        public string[] IgnorePatterns { get; set; } = Array.Empty<string>();

        public bool UseBasenameAsCollectionName { get; set; } = true;
    }
}
