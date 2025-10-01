// src/FolderCollections/Plugin.cs
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Web;

namespace FolderCollections;

public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
{
    public override string Name => "Folder Collections";
    public override Guid Id => new("YOUR-GUID-HERE-1234-5678-90AB-ABCDEF012345"); // fest lassen, nicht ändern

    public Plugin(IApplicationPaths appPaths, IXmlSerializer xml)
        : base(appPaths, xml) { }

    public IEnumerable<PluginPageInfo> GetPages() => new[]
    {
        new PluginPageInfo
        {
            Name = "folderCollectionsConfigPage",
            EmbeddedResourcePath = GetType().Namespace + ".Configuration.config.html",
            // Jellyfin erkennt daran, dass es eine volle Plugin-Konfig-Seite ist:
            PageType = PluginPageType.PluginConfiguration
        }
    };
}
