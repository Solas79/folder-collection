using System;
using System.Collections.Generic;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.CollectionsByFolder
{
    public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
    {
        public Plugin(IApplicationPaths applicationPaths)
            : base(applicationPaths)
        {
        }

        public override string Name => "CollectionsByFolder";

        public override string Description =>
            "Erstellt/aktualisiert Collections anhand des letzten Ordnernamens.";

        // MUSS dieselbe GUID bleiben wie in manifest.json!
        public override Guid Id => new Guid("f58f3a40-6a8a-48e8-9b3a-9d7f0b6a3a41");

        //
        // Das ist der wichtige Teil:
        // Diese Pages werden Jellyfin direkt am Plugin gemeldet.
        // Die erste Page in der Liste ist die, die Jellyfin beim Klick auf das Plugin öffnet.
        //
        public IEnumerable<PluginPageInfo> GetPages()
        {
            yield return new PluginPageInfo
            {
                // der Name wird zur Route, z.B. CollectionsByFolder.html
                Name = "CollectionsByFolder",
                // das muss exakt zur eingebetteten Ressource passen:
                EmbeddedResourcePath = GetType().Namespace + ".Web.collectionsbyfolder.html"
                // also "Jellyfin.Plugin.CollectionsByFolder.Web.collectionsbyfolder.html"
            };
        }
    }
}
