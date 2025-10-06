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
           var ns = typeof(Plugin).Namespace!;
           return new[]
           {
               new PluginPageInfo
               {
                   // /web/configurationpage?name=collectionsbyfolder
                   Name = "collectionsbyfolder",
                   EmbeddedResourcePath = $"{ns}.Web.collectionsbyfolder.html"
               },
               new PluginPageInfo
               {
                   // /web/collectionsbyfolder.js   ← mit Punkt + .js (wie viele 10.10.x-Plugins)
                   Name = "collectionsbyfolder.js",
                   EmbeddedResourcePath = $"{ns}.Web.collectionsbyfolder.js"
               }
           };
       }

    }
}
