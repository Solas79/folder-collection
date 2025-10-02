using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;

namespace FolderCollections;

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

    public async Task Execute(CancellationToken cancellationToken, IProgress<double> progress)
    {
        // Konfiguration holen (mit Fallback)
        var cfg = Plugin.Instance?.Configuration ?? new PluginConfiguration();

        _logger.LogInformation("FolderCollectionsTask gestartet. IncludeMovies={IncludeMovies}, IncludeSeries={IncludeSeries}, MinItems={MinItems}, Prefix='{Prefix}', Suffix='{Suffix}', Scan={Hour:D2}:{Minute:D2}",
            cfg.IncludeMovies, cfg.IncludeSeries, cfg.MinItems, cfg.Prefix, cfg.Suffix, cfg.ScanHour, cfg.ScanMinute);

        // TODO: Hier deine eigentliche Scan-/Erstell-Logik einfügen.
        // Für jetzt nur ein Dummy-Progress, damit der Task sauber läuft:
        progress?.Report(0);
        await Task.Delay(200, cancellationToken);
        progress?.Report(100);

        _logger.LogInformation("FolderCollectionsTask beendet.");
    }

    public IEnumerable<ITaskTrigger> GetDefaultTriggers()
    {
        var cfg = Plugin.Instance?.Configuration ?? new PluginConfiguration();
        var hour = Math.Clamp(cfg.ScanHour, 0, 23);
        var minute = Math.Clamp(cfg.ScanMinute, 0, 59);

        yield return new DailyTrigger { TimeOfDay = new TimeSpan(hour, minute, 0) };
    }
}
