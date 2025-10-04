using System.Collections.Generic;
using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.CollectionsByFolder
{
    public class PluginConfiguration : BasePluginConfiguration
    {
        public List<string> FolderPaths { get; set; } = new();     // mehrere Pfade
        public string? Prefix { get; set; }                         // Präfix
        public string? Suffix { get; set; }                         // Suffix
        public List<string> Blacklist { get; set; } = new();        // Blacklist
        public int MinItemCount { get; set; } = 1;                  // Mindestanzahl
        public bool EnableDailyScan { get; set; } = false;          // täglicher Scan aktiv
        public string ScanTime { get; set; } = "03:00";             // Uhrzeit (HH:mm)
    }
}
