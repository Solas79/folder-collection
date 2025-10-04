using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using System;
using System.Collections.Generic;

namespace Jellyfin.Plugin.CollectionsByFolder
{
    public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
    {
        // Hinweis-Warnung entfernt (Non-nullable) – wird im Ctor gesetzt.
        public static Plugin Instance { get; private set; } = null!;

        public override string Name => "CollectionsByFolder";
        public override string Description => "Erstellt/aktualisiert Collections aus den letzten Ordnernamen.";
        public override Guid Id => Guid.Parse("f58f3a40-6a8a-48e8-9b3a-9d7f0b6a3a41");

        public Plugin(IApplicationPaths appPaths, IXmlSerializer xmlSerializer)
            : base(appPaths, xmlSerializer)
        {
            Instance = this;
        }

        public IEnumerable<PluginPageInfo> GetPages()
        {
            return new[]
            {
                new PluginPageInfo
                {
                    Name = "collectionsbyfolder",
                    EmbeddedResourcePath = GetType().Namespace + ".Configuration.configPage.html"
                },
                new PluginPageInfo
                {
                    Name = "collectionsbyfolderjs",
                    EmbeddedResourcePath = GetType().Namespace + ".Configuration.config.js"
                }
            };
        }
    }
}
