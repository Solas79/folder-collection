using System;
using System.Collections.Generic;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace Jellyfin.Plugin.CollectionsByFolder
{
    public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
    {
        public static Plugin Instance { get; private set; } = null!;

        public override string Name => "CollectionsByFolder";
        public override string Description => "Erstellt automatisch Sammlungen nach Ordnernamen.";
        public override Guid Id => Guid.Parse("f58f3a40-6a8a-48e8-9b3a-9d7f0b6a3a41");

        public Plugin(IApplicationPaths paths, IXmlSerializer xml)
            : base(paths, xml)
        {
            Instance = this;
        }

        public IEnumerable<PluginPageInfo> GetPages()
        {
            var ns = typeof(Plugin).Namespace!;

            // Nur die HTML-Konfigseite über den offiziellen configurationpage-Endpunkt.
            // Das JS wird NICHT über /web/<name> geladen, sondern über den Controller:
            //   <script src="../Plugins/CollectionsByFolder/js"></script>
            // (siehe Controllers/StaticController.cs)
            return new[]
            {
                new PluginPageInfo
                {
                    // -> /web/configurationpage?name=collectionsbyfolder
                    Name = "collectionsbyfolder",
                    EmbeddedResourcePath = $"{ns}.Web.collectionsbyfolder.html"
                }
            };
        }
    }
}
