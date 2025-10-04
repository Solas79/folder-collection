using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.IO; // <- wichtig für IApplicationPaths

namespace Jellyfin.Plugin.CollectionsByFolder
{
    public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
    {
        public override string Name => "CollectionsByFolder";
        public override string Description => "Erstellt automatisch Sammlungen nach Ordnernamen.";

        public static Plugin Instance { get; private set; }

        public Plugin(IApplicationPaths appPaths, IXmlSerializer xmlSerializer)
            : base(appPaths, xmlSerializer)
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
                    Type = PluginPageType.Html,     // iframe-Seite
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
