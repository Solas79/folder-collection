using System.Linq;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.CollectionsByFolder.Api
{
    [ApiController]
    [Route("CollectionsByFolder/Debug")]
    public class DebugController : ControllerBase
    {
        [HttpGet("Resources")]
        public IActionResult Resources()
        {
            var names = typeof(Plugin).Assembly.GetManifestResourceNames().OrderBy(n => n);
            return Ok(names);
        }
    }
}
