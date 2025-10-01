using System;
using System.Collections.Generic;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace Jellyfin.Plugin.FolderCollections.GUI
{
    public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
    {
        public static Plugin? Instance { get; private set; }

        public override string Name => "Folder Collections";
        public override Guid Id { get; } = Guid.Parse("4bb2a3d2-b8c6-4b3f-bf2c-d1a3e4e9b7a1");
        public override string Description => "Group library items by parent folder into BoxSets (configurable).";

        public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
            : base(applicationPaths, xmlSerializer)
        {
            Instance = this;
        }

        public IEnumerable<PluginPageInfo> GetPages()
        {
            var ns = GetType().Namespace;
            return new[]
            {
                new PluginPageInfo
                {Name = "config.html", EmbeddedResourcePath = ns + ".Web.Configuration.config.html"},
                new PluginPageInfo
                {Name = "config.js", EmbeddedResourcePath = ns + ".Web.Configuration.config.js"}
            };
        }
    }
}
