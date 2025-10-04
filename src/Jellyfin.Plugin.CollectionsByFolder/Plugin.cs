using System;
using System.Collections.Generic;
using System.Linq;
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

        public Plugin(IApplicationPaths appPaths, IXmlSerializer xmlSerializer)
            : base(appPaths, xmlSerializer)
        {
            Instance = this;
        }

        public IEnumerable<PluginPageInfo> GetPages()
        {
            // Reale (eingebettete) Ressourcennamen ermitteln – robust gegen RootNamespace/Ordnerabweichungen
            var names = GetType().Assembly.GetManifestResourceNames();

            string? html = names.FirstOrDefault(n =>
                n.EndsWith(".Configuration.index.html", StringComparison.OrdinalIgnoreCase));
            string? js = names.FirstOrDefault(n =>
                n.EndsWith(".Configuration.index.js", StringComparison.OrdinalIgnoreCase));

            // Fallbacks (wenn was schief eingebettet wurde)
            html ??= "Jellyfin.Plugin.CollectionsByFolder.Configuration.index.html";
            js   ??= "Jellyfin.Plugin.CollectionsByFolder.Configuration.index.js";

            return new[]
            {
                new PluginPageInfo
                {
                    Name = "collectionsbyfolder",
                    EmbeddedResourcePath = html,
                    EnableInMainMenu = true
                },
                new PluginPageInfo
                {
                    Name = "collectionsbyfolderjs",
                    EmbeddedResourcePath = js
                }
            };
        }
    }
}
