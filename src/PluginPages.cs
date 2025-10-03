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
                // WICHTIG: genau "config", damit der Einstellungen-Button funktioniert
                Name = "config",

                // entspricht deinem Embedded LogicalName in der .csproj
                EmbeddedResourcePath = "FolderCollections.Web.redirect.launch.html",

                // zusätzlich im linken Menü anzeigen
                EnableInMainMenu = true
            };
        }
    }
}
