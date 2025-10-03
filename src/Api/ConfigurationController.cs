using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace FolderCollections.Api
{
    [ApiController]
    [Authorize(Policy = "RequiresElevation")]
    [Route("FolderCollections/Configuration")]
    public class ConfigurationController : ControllerBase
    {
        private readonly ILogger<ConfigurationController> _logger;

        public ConfigurationController(ILogger<ConfigurationController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public ActionResult Get()
        {
            var cfg = Plugin.Instance.Configuration;
            return Ok(cfg);
        }

        [HttpPost]
        public ActionResult Update([FromBody] PluginConfiguration cfg)
        {
            Plugin.Instance.UpdateConfiguration(cfg);
            _logger.LogInformation("FolderCollections configuration saved: {@cfg}", cfg);
            return NoContent();
        }

        [HttpPost("Scan")]
        public ActionResult ScanNow()
        {
            _logger.LogInformation("FolderCollections scan requested via UI.");
            return NoContent();
        }
    }
}
