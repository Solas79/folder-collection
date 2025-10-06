using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.CollectionsByFolder.Controllers
{
    [ApiController]
    [Route("Plugins/CollectionsByFolder")]
    public sealed class CollectionsByFolderController : ControllerBase
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

        // GET /Plugins/CollectionsByFolder/Config  -> aktuelle Werte als JSON
        [HttpGet("Config")]
        [AllowAnonymous]
        public IActionResult GetConfig()
        {
            var cfg = Plugin.Instance?.Configuration ?? new PluginConfiguration();

            return Ok(new
            {
                whitelist = cfg.Whitelist ?? new List<string>(),
                blacklist = cfg.Blacklist ?? new List<string>(),
                prefix    = cfg.Prefix   ?? string.Empty,
                suffix    = cfg.Suffix   ?? string.Empty,
                minfiles  = cfg.MinFiles
            });
        }




        // POST /Plugins/CollectionsByFolder/Save
        [HttpPost("Save")]
        [AllowAnonymous]
        [IgnoreAntiforgeryToken]
        public IActionResult Save(
            [FromForm] string? whitelist,
            [FromForm] string? blacklist,
            [FromForm] string? prefix,
            [FromForm] string? suffix,
            [FromForm] int?    minfiles)
        {
            try
            {
                var plugin = Plugin.Instance ?? throw new InvalidOperationException("Plugin.Instance == null");
                var cfg = plugin.Configuration ?? new PluginConfiguration();

                // Diese Properties müssen in deiner PluginConfiguration existieren:
                cfg.Whitelist = SplitLines(whitelist);
                cfg.Blacklist = SplitLines(blacklist);
                cfg.Prefix    = prefix ?? string.Empty;
                cfg.Suffix    = suffix ?? string.Empty;
                cfg.MinFiles  = Math.Max(0, minfiles ?? 0);

                // optional/kompatibel:
                cfg.FolderPaths = new List<string>(cfg.Whitelist);

                plugin.UpdateConfiguration(cfg);

                // Zurücklink zur Konfigseite (funktioniert auch mit /jellyfin BasePath)
                var baseUrl = (Request?.PathBase.HasValue == true) ? Request.PathBase.Value : string.Empty;
                var backUrl = $"{baseUrl}/web/configurationpage?name=collectionsbyfolder";

                var html = $@"<!doctype html><meta charset=""utf-8"">
<title>CollectionsByFolder – gespeichert</title>
<style>body{{font-family:system-ui,Segoe UI,Roboto,Arial,sans-serif;padding:24px;line-height:1.4}} .ok{{color:#0a7a0a}}</style>
<h1 class=""ok"">Gespeichert ✔</h1>
<p>Die Einstellungen wurden übernommen.</p>
<p><a href=""{backUrl}"">Zurück zur Konfigurationsseite</a></p>";
                return Content(html, "text/html; charset=utf-8");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"CBF Save Fehler: {ex.GetType().Name}: {ex.Message}");
            }
        }
    }
}
