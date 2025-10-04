using System;
using System.Collections.Generic;
using MediaBrowser.Common.Configuration;   // IApplicationPaths
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
            var ns = GetType().Namespace; // "Jellyfin.Plugin.CollectionsByFolder"
            return new[]
            {
                new PluginPageInfo
                {
                    Name = "collectionsbyfolder",                           // URL: ...?name=collectionsbyfolder
                    EmbeddedResourcePath = ns + ".Configuration.index.html",
                    EnableInMainMenu = true
                },
                new PluginPageInfo
                {
                    Name = "collectionsbyfolderjs",                         // MUSS mit <script src="..."> matchen
                    EmbeddedResourcePath = ns + ".Configuration.index.js"
                }
            };
        }
    }
}
