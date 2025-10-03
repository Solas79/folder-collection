using System;
using System.Collections.Generic;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace FolderCollections
{
    public sealed class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
    {
        public static readonly Guid PluginGuid = new("9f4f2c47-b3c5-4b13-9b1f-1c2d3e4f5a60");

        public Plugin(IApplicationPaths appPaths, IXmlSerializer xml)
            : base(appPaths, xml)
        {
        }

        public override string Name => "FolderCollections";
        public override string Description => "Erstellt/verwaltet Sammlungen basierend auf Ordnern.";

        public override Guid Id => PluginGuid;

        public IEnumerable<PluginPageInfo> GetPages()
        {
            // WICHTIG: LogicalName in csproj == EmbeddedResourcePath hier
            return new[]
            {
                new PluginPageInfo
                {
                    Name = "FolderCollectionsConfigPage",
                    EmbeddedResourcePath = "FolderCollections.Web.configPage.html",
                    EnableInMainMenu = false,
                    DisplayName = Name,
                    ConfigurationPageType = PluginPageType.PluginConfiguration
                },
                new PluginPageInfo
                {
                    Name = "FolderCollectionsConfigPageJS",
                    EmbeddedResourcePath = "FolderCollections.Web.configPage.js"
                }
            };
        }
    }
}
