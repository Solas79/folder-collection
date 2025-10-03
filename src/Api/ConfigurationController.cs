using MediaBrowser.Controller.Configuration;
using MediaBrowser.Model.Serialization;
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
        private readonly IServerConfigurationManager _config;
        private readonly IJsonSerializer _json;
        private readonly ILogger<ConfigurationController> _logger;

        public ConfigurationController(
            IServerConfigurationManager config,
            IJsonSerializer json,
            ILogger<ConfigurationController> logger)
        {
            _config = config;
            _json = json;
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

        // Optionaler Scan-Endpoint (Stub) für den Button
        [HttpPost("Scan")]
        public ActionResult ScanNow()
        {
            // Hier später deinen echten Scan aufrufen
            _logger.LogInformation("FolderCollections scan requested via UI.");
            return NoContent();
        }
    }
}
