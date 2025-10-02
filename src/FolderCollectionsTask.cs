using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;

namespace FolderCollections
{
    public class FolderCollectionsTask : IScheduledTask
    {
        private readonly ILogger<FolderCollectionsTask> _logger;

        public FolderCollectionsTask(ILogger<FolderCollectionsTask> logger)
        {
            _logger = logger;
        }

        public string Name => "Folder Collections: täglicher Scan";
        public string Key => "FolderCollections.DailyScan";
        public string Description => "Erstellt/aktualisiert Sammlungen basierend auf der Ordnerstruktur.";
        public string Category => "Library";

        public async Task ExecuteAsync(IProgress<double>? progress, CancellationToken cancellationToken)
        {
            var cfg = Plugin.Instance?.Configuration ?? new PluginConfiguration();

            _logger.LogInformation(
                "FolderCollectionsTask gestartet. IncludeMovies={IncludeMovies}, IncludeSeries={IncludeSeries}, MinItems={MinItems}, Prefix='{Prefix}', Suffix='{Suffix}', Scan={Hour:D2}:{Minute:D2}",
                cfg.IncludeMovies, cfg.IncludeSeries, cfg.MinItems, cfg.Prefix, cfg.Suffix, cfg.ScanHour, cfg.ScanMinute);

            progress?.Report(0);

            // TODO: hier deine eigentliche Scan-/Erstell-Logik
            await Task.Delay(200, cancellationToken);

            progress?.Report(100);
            _logger.LogInformation("FolderCollectionsTask beendet.");
        }

        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
        {
            // Standardmäßig keine Auto-Trigger setzen; Zeitplan im Dashboard konfigurieren
            return Array.Empty<TaskTriggerInfo>();
        }
    }
}
