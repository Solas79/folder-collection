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

        public Plugin(IApplicationPaths appPaths, IXmlSerializer xmlSerializer)
            : base(appPaths, xmlSerializer)
        {
            Instance = this;

            // Logge ALLE eingebetteten Ressourcen, damit wir sofort sehen, wie sie wirklich heißen
            try
            {
                foreach (var n in GetType().Assembly.GetManifestResourceNames())
                    Console.WriteLine($"[CBF] EmbeddedResource: {n}");
            }
            catch { }
        }

        public IEnumerable<PluginPageInfo> GetPages()
        {
            // Finde die echten Namen dynamisch, egal wie der RootNamespace/Ordner heißt
            var names = GetType().Assembly.GetManifestResourceNames();

            string? html = names.FirstOrDefault(n =>
                n.EndsWith(".Configuration.index.html", StringComparison.OrdinalIgnoreCase));
            string? js = names.FirstOrDefault(n =>
                n.EndsWith(".Configuration.index.js", StringComparison.OrdinalIgnoreCase));

            if (html is null || js is null)
            {
                // Harte Fallbacks (falls oben nichts gefunden wurde)
                html ??= "Jellyfin.Plugin.CollectionsByFolder.Configuration.index.html";
                js   ??= "Jellyfin.Plugin.CollectionsByFolder.Configuration.index.js";
                Console.WriteLine("[CBF] WARN: Using hardcoded EmbeddedResourcePath fallback.");
            }
            else
            {
                Console.WriteLine($"[CBF] Using resources: html={html}, js={js}");
            }

            return new[]
            {
                // /web/collectionsbyfolder  -> index.html
                new PluginPageInfo
                {
                    Name = "collectionsbyfolder",
                    EmbeddedResourcePath = html,
                    EnableInMainMenu = true
                },
                // /web/collectionsbyfolderjs -> index.js
                new PluginPageInfo
                {
                    Name = "collectionsbyfolderjs",
                    EmbeddedResourcePath = js
                }
            };
        }
    }
}
