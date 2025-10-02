using System;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Services;
using Microsoft.Extensions.Logging;
using MediaBrowser.Model.Tasks;

namespace FolderCollections
{
    // POST /Plugins/{PluginId}/Scan
    [Route("/Plugins/{PluginId}/Scan", "POST", Summary = "Startet den FolderCollections-Scan manuell")]
    public class ManualScanRequest : IReturnVoid
    {
        public string PluginId { get; set; } = "";
    }

    public class ScanController : IService
    {
        private readonly ILoggerFactory _loggerFactory;

        public ScanController(ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;
        }

        public async Task Post(ManualScanRequest request)
        {
            // Task direkt ausführen – ohne IScheduledTaskManager
            var logger = _loggerFactory.CreateLogger<FolderCollectionsTask>();
            var task = new FolderCollectionsTask(logger);

            await task.ExecuteAsync(progress: null, cancellationToken: CancellationToken.None);
        }
    }
}
