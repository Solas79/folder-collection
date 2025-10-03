using MediaBrowser.Model.Plugins;

namespace FolderCollections.Web
{
    public sealed class PluginPages : IPluginConfigurationPage
    {
        public IEnumerable<PluginPageInfo> GetPages()
        {
            // kleiner Launcher, der /FolderCollections/ui im Top-Level öffnet
            yield return new PluginPageInfo
            {
                Name = "config",
                EmbeddedResourcePath = "FolderCollections.Web.redirect.launch.html",
                EnableInMainMenu = true
            };
        }
    }
}
