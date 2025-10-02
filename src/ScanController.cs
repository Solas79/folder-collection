using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Services;
using MediaBrowser.Model.Tasks;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FolderCollections
{
    [Route("/Plugins/{PluginId}/Scan", "POST", Summary = "Startet den FolderCollections-Scan manuell")]
    public class ManualScanRequest : IReturnVoid
    {
        public string PluginId { get; set; }
    }

    public class ScanController : IService
    {
        private readonly IScheduledTaskManager _taskManager;

        public ScanController(IScheduledTaskManager taskManager)
        {
            _taskManager = taskManager;
        }

        public async Task Post(ManualScanRequest request)
        {
            // Task nach Typ finden
            var task = _taskManager.ScheduledTasks
                .FirstOrDefault(t => t.ScheduledTask.GetType() == typeof(FolderCollectionsTask));

            if (task == null)
            {
                throw new InvalidOperationException("FolderCollectionsTask wurde nicht gefunden.");
            }

            // Task manuell ausführen
            await _taskManager.Execute(task.ScheduledTask, CancellationToken.None);
        }
    }
}
