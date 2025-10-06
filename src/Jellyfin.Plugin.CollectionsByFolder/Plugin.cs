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

        public Plugin(IApplicationPaths paths, IXmlSerializer xml) : base(paths, xml) => Instance = this;

        public IEnumerable<PluginPageInfo> GetPages()
        {
            var ns = GetType().Namespace!;
            return new[]
            {
                // HTML → /web/configurationpage?name=collectionsbyfolder
                new PluginPageInfo
                {
                    Name = "collectionsbyfolder",
                    EmbeddedResourcePath = string.Format(CultureInfo.InvariantCulture,
                        "{0}.Web.configPage.html", ns)
                },
                // JS → /web/configPage.js   (wie im Beispiel: Name enthält ".js")
                new PluginPageInfo
                {
                    Name = "configPage.js",
                    EmbeddedResourcePath = string.Format(CultureInfo.InvariantCulture,
                        "{0}.Web.configPage.js", ns)
                }
            };
        }
    }
}
