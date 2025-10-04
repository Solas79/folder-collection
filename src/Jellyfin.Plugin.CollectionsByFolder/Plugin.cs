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

            // Debug: eingebettete Ressourcen im Log sehen (einmalig ok)
            try
            {
                foreach (var n in GetType().Assembly.GetManifestResourceNames())
                    Console.WriteLine($"[CBF] EmbeddedResource: {n}");
            }
            catch { /* no-op */ }
        }

        public IEnumerable<PluginPageInfo> GetPages()
        {
            // harte Ressourcennamen – exakt so liegen sie in der DLL
            return new[]
            {
                new PluginPageInfo
                {
                    Name = "collectionsbyfolder",
                    EmbeddedResourcePath = "Jellyfin.Plugin.CollectionsByFolder.Configuration.index.html",
                    EnableInMainMenu = true
                },
                new PluginPageInfo
                {
                    Name = "collectionsbyfolderjs",
                    EmbeddedResourcePath = "Jellyfin.Plugin.CollectionsByFolder.Configuration.index.js"
                }
            };
        }
    }
}
