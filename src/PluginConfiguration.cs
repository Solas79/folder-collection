using System.Collections.Generic;
using MediaBrowser.Model.Plugins;

namespace FolderCollections
{
    public sealed class PluginConfiguration : BasePluginConfiguration
    {
        public List<string> IncludeFolders { get; set; } = new();
        public List<string> ExcludeFolders { get; set; } = new();

        public string? CollectionNamePrefix { get; set; } = "";
        public string? CollectionNameSuffix { get; set; } = "";

        public int  MinItemsPerFolder { get; set; } = 2;

        public bool   EnableDailyScan  { get; set; } = false;
        public string DailyScanTime    { get; set; } = "03:00";
        public int    DailyScanHour    { get; set; } = 3;   // Back-Compat
        public int    DailyScanMinute  { get; set; } = 0;   // Back-Compat
    }
}
