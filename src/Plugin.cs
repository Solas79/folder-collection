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
            : base(appPaths, xml)
        {
        }

        // <<<<<< WICHTIG: Web-Seiten hier am Plugin-Typ melden
        public IEnumerable<PluginPageInfo> GetPages()
        {
            yield return new PluginPageInfo
            {
                // exakt "config", damit der Einstellungen-Button funktioniert
                Name = "config",

                // muss zu deinem Embedded LogicalName in der .csproj passen
                EmbeddedResourcePath = "FolderCollections.Web.redirect.launch.html",

                // optional: zusätzlich im linken Menü anzeigen
                EnableInMainMenu = true
            };
        }
    }
}
