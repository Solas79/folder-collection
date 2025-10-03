// src/PluginPages.cs
using System.Collections.Generic;
using MediaBrowser.Model.Plugins;

namespace FolderCollections.Web
{
    // Achtung: IHasWebPages (nicht IPluginConfigurationPage)
    public sealed class PluginPages : IHasWebPages
    {
        public IEnumerable<PluginPageInfo> GetPages()
        {
            // Launcher-Seite, die auf die Standalone-UI verweist
            yield return new PluginPageInfo
            {
                Name = "config",
                EmbeddedResourcePath = "FolderCollections.Web.redirect.launch.html",
                EnableInMainMenu = true
            };
        }
    }
}
