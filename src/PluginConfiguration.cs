using System.Collections.Generic;
using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.FolderCollections.GUI
{
    /// <summary>
    /// Konfiguration für das Folder-Collections-Plugin.
    /// </summary>
    public class PluginConfiguration : BasePluginConfiguration
    {
        /// <summary>
        /// Filme berücksichtigen.
        /// </summary>
        public bool IncludeMovies { get; set; } = true;

        /// <summary>
        /// Serien berücksichtigen.
        /// </summary>
        public bool IncludeSeries { get; set; } = true;

        /// <summary>
        /// Whitelist: Nur Pfade, die mit einem dieser Präfixe beginnen, werden berücksichtigt.
        /// Leer = keine Einschränkung.
        /// </summary>
        public List<string> LibraryPathPrefixes { get; set; } = new();

        /// <summary>
        /// Blacklist: Regex-Muster. Pfade, die auf eines davon matchen, werden ignoriert.
        /// </summary>
        public List<string> IgnorePatterns { get; set; } = new();

        /// <summary>
        /// Mindestanzahl an Items pro Ordner, damit eine Collection erzeugt wird.
        /// </summary>
        public int MinimumItemsPerFolder { get; set; } = 2;

        /// <summary>
        /// Wenn true, wird der Collection-Name aus dem Ordnernamen (Basename) gebildet;
        /// sonst kann z. B. der gesamte Pfad verwendet werden (aktueller Code nutzt Basename).
        /// </summary>
        public bool UseBasenameForCollection { get; set; } = true;

        /// <summary>
        /// Optionaler Prefix für den Collection-Namen.
        /// </summary>
        public string NamePrefix { get; set; } = string.Empty;

        /// <summary>
        /// Optionaler Suffix für den Collection-Namen.
        /// </summary>
        public string NameSuffix { get; set; } = string.Empty;

        /// <summary>
        /// Uhrzeit (Stunde, 0–23) für den täglichen Scan-Trigger.
        /// </summary>
        public int ScanHour { get; set; } = 4;

        /// <summary>
        /// Uhrzeit (Minute, 0–59) für den täglichen Scan-Trigger.
        /// </summary>
        public int ScanMinute { get; set; } = 0;
    }
}
