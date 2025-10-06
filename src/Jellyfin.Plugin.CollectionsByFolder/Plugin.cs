using System;
using System.Collections.Generic;
using System.Globalization;
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

        public IEnumerable<PluginPageInfo> GetPages()
        {
            var ns = typeof(Plugin).Namespace!;
            const string JsRoute = "cbf_js_20251006"; // eindeutiger, neuer Name

            return new[]
            {
                new PluginPageInfo
                {
                    Name = "collectionsbyfolder", // /web/configurationpage?name=collectionsbyfolder
                    EmbeddedResourcePath = $"{ns}.Web.collectionsbyfolder.html"
                },
                new PluginPageInfo
                {
                    Name = JsRoute,               // -> /web/cbf_js_20251006
                    EmbeddedResourcePath = $"{ns}.Web.collectionsbyfolder.js"
                }
            };
        }

    }
}
