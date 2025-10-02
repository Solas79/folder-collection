using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Services;
using MediaBrowser.Model.Tasks;

namespace FolderCollections
{
    [Route("/Plugins/{PluginId}/Scan", "POST", Summary = "Startet den FolderCollections-Scan manuell")]
    public class ManualScanRequest : IReturnVoid
    {
        public string PluginId { get; set; } = "";
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
            var taskInfo = _taskManager.ScheduledTasks
                .FirstOrDefault(t => t.ScheduledTask.GetType() == typeof(FolderCollectionsTask));

            if (taskInfo == null)
                throw new InvalidOperationException("FolderCollectionsTask wurde nicht gefunden.");

            await _taskManager.Execute(taskInfo.ScheduledTask, CancellationToken.None);
        }
    }
}
