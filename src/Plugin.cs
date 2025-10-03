using System;
using System.Collections.Generic;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace FolderCollections
{
    public sealed class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
    {
        public static readonly Guid PluginGuid = new("9f4f2c47-b3c5-4b13-9b1f-1c9a5c3b8d6a");

        public override string Name => "Folder Collections";
        public override string Description => "Erzeugt/aktualisiert Sammlungen anhand von Ordnerstrukturen.";
        public override Guid Id => PluginGuid;

        public Plugin(IApplicationPaths appPaths, IXmlSerializer xml)
            : base(appPaths, xml) { }

        // WICHTIG: Web-Seite am Plugin melden
        public IEnumerable<PluginPageInfo> GetPages()
        {
            yield return new PluginPageInfo
            {
                // Dieser Name wird von der Jellyfin-UI beim Einstellungs-Button erwartet
                Name = "config",

                // Muss exakt dem LogicalName in der .csproj entsprechen (siehe Schritt 2)
                EmbeddedResourcePath = "FolderCollections.Web.redirect.launch.html",

                // Damit zusätzlich ein Eintrag links im Menü erscheint (den du „am Rand“ magst)
                EnableInMainMenu = true,

                // Optional (wenn vorhanden in deiner SDK-Version):
                // DisplayName = "Folder Collections",
                // MenuSection = "plugins",
                // MenuIcon = "folder"
            };
        }
    }
}
