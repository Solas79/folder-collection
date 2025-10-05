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

        // ✅ Für Jellyfin 10.10 (net8): JSON
        public Plugin(IApplicationPaths paths, IJsonSerializer json)
            : base(paths, json)
        {
            Instance = this;
        }

        // ✅ Fallback für ältere (10.9): XML
        public Plugin(IApplicationPaths paths, IXmlSerializer xml)
            : base(paths, xml)
        {
            Instance = this;
        }

        // Robust: suche eingebettete Ressourcen automatisch
        private static string FindRes(string suffix)
        {
            var names = typeof(Plugin).Assembly.GetManifestResourceNames();
            var hit = names.FirstOrDefault(n => n.EndsWith(suffix, StringComparison.Ordinal));
            if (hit == null)
            {
                throw new InvalidOperationException(
                    $"CBF: EmbeddedResource '{suffix}' nicht gefunden. Vorhanden: {string.Join(" | ", names)}");
            }
            return hit;
        }

        public IEnumerable<PluginPageInfo> GetPages()
        {
            Console.WriteLine("[CBF] GetPages() aufgerufen");
            var names = typeof(Plugin).Assembly.GetManifestResourceNames();
            Console.WriteLine("[CBF] Res in DLL: " + string.Join(" | ", names));

            // robust: suche die beiden Dateien automatisch
            string find(string suffix)
            {
                var hit = names.FirstOrDefault(n => n.EndsWith(suffix, StringComparison.Ordinal));
                if (hit == null) throw new InvalidOperationException($"[CBF] '{suffix}' nicht gefunden.");
                Console.WriteLine("[CBF] Treffer " + suffix + ": " + hit);
                return hit;
            }

            return new[]
            {
                new PluginPageInfo { Name = "collectionsbyfolder",  EmbeddedResourcePath = find(".configPage.html") },
                new PluginPageInfo { Name = "cbf_js", EmbeddedResourcePath = find(".configPage.js") }
            };
        }

    }
}
