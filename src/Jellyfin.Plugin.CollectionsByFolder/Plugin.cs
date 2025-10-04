using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace Jellyfin.Plugin.CollectionsByFolder
{
    public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
    {
        public override string Name => "CollectionsByFolder";
        public override string Description => "Erstellt automatisch Sammlungen nach Ordnernamen.";

        public static Plugin Instance { get; private set; }

        // 10.10: BasePlugin benötigt nur noch IXmlSerializer
        public Plugin(IXmlSerializer xmlSerializer) : base(xmlSerializer)
        {
            Instance = this;
        }

        public IEnumerable<PluginPageInfo> GetPages()
        {
            var ns = GetType().Namespace;
            return new[]
            {
                new PluginPageInfo
                {
                    Name = "collectionsbyfolder",
                    EmbeddedResourcePath = ns + ".Configuration.index.html",
                    Type = PluginPageType.Html,    // iframe-Seite
                    EnableInMainMenu = true
                },
                new PluginPageInfo
                {
                    Name = "collectionsbyfolderjs",
                    EmbeddedResourcePath = ns + ".Configuration.index.js",
                    Type = PluginPageType.Script
                }
            };
        }
    }
}
