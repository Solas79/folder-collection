using Microsoft.AspNetCore.Mvc;
using FolderCollections; // für Embedded

namespace FolderCollections.Web
{
    [ApiController]
    [Route("FolderCollections")]
    public class ConfigUiController : ControllerBase
    {
        // 1) Diagnose-Endpunkt: http(s)://<server>:8096/FolderCollections/ping  ->  200 "pong"
        [HttpGet("ping")]
        public IActionResult Ping() => Content("pong", "text/plain");

        // 2) Standalone HTML: http(s)://<server>:8096/FolderCollections/ui
        [HttpGet("ui")]
        public IActionResult Ui()
        {
            var html = Embedded.ReadAllText("FolderCollections.Web.ui.config.html");
            return Content(html, "text/html; charset=utf-8");
        }

        // 3) Standalone JS: http(s)://<server>:8096/FolderCollections/ui/config.js
        [HttpGet("ui/config.js")]
        public IActionResult UiJs()
        {
            var js = Embedded.ReadAllText("FolderCollections.Web.ui.fc-config.js");
            return Content(js, "application/javascript; charset=utf-8");
        }
    }
}
