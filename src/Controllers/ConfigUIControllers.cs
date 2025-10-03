using Microsoft.AspNetCore.Mvc;
using FolderCollections;

namespace FolderCollections.Web;

[ApiController]
[Route("FolderCollections")]
public class ConfigUiController : ControllerBase
{
    [HttpGet("ui")]
    public IActionResult Ui()
    {
        var html = Embedded.ReadAllText("FolderCollections.Web.ui.config.html");
        return Content(html, "text/html; charset=utf-8");
    }

    [HttpGet("ui/config.js")]
    public IActionResult UiJs()
    {
        var js = Embedded.ReadAllText("FolderCollections.Web.ui.fc-config.js");
        return Content(js, "application/javascript; charset=utf-8");
    }
}
