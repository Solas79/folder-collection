// src/Jellyfin.Plugin.CollectionsByFolder/PluginConfiguration.cs
using System.Collections.Generic;
using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.CollectionsByFolder
{
    public class PluginConfiguration : BasePluginConfiguration
    {
        // NEU: Whitelist (Scan-Verzeichnisse) – je Zeile ein Pfad
        public List<string> Whitelist { get; set; } = new();

        // ALT (kompatibel bleiben): falls Whitelist leer ist, nutzen wir FolderPaths
        public List<string> FolderPaths { get; set; } = new();

        public string? Prefix { get; set; }
        public string? Suffix { get; set; }

        // NEU: Blacklist je Zeile
        public List<string> Blacklist { get; set; } = new();

        public int MinItemCount { get; set; } = 1;
        public bool EnableDailyScan { get; set; } = false;
        public string ScanTime { get; set; } = "03:00";
    }
}
