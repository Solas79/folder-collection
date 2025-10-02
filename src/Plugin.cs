// src/FolderCollections/Plugin.cs
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace FolderCollections;

public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
{
    public override string Name => "Folder Collections";
    public override Guid Id => new("9f4f2c47-b3c5-4b13-9b1f-1c9a5c3b8d6a"); // fest lassen, nicht ändern

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
