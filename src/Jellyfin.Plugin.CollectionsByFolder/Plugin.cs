using System;
using System.Collections.Generic;
using Jellyfin.Data.Serialization; // in manchen Builds heißt das anders; siehe Hinweis unten
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.CollectionsByFolder
{
    public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages, IHasPluginConfiguration
    {
        // static Instance für Controller etc.
        public static Plugin Instance { get; private set; }

        public override string Name => "CollectionsByFolder";

        public override string Description =>
            "Erstellt/aktualisiert Collections anhand des letzten Ordnernamens.";

        // GUID muss dieselbe bleiben wie im manifest.json!
        public override Guid Id => new Guid("f58f3a40-6a8a-48e8-9b3a-9d7f0b6a3a41");

        // Neuer Konstruktorstil ab Jellyfin 10.11:
        // BasePlugin<TConfig> erwartet jetzt auch einen XmlSerializer.
        // Der konkrete Typ heißt typischerweise IXmlSerializer
        // und lebt (Stand 10.11) im Namespace Jellyfin.Data.Serialization.
        public Plugin(IApplicationPaths appPaths, IXmlSerializer xmlSerializer)
            : base(appPaths, xmlSerializer)
        {
            Instance = this;
        }

        // Plugin-Konfigurationsobjekt zurückgeben
        public PluginConfiguration GetConfiguration() => Configuration;

        //
        // Entscheidend für dein UI:
        // IHasWebPages hier direkt in der Plugin-Klasse.
        // Die erste (und einzige) Page, die wir zurückgeben,
        // ist deine HTML-Seite. Jellyfin öffnet diese Seite,
        // wenn du im Admin-UI auf das Puzzle-Symbol deines Plugins klickst.
        //
        public IEnumerable<PluginPageInfo> GetPages()
        {
            yield return new PluginPageInfo
            {
                // Das "Name" wird zur Route, z.B. .../CollectionsByFolder.html
                // Wichtig: stabil lassen, keine Leerzeichen.
                Name = "CollectionsByFolder",

                // Genau dieser Pfad muss zu deiner EmbeddedResource passen.
                // Bei dir: Web/collectionsbyfolder.html ist eingebettet als
                // Jellyfin.Plugin.CollectionsByFolder.Web.collectionsbyfolder.html
                EmbeddedResourcePath = GetType().Namespace + ".Web.collectionsbyfolder.html"
            };
        }
    }
}
