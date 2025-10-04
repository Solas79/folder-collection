using Jellyfin.Plugin.CollectionsByFolder.Services;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.Plugin.CollectionsByFolder.ScheduledTasks
{
    public class CollectionsByFolderScanTask : IScheduledTask
    {
        private readonly CollectionBuilder _builder;
        private readonly ILogger<CollectionsByFolderScanTask> _logger;

        public CollectionsByFolderScanTask(CollectionBuilder builder, ILogger<CollectionsByFolderScanTask> logger)
        {
            _builder = builder;
            _logger = logger;
        }

        public string Key => "CollectionsByFolderScan";
        public string Name => "CollectionsByFolder – Scan";
        public string Description => "Erstellt/aktualisiert Sammlungen nach Ordnernamen.";
        public string Category => "Library";

        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
        {
            // Standard: täglich um 01:00, Benutzerzeit wird in der Config gesetzt (siehe UI)
            return new[]
            {
                new TaskTriggerInfo
                {
                    Type = TaskTriggerInfo.TriggerDaily,
                    TimeOfDayTicks = new TimeSpan(1,0,0).Ticks
                }
            };
        }

        public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
        {
            progress.Report(0);
            var cfg = Plugin.Instance.Configuration;
            var count = await _builder.RunAsync(cfg, cancellationToken);
            _logger.LogInformation("[CollectionsByFolder] Geplanter Scan abgeschlossen: {Count} Collections aktualisiert", count);
            progress.Report(100);
        }
    }
}
