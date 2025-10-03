using System;
using System.Collections.Generic;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
// Hinweis: In 10.10 ist der Serializer-Parameter i.d.R. noch gültig;
// falls es dazu später eine Fehlermeldung gibt, sag Bescheid, dann passe ich den Konstruktor an.

namespace FolderCollections
{
    public sealed class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
    {
        public static readonly Guid PluginGuid = new("9f4f2c47-b3c5-4b13-9b1f-1c2d3e4f5a60");
        public static Plugin Instance { get; private set; } = null!;

        public Plugin(IApplicationPaths appPaths, MediaBrowser.Model.Serialization.IXmlSerializer xml)
            : base(appPaths, xml)
        {
            Instance = this;
        }

        public override string Name => "FolderCollections";
        public override string Description => "Erstellt/verwaltet Sammlungen basierend auf Ordnern.";
        public override Guid Id => PluginGuid;

        public IEnumerable<PluginPageInfo> GetPages()
        {
            return new[]
            {
                new PluginPageInfo
                {
                    Name = "FolderCollectionsConfigPage",
                    EmbeddedResourcePath = "FolderCollections.Web.configPage.html"
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
