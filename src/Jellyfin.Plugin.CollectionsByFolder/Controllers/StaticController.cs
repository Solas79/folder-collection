using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.CollectionsByFolder.Controllers
{
    [ApiController]
    [Route("Plugins/CollectionsByFolder")]
    public sealed class StaticController : ControllerBase
    {
        private static readonly Assembly Asm = typeof(Plugin).Assembly;
        private static readonly string Ns = typeof(Plugin).Namespace!;

        // GET /Plugins/CollectionsByFolder/js
        [HttpGet("js")]
        [AllowAnonymous]
        public IActionResult GetJs()
        {
            var resName = $"{Ns}.Web.collectionsbyfolder.js";
            var stream = Asm.GetManifestResourceStream(resName);
            if (stream == null)
            {
                return NotFound(resName);
            }

            return File(stream, "application/javascript; charset=utf-8");
        }
    }
}
