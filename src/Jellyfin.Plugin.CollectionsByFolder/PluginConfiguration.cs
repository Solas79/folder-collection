using System.Collections.Generic;
using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.CollectionsByFolder
{
    public class PluginConfiguration : BasePluginConfiguration
    {
        public List<string> FolderPaths { get; set; } = new();
        public string? Prefix { get; set; }
        public string? Suffix { get; set; }
        public List<string> Blacklist { get; set; } = new();
        public int MinItemCount { get; set; } = 1;
        public bool EnableDailyScan { get; set; } = false;
        public string ScanTime { get; set; } = "03:00";
    }
}
