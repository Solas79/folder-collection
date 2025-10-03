using MediaBrowser.Model.Plugins;

namespace FolderCollections
{
    public class PluginConfiguration : BasePluginConfiguration
    {
        public string? RootPath { get; set; }
        public bool Recursive { get; set; } = true;

        // Platz für weitere Optionen
        public string? CollectionNamePrefix { get; set; } = "FC:";
    }
}
