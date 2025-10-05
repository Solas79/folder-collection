using System.Collections.Generic;
using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.CollectionsByFolder
{
    /// <summary>
    /// Persistente Plugin-Einstellungen. Wird von Jellyfin (de)serialisiert.
    /// </summary>
    public class PluginConfiguration : BasePluginConfiguration
    {
        public List<string> Whitelist { get; set; } = new();
        public List<string> Blacklist { get; set; } = new();

        public string Prefix { get; set; } = string.Empty;
        public string Suffix { get; set; } = string.Empty;

        public int MinFiles { get; set; } = 0;

        // Für bestehenden Code (z. B. CollectionBuilder) häufig verwendet:
        public List<string> FolderPaths { get; set; } = new();
    }
}
