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
            var pages = new[]
            {
                new PluginPageInfo {
                    Name = "collectionsbyfolder", // HTML
                    EmbeddedResourcePath = $"{ns}.Web.collectionsbyfolder.html"
                },
                new PluginPageInfo {
                    Name = "collectionsbyfolderjs", // JS  -> /web/collectionsbyfolderjs
                    EmbeddedResourcePath = $"{ns}.Web.collectionsbyfolder.js"
                }
            };

            // << DEBUG: zur Laufzeit sehen, was wirklich registriert wird
            try
            {
                Console.WriteLine("[CBF] GetPages() called");
                foreach (var p in pages)
                    Console.WriteLine($"[CBF] Page Name='{p.Name}'  Res='{p.EmbeddedResourcePath}'");
            }
            catch { /* ignore */ }

            return pages;
        }

    }
}
