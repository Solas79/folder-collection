using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MediaBrowser.Controller.Configuration;

namespace FolderCollections.Api
{
    [ApiController]
    [Authorize(Policy = "RequiresElevation")]
    [Route("FolderCollections/Configuration")]
    public class ConfigurationController : ControllerBase
    {
        private readonly IServerConfigurationManager _config;
        private readonly ILogger<ConfigurationController> _logger;

        public ConfigurationController(
            IServerConfigurationManager config,
            ILogger<ConfigurationController> logger)
        {
            _config = config;
            _logger = logger;
        }

        [HttpGet]
        public ActionResult Get()
        {
            var cfg = _config.GetConfiguration<PluginConfiguration>("FolderCollections");
            return Ok(cfg);
        }

        [HttpPost]
        public ActionResult Update([FromBody] PluginConfiguration cfg)
        {
            _config.SaveConfiguration("FolderCollections", cfg);
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
