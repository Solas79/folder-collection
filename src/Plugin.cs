// src/PluginPages.cs
using System.Collections.Generic;
using MediaBrowser.Model.Plugins;

namespace FolderCollections.Web
{
    // Jellyfin 10.10.x → IHasWebPages (not IPluginConfigurationPage)
    public sealed class PluginPages : IHasWebPages
    {
        public IEnumerable<PluginPageInfo> GetPages()
        {
            yield return new PluginPageInfo
            {
                Name = "config",
                EmbeddedResourcePath = "FolderCollections.Web.redirect.launch.html",
                EnableInMainMenu = true
            };
        }
    }
}
