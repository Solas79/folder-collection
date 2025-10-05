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

        public Plugin(IApplicationPaths paths, IXmlSerializer xml) : base(paths, xml)
        {
            Instance = this;
        }

        // Nur die HTML-Seite registrieren – keine JS-Seite mehr.
        public IEnumerable<PluginPageInfo> GetPages() => new[]
        {
            new PluginPageInfo
            {
                Name = "collectionsbyfolder", // /web/configurationpage?name=collectionsbyfolder
                EmbeddedResourcePath = "Jellyfin.Plugin.CollectionsByFolder.configPage.html"
            }
        };
    }
}
