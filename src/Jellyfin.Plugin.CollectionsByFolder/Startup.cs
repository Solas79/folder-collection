// Datei: Startup.cs
using System.Threading.Tasks;
using MediaBrowser.Controller.Plugins;   // IServerEntryPoint
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.CollectionsByFolder
{
    // Wird beim Jellyfin-Start automatisch ausgeführt
    public class Startup : IServerEntryPoint
    {
        private readonly ILogger<Startup> _logger;

        public Startup(ILogger<Startup> logger)
        {
            _logger = logger;
        }

        public Task RunAsync()
        {
            _logger.LogInformation("[CBF] Startup.RunAsync – Plugin aktiv, registriere Seiten.");
            return Task.CompletedTask;
        }

        public Task StopAsync()
        {
            _logger.LogInformation("[CBF] Startup.StopAsync – Plugin wird beendet.");
            return Task.CompletedTask;
        }
    }
}
