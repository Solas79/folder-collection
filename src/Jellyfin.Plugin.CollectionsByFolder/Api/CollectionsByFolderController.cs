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
        [HttpPost("ScanNow")]
        public async Task<IActionResult> ScanNow(CancellationToken ct)
        {
            var builder = new CollectionBuilder();
            var applier = new CollectionsApplier();

            var candidates  = await builder.BuildCollectionsAsync(ct);
            var applyResult = await applier.ApplyAsync(candidates, ct);

            return Ok(new
            {
                started    = true,
                candidates = candidates.Count,
                created    = applyResult.Created,
                updated    = applyResult.Updated,
                skipped    = applyResult.Skipped
            });
        }
    }
}
