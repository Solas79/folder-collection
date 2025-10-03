using System;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Serialization;

namespace FolderCollections
{
    public sealed class Plugin : BasePlugin<PluginConfiguration>
    {
        // Deine GUID
        public static readonly Guid PluginGuid = new("9f4f2c47-b3c5-4b13-9b1f-1c9a5c3b8d6a");

        public override string Name => "Folder Collections";
        public override string Description => "Erzeugt/aktualisiert Sammlungen anhand von Ordnerstrukturen.";
        public override Guid Id => PluginGuid;

        public Plugin(IApplicationPaths appPaths, IXmlSerializer xml)
            : base(appPaths, xml)
        {
        }
    }
}
