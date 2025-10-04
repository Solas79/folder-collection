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
        private readonly ILogger<CollectionsByFolderScanTask> _logger;

        public CollectionsByFolderScanTask(ILogger<CollectionsByFolderScanTask> logger)
        {
            _logger = logger;
        }

        public string Key => "CollectionsByFolderScan";
        public string Name => "CollectionsByFolder – Scan";
        public string Description => "Durchsucht alle konfigurierten Verzeichnisse und erstellt Sammlungen nach Ordnernamen.";
        public string Category => "Library";

        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
        {
            return new[]
            {
                new TaskTriggerInfo
                {
                    Type = TaskTriggerInfo.TriggerDaily,
                    TimeOfDayTicks = new TimeSpan(1, 0, 0).Ticks
                }
            };
        }

        public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("[CollectionsByFolder] Starte Scan...");

                var config = Plugin.Instance.Configuration;
                if (config == null || config.FolderPaths.Length == 0)
                {
                    _logger.LogWarning("[CollectionsByFolder] Keine Pfade konfiguriert – Scan abgebrochen.");
                    return;
                }

                _logger.LogInformation($"[CollectionsByFolder] Überprüfe {config.FolderPaths.Length} Verzeichnisse...");

                // Hier folgt später dein Logik-Aufruf, z.B.:
                // await new CollectionBuilder(_logger).BuildCollectionsAsync(config, cancellationToken);

                await Task.Delay(1000, cancellationToken); // Platzhalter
                _logger.LogInformation("[CollectionsByFolder] Scan abgeschlossen.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[CollectionsByFolder] Fehler beim Scanvorgang");
            }
        }
    }
}
