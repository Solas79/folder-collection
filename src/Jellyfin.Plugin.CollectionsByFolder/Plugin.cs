using System;
using System.Collections.Generic;
using System.Linq;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace Jellyfin.Plugin.CollectionsByFolder
{
    public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
    {
        public static Plugin Instance { get; private set; } = null!;
        public override string Name => "CollectionsByFolder";
        public override string Description => "Erstellt automatisch Sammlungen nach Ordnernamen.";
        public override Guid Id => Guid.Parse("f58f3a40-6a8a-48e8-9b3a-9d7f0b6a3a41");

        public Plugin(IApplicationPaths paths, IXmlSerializer xml) : base(paths, xml)
        {
            Instance = this;
        }

        // 🔎 wirf NICHT mehr – liefere null, wenn nicht gefunden
        private static string? TryFindRes(string suffix)
        {
            var names = typeof(Plugin).Assembly.GetManifestResourceNames();
            return names.FirstOrDefault(n => n.EndsWith(suffix, StringComparison.Ordinal));
        }

        public IEnumerable<PluginPageInfo> GetPages()
        {
            // HTML **immer** registrieren (mit robustem Fallback auf den erwarteten LogicalName)
            var htmlRes = TryFindRes(".configPage.html")
                          ?? "Jellyfin.Plugin.CollectionsByFolder.configPage.html";

            var pages = new List<PluginPageInfo>
            {
                new PluginPageInfo
                {
                    Name = "collectionsbyfolder",              // → /web/collectionsbyfolder
                    EmbeddedResourcePath = htmlRes
                }
            };

            // JS **nur** registrieren, wenn vorhanden – sonst macht später die HTML den Inline-Fallback
            var jsRes = TryFindRes(".configPage.js");
            if (jsRes != null)
            {
                pages.Add(new PluginPageInfo
                {
                    Name = "cbf_js",                           // → /web/cbf_js
                    EmbeddedResourcePath = jsRes
                });
            }
            else
            {
                Console.WriteLine("[CBF] WARN: JS-Ressource nicht gefunden – Inline-Fallback nutzen.");
            }

            return pages;
        }
    }
}
