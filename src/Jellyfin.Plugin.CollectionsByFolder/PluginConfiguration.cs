using System.Collections.Generic;
using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.CollectionsByFolder
{
    public class PluginConfiguration : BasePluginConfiguration
    {
        // Whitelist (Scan-Verzeichnisse), ein Pfad pro Zeile (in UI)
        public List<string> Whitelist { get; set; } = new();

        // Fallback: alte Eigenschaft – wird genutzt, wenn Whitelist leer bleibt
        public List<string> FolderPaths { get; set; } = new();

        // Blacklist: Ordnernamen oder Teilstrings (ein Eintrag pro Zeile in UI)
        public List<string> Blacklist { get; set; } = new();

        public string? Prefix { get; set; }
        public string? Suffix { get; set; }

        public int MinItemCount { get; set; } = 1;

        public bool EnableDailyScan { get; set; } = false;
        public string ScanTime { get; set; } = "03:00";
    }
}
