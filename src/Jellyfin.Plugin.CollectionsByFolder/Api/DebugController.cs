using Microsoft.AspNetCore.Mvc;
using MediaBrowser.Controller.Library;

namespace Jellyfin.Plugin.CollectionsByFolder.Api
{
    [ApiController]
    [Route("CollectionsByFolder/Debug")]
    public class DebugController : ControllerBase
    {
        [HttpGet("Resources")]
        public ActionResult GetResources()
        {
            var names = typeof(Plugin).Assembly.GetManifestResourceNames();
            return Ok(names); // gibt ein Array mit allen eingebetteten Ressourcennamen zurück
        }
    }
}
