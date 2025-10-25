using System;
using System.Collections.Generic;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization; // <- wichtig für IXmlSerializer in Jellyfin 10.11

namespace Jellyfin.Plugin.CollectionsByFolder
{
    public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages, IHasPluginConfiguration
    {
        // Singleton-ähnlicher Zugriff für Controller usw.
        public static Plugin Instance { get; private set; }

        public override string Name => "CollectionsByFolder";

        public override string Description =>
            "Erstellt/aktualisiert Collections anhand des letzten Ordnernamens.";

        // MUSS identisch mit manifest.json GUID sein
        public override Guid Id => new Guid("f58f3a40-6a8a-48e8-9b3a-9d7f0b6a3a41");

        // Neuer Jellyfin 10.11 Stil: BasePlugin<TConfig>(IApplicationPaths, IXmlSerializer)
        public Plugin(IApplicationPaths appPaths, IXmlSerializer xmlSerializer)
            : base(appPaths, xmlSerializer)
        {
            Instance = this;
        }

        // Damit Jellyfin die Plugin-Einstellungen persistiert
        public PluginConfiguration GetConfiguration() => Configuration;

        // WICHTIG:
        // Diese Page melden wir direkt hier am Plugin.
        // Jellyfin wird diese HTML-Seite öffnen, wenn du im Admin-UI
        // auf dein Plugin klickst (Puzzle-Symbol).
        public IEnumerable<PluginPageInfo> GetPages()
        {
            yield return new PluginPageInfo
            {
                // interne Route / Seitenname
                Name = "CollectionsByFolder",

                // eingebettete Ressource: Namespace + ".Web." + Dateiname
                // -> Jellyfin.Plugin.CollectionsByFolder.Web.collectionsbyfolder.html
                EmbeddedResourcePath = GetType().Namespace + ".Web.collectionsbyfolder.html"
            };
        }
    }
}
