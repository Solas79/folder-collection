using System;
using System.Collections.Generic;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace FolderCollections
{
    public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
    {
        public static Plugin? Instance { get; private set; }

        public override string Name => "Folder Collections";
        public override Guid Id => new Guid("9f4f2c47-b3c5-4b13-9b1f-1c9a5c3b8d6a");

        public Plugin(IApplicationPaths appPaths, IXmlSerializer xml)
            : base(appPaths, xml)
        {
            Instance = this;
        }

        public IEnumerable<PluginPageInfo> GetPages() => new[]
        {
            new PluginPageInfo
            {
                Name = "config", // Pflichtname für das Zahnrad
                EmbeddedResourcePath = "FolderCollections.Web.Configuration.config.html"
            }
        };

    }
}
