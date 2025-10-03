using Microsoft.AspNetCore.Mvc;
using FolderCollections; // wenn du Embedded.ReadAllText nutzt – sonst nicht nötig

namespace FolderCollections.Web
{
    [ApiController]
    [Route("FolderCollections")]
    public class ConfigUiController : ControllerBase
    {
        [HttpGet("ping")]
        public IActionResult Ping() => Content("pong", "text/plain");

        [HttpGet("ui")]
        public IActionResult Ui()
        {
            // Wenn du die UI als eigene Datei hostest:
            var html = System.IO.File.ReadAllText("wwwroot/fc/config.html"); // <- falls lokal ausgeliefert
            // ODER (falls als Embedded hinterlegt):
            // var html = Embedded.ReadAllText("FolderCollections.Web.ui.config.html");
            return Content(html, "text/html; charset=utf-8");
        }
    }
}
