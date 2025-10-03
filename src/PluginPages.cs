using System.Collections.Generic;
using MediaBrowser.Model.Plugins;

namespace FolderCollections.Web
{
    public sealed class PluginPages : IHasWebPages
    {
        public IEnumerable<PluginPageInfo> GetPages()
        {
            yield return new PluginPageInfo
            {
                // WICHTIG: Name beliebig, aber stabil – wird in der URL verwendet (?name=foldercollections)
                Name = "foldercollections",
                // Diese eingebettete Datei haben wir angelegt:
                EmbeddedResourcePath = "FolderCollections.Web.redirect.launch.html",

                // Button „Einstellungen“ in Plugins-Liste anzeigen:
                IsMainConfigPage = true,

                // Zusätzlich im linken Admin-Menü anzeigen:
                EnableInMainMenu = true
            };
        }
    }
}
