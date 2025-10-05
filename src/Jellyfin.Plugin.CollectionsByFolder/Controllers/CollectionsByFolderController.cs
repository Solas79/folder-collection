using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.CollectionsByFolder.Controllers
{
    // Endpunkte liegen unter  /Plugins/CollectionsByFolder/<Action>
    [ApiController]
    [Route("Plugins/[controller]/[action]")]
    public class CollectionsByFolderController : ControllerBase
    {
        private readonly ILogger<CollectionsByFolderController> _logger;

        public CollectionsByFolderController(ILogger<CollectionsByFolderController> logger)
        {
            _logger = logger;
        }

        private static List<string> SplitLines(string? text) =>
            string.IsNullOrWhiteSpace(text)
                ? new List<string>()
                : text
                    .Replace("\r\n", "\n")
                    .Replace("\r", "\n")
                    .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim())
                    .Where(s => !string.IsNullOrEmpty(s))
                    .Distinct()
                    .ToList();

        /// <summary>
        /// Nimmt das HTML-Form (application/x-www-form-urlencoded) entgegen und speichert die Plugin-Konfiguration.
        /// </summary>
        [HttpPost]
        [Consumes("application/x-www-form-urlencoded")]
        public IActionResult Save([FromForm] string? whitelist,
                                  [FromForm] string? blacklist,
                                  [FromForm] string? prefix,
                                  [FromForm] string? suffix,
                                  [FromForm] int?    minfiles)
        {
            var plugin = Plugin.Instance;
            if (plugin is null)
            {
                _logger.LogError("Plugin.Instance ist null");
                return StatusCode(500, "Plugin nicht initialisiert");
            }

            var cfg = plugin.Configuration;

            cfg.Whitelist = SplitLines(whitelist);
            cfg.Blacklist = SplitLines(blacklist);
            cfg.Prefix    = prefix ?? string.Empty;
            cfg.Suffix    = suffix ?? string.Empty;
            cfg.MinFiles  = Math.Max(0, minfiles ?? 0);

            plugin.UpdateConfiguration(cfg);

            _logger.LogInformation("[CBF] Konfiguration gespeichert: {W}W/{B}B Prefix='{P}' Suffix='{S}' Min={M}",
                cfg.Whitelist.Count, cfg.Blacklist.Count, cfg.Prefix, cfg.Suffix, cfg.MinFiles);

            // Einfache Rückmeldung als HTML – mit Link zurück zur Config-Seite.
            var html = @"<!doctype html><meta charset=""utf-8"">
                <title>CollectionsByFolder – gespeichert</title>
                <style>body{font-family:system-ui,Segoe UI,Roboto,Arial,sans-serif;padding:24px}</style>
                <h1>Gespeichert ✔</h1>
                <p>Die Einstellungen wurden übernommen.</p>
                <p><a href=""../web/configurationpage?name=collectionsbyfolder"">Zurück zur Konfigurationsseite</a></p>";
            return Content(html, "text/html; charset=utf-8");
        }
    }
}
