using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace FolderCollections
{
    /// <summary>
    /// Minimaler Controller: Startet den Scan direkt,
    /// indem er die ScheduledTask-Logik aufruft.
    /// Kein ServiceStack, nur ASP.NET Core.
    /// </summary>
    [ApiController]
    [Route("Plugins/FolderCollections")]
    public class ScanController : ControllerBase
    {
        private readonly FolderCollectionsTask _task;
        private readonly ILogger<ScanController> _logger;

        public ScanController(FolderCollectionsTask task, ILogger<ScanController> logger)
        {
            _task = task;
            _logger = logger;
        }

        /// <summary>
        /// POST /Plugins/FolderCollections/Scan
        /// Führt die gleiche Logik aus wie der geplante Task (synchron im Request).
        /// </summary>
        [HttpPost("Scan")]
        public async Task<IActionResult> Scan(CancellationToken ct)
        {
            _logger.LogInformation("Manual scan via ScanController requested.");
            await _task.ExecuteAsync(progress: null, cancellationToken: ct);
            return Ok(new { ok = true, message = "FolderCollections scan finished" });
        }
    }
}
