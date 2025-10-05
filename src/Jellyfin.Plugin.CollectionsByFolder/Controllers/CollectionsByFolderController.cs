using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.CollectionsByFolder.Controllers
{
    // Feste Basisroute: /Plugins/CollectionsByFolder/...
    [Route("Plugins/CollectionsByFolder")]
    [AllowAnonymous] // erlaubt Aufrufe ohne Token (wichtig bei reinem HTML-Form)
    public class CollectionsByFolderController : Controller
    {
        private static List<string> SplitLines(string? text) =>
            string.IsNullOrWhiteSpace(text)
                ? new List<string>()
                : text.Replace("\r\n", "\n").Replace("\r", "\n")
                      .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                      .Select(s => s.Trim())
                      .Where(s => s.Length > 0)
                      .Distinct(StringComparer.OrdinalIgnoreCase)
                      .ToList();

        // GET /Plugins/CollectionsByFolder/Ping
        [HttpGet("Ping")]
        public IActionResult Ping()
        {
            // bewusst ohne jegliche Abhängigkeiten
            return Content("ok", "text/plain");
        }

        // POST /Plugins/CollectionsByFolder/Save
        [HttpPost("Save")]
        [Consumes("application/x-www-form-urlencoded")]
        [IgnoreAntiforgeryToken] // falls globales CSRF aktiv
        public IActionResult Save(
            [FromForm] string? whitelist,
            [FromForm] string? blacklist,
            [FromForm] string? prefix,
            [FromForm] string? suffix,
            [FromForm] int?    minfiles,
            [FromForm] string? folderpaths
        )
        {
            try
            {
                var plugin = Plugin.Instance;
                if (plugin is null)
                    return StatusCode(500, "CBF Save: Plugin.Instance == null");

                var cfg = plugin.Configuration ?? new PluginConfiguration();

                cfg.Whitelist   = SplitLines(whitelist);
                cfg.Blacklist   = SplitLines(blacklist);
                cfg.Prefix      = prefix ?? string.Empty;
                cfg.Suffix      = suffix ?? string.Empty;
                cfg.MinFiles    = Math.Max(0, minfiles ?? 0);

                var fp = SplitLines(folderpaths);
                cfg.FolderPaths = fp.Count > 0 ? fp : new List<string>(cfg.Whitelist);

                plugin.UpdateConfiguration(cfg);

                var baseUrl = (Request?.PathBase.HasValue == true) ? Request.PathBase.Value : string.Empty;
                var backUrl = $"{baseUrl}/web/configurationpage?name=collectionsbyfolder";

                var okHtml = $@"<!doctype html><meta charset=""utf-8"">
<title>CollectionsByFolder – gespeichert</title>
<style>body{{font-family:system-ui,Segoe UI,Roboto,Arial,sans-serif;padding:24px}}</style>
<h1>Gespeichert ✔</h1>
<p>Die Einstellungen wurden übernommen.</p>
<p><a href=""{backUrl}"">Zurück zur Konfigurationsseite</a></p>";
                return Content(okHtml, "text/html; charset=utf-8");
            }
            catch (Exception ex)
            {
                return StatusCode(500, "CBF Save Fehler: " + ex.GetType().Name + ": " + ex.Message);
            }
        }
    }
}
