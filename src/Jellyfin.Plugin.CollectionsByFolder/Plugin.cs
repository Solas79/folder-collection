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

        public Plugin(IApplicationPaths appPaths, IXmlSerializer xmlSerializer)
            : base(appPaths, xmlSerializer)
        {
            Instance = this;
        }

        public IEnumerable<PluginPageInfo> GetPages() => new[]
        {
            new PluginPageInfo
            {
                // /web/collectionsbyfolder  → HTML
                Name = "collectionsbyfolder",
                EmbeddedResourcePath = "Jellyfin.Plugin.CollectionsByFolder.configPage.html"
            },
            new PluginPageInfo
            {
                // /web/collectionsbyfolderjs → JS
                Name = "collectionsbyfolderjs",
                EmbeddedResourcePath = "Jellyfin.Plugin.CollectionsByFolder.configPage.js"
            }    
        };

    public Plugin(IApplicationPaths appPaths, IXmlSerializer xmlSerializer)
        : base(appPaths, xmlSerializer)
    {
        Instance = this;

        // 🔍 DEBUG: eingebettete Ressourcen auflisten
        var all = typeof(Plugin).Assembly.GetManifestResourceNames();
        foreach (var name in all)
        {
            Console.WriteLine("[CBF] Resource: " + name);
        }
    }


    }
}
