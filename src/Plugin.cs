using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Controller.Plugins;

namespace FolderCollections;

public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
{
    public static Plugin? Instance { get; private set; }

    public override string Name => "Folder Collections";
    public override Guid Id => new("9f4f2c47-b3c5-4b13-9b1f-1c9a5c3b8d6a");

    public Plugin(IApplicationPaths appPaths, IXmlSerializer xml) : base(appPaths, xml)
    {
        Instance = this;
    }

    public IEnumerable<PluginPageInfo> GetPages() => new[]
{
    new PluginPageInfo
    {
        Name = "config",
        EmbeddedResourcePath = "FolderCollections.Web.Configuration.config.html"
    },
    new PluginPageInfo
    {
        Name = "config.js",
        EmbeddedResourcePath = "FolderCollections.Web.Configuration.config.js"
    }
};

}
