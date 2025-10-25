using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.CollectionsByFolder.Services;
using MediaBrowser.Controller.Collections;
using MediaBrowser.Controller.Library;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.CollectionsByFolder.Controllers
{
    [ApiController]
    [Route("Plugins/CollectionsByFolder")]
    public sealed class CollectionsByFolderController : ControllerBase
    {
        private readonly ILibraryManager _library;
        private readonly ICollectionManager _collections;
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger<CollectionsByFolderController> _log;

        public CollectionsByFolderController(
            ILibraryManager library,
            ICollectionManager collections,
            ILoggerFactory loggerFactory,
            ILogger<CollectionsByFolderController> log)
        {
            _library = library;
            _collections = collections;
            _loggerFactory = loggerFactory;
            _log = log;
        }

        private static List<string> SplitLines(string? text) =>
            string.IsNullOrWhiteSpace(text)
                ? new List<string>()
                : text.Replace("\r\n", "\n").Replace("\r", "\n")
                      .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                      .Select(s => s.Trim())
                      .Where(s => s.Length > 0)
                      .Distinct(StringComparer.OrdinalIgnoreCase)
                      .ToList();

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
                cfg.FolderPaths = new List<string>(cfg.Whitelist);

                plugin.UpdateConfiguration(cfg);

                _log.LogInformation("[CBF] Save: WL={W} BL={B} Min={Min} Prefix='{P}' Suffix='{S}'",
                    cfg.Whitelist.Count, cfg.Blacklist.Count, cfg.MinFiles, cfg.Prefix, cfg.Suffix);

                return Content("OK", "text/plain; charset=utf-8");
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "[CBF] Save Fehler");
                return StatusCode(500, $"CBF Save Fehler: {ex.GetType().Name}: {ex.Message}");
            }
        }

        [HttpPost("Scan")]
        [AllowAnonymous]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Scan(CancellationToken ct)
        {
            try
            {
                var cfg = Plugin.Instance?.Configuration ?? new PluginConfiguration();

                _log.LogInformation("[CBF] Scan-Request empfangen.");
                var builderLogger = _loggerFactory.CreateLogger<CollectionBuilder>();

                // Neue CollectionBuilder-Signatur ohne BaseItemRepository
                var builder = new CollectionBuilder(_library, _collections, builderLogger);

                var (created, updated) = await builder.RunOnceAsync(cfg, ct).ConfigureAwait(false);

                return Content($"OK created={created} updated={updated}", "text/plain; charset=utf-8");
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "[CBF] Scan Fehler");
                return StatusCode(500, $"CBF Scan Fehler: {ex.GetType().Name}: {ex.Message}");
            }
        }
    }
}
