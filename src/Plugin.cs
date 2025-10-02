using System;
using System.Collections.Generic;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace FolderCollections
{
    public class PluginConfiguration : BasePluginConfiguration
    {
        public bool IncludeMovies { get; set; } = true;
        public bool IncludeSeries { get; set; } = true;
        public int MinItems { get; set; } = 2;
        public string Prefix { get; set; } = "";
        public string Suffix { get; set; } = "";
        public int ScanHour { get; set; } = 4;
        public int ScanMinute { get; set; } = 0;
        public string[] PathPrefixes { get; set; } = Array.Empty<string>();
        public string[] IgnorePatterns { get; set; } = Array.Empty<string>();
    }

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
                // Name MUSS "config" heißen, sonst kein Zahnrad
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
}
