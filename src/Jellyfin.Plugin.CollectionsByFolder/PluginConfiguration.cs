using System.Collections.Generic;
using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.CollectionsByFolder
{
    /// <summary>
    /// Persistente Plugin-Einstellungen. Wird automatisch (de)serialisiert.
    /// </summary>
    public class PluginConfiguration : BasePluginConfiguration
    {
        public List<string> Whitelist { get; set; } = new();
        public List<string> Blacklist { get; set; } = new();

        public string Prefix { get; set; } = string.Empty;
        public string Suffix { get; set; } = string.Empty;

        // 👇 Das fehlte dir – Standard 0, damit Builds und Deserialisierung sicher sind.
        public int MinFiles { get; set; } = 0;
    }
}

