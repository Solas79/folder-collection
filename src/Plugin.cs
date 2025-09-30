using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using System;
using System.IO;

namespace Jellyfin.Plugin.FolderCollections.GUI;

public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
{
public override string Name => "Folder Collections";
public override Guid Id { get; } = Guid.Parse("4bb2a3d2-b8c6-4b3f-bf2c-d1a3e4e9b7a1");

public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
: base(applicationPaths, xmlSerializer)
{
}

public override string Description => "Group library items by parent folder into BoxSets (configurable).";

public IEnumerable<PluginPageInfo> GetPages()
{
return new[]
{
new PluginPageInfo
{
Name = "config",
EmbeddedResourcePath = GetType().Namespace + ".Web.Configuration.config.html"
},
new PluginPageInfo
{
Name = "config.js",
EmbeddedResourcePath = GetType().Namespace + ".Web.Configuration.config.js"
}
};
}
}
