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

        // GET /Plugins/CollectionsByFolder/Config
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

                cfg.Whitelist = SplitLines(whitelist);
                cfg.Blacklist = SplitLines(blacklist);
                cfg.Prefix    = prefix ?? string.Empty;
                cfg.Suffix    = suffix ?? string.Empty;
                cfg.MinFiles  = Math.Max(0, minfiles ?? 0);

                // optional/kompatibel:
                cfg.FolderPaths = new List<string>(cfg.Whitelist);

                plugin.UpdateConfiguration(cfg);

                // simple OK für fetch
                return Content("OK", "text/plain; charset=utf-8");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"CBF Save Fehler: {ex.GetType().Name}: {ex.Message}");
            }
        }

        // POST /Plugins/CollectionsByFolder/Scan
        [HttpPost("Scan")]
        [AllowAnonymous]
        [IgnoreAntiforgeryToken]
        public IActionResult Scan()
        {
            try
            {
                // TODO: Echte Scan-Logik hier anstoßen
                // z.B. Task.Run(() => new CollectionBuilder(...).RunOnce());

                return Content("OK", "text/plain; charset=utf-8");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"CBF Scan Fehler: {ex.GetType().Name}: {ex.Message}");
            }
        }
    }
}
