// src/PluginPages.cs
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
                // frei wählbar, stabil halten (landet in der URL: ?name=foldercollections)
                Name = "foldercollections",

                // eingebettete Datei (siehe csproj LogicalName)
                EmbeddedResourcePath = "FolderCollections.Web.redirect.launch.html",

                // Link im linken Admin-Menü anzeigen
                EnableInMainMenu = true

                // In 10.10.x KEIN IsMainConfigPage mehr
                // Optional vorhanden (je nach Build): MenuSection, DisplayName, MenuIcon
            };
        }
    }
}
