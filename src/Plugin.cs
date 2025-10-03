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

        public Plugin(IApplicationPaths appPaths, IXmlSerializer xml) : base(appPaths, xml) {}

        public IEnumerable<PluginPageInfo> GetPages()
        {
            yield return new PluginPageInfo
            {
                // WICHTIG: genau "config", damit der Einstellungen-Button in 10.10.7 greift
                Name = "config",

                // Launcher-HTML (s.u.) – leitet nach /FolderCollections/ui um
                EmbeddedResourcePath = "FolderCollections.Web.redirect.launch.html",

                // zusätzlich Eintrag im linken Menü
                EnableInMainMenu = true
            };
        }
    }
}
