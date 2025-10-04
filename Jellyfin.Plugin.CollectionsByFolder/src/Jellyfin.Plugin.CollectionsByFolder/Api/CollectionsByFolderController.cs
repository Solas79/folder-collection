using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.CollectionsByFolder.Services;
using MediaBrowser.Controller.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.CollectionsByFolder.Api;

[ApiController]
[Route("Plugins/CollectionsByFolder")]
public class CollectionsByFolderController : ControllerBase
{
    private readonly CollectionBuilder _builder;
    private readonly ILogger<CollectionsByFolderController> _logger;

    public CollectionsByFolderController(CollectionBuilder builder, ILogger<CollectionsByFolderController> logger)
    {
        _builder = builder;
        _logger = logger;
    }

    [HttpPost("ScanNow")]
    public async Task<ActionResult> ScanNow(CancellationToken ct)
    {
        var cfg = Plugin.Instance.Configuration;
        var n = await _builder.RunAsync(cfg, ct);
        _logger.LogInformation("CollectionsByFolder: manueller Scan, {Count} Collections aktualisiert", n);
        return Ok(new { updated = n });
    }
}
