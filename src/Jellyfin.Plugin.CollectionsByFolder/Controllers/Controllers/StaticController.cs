using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.CollectionsByFolder.Controllers
{
    [ApiController]
    [Route("Plugins/CollectionsByFolder")]
    public sealed class StaticController : ControllerBase {
          [HttpGet("js")]
          [AllowAnonymous]
          public IActionResult GetJs() {
            var res = $"{typeof(Plugin).Namespace}.Web.collectionsbyfolder.js";
            var s = typeof(Plugin).Assembly.GetManifestResourceStream(res);
            if (s == null) return NotFound(res);
            return File(s, "application/javascript; charset=utf-8");
          }
        }

    }
}
