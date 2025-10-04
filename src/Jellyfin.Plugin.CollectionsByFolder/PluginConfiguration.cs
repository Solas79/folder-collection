using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.CollectionsByFolder
{
    public class PluginConfiguration : BasePluginConfiguration
    {
        public string[] FolderPaths { get; set; } = [];
        public string Prefix { get; set; } = string.Empty;
        public string Suffix { get; set; } = string.Empty;
        public string[] Blacklist { get; set; } = [];
        public int MinItemCount { get; set; } = 1;
        public bool EnableDailyScan { get; set; } = false;
        public string ScanTime { get; set; } = "00:00";
    }
}
