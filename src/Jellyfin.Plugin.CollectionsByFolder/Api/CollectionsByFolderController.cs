using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.CollectionsByFolder.Services;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.CollectionsByFolder.Api
{
    [ApiController]
    [Route("CollectionsByFolder")]
    public class CollectionsByFolderController : ControllerBase
    {
        /// <summary>
        /// Startet sofort einen Scan. Gibt aktuell nur eine kurze Bestätigung + Kandidatenanzahl zurück.
        /// </summary>
        [HttpPost("ScanNow")]
        public async Task<IActionResult> ScanNow(CancellationToken ct)
        {
            var builder = new CollectionBuilder();

            // Hier werden die Kandidaten (Ordner -> CollectionName) ermittelt.
            var candidates = await builder.BuildCollectionsAsync(ct);

            // TODO: Später hier mit Jellyfin-APIs (ICollectionManager etc.) die Collections wirklich erstellen/aktualisieren.
            return Ok(new
            {
                started = true,
                candidateCount = candidates.Count
            });
        }
    }
}
