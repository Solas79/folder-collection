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

    public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
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
        // Hinweis: Ab 10.10.x erwartet Jellyfin TaskTriggerInfo.
        // Um 100% kompilierbar zu bleiben, liefern wir hier zunächst keine Default-Trigger zurück.
        // Den Zeitplan kannst du im Jellyfin-Dashboard setzen.
        //
        // Wenn du einen täglichen Default setzen willst, kannst du – je nach API –
        // einen TaskTriggerInfo mit Daily konfigurieren (Eigenschaftsnamen variieren zwischen Versionen).
        //
        // Beispiel (falls deine API diese Properties hat):
        // var cfg = Plugin.Instance?.Configuration ?? new PluginConfiguration();
        // var hour = Math.Clamp(cfg.ScanHour, 0, 23);
        // var minute = Math.Clamp(cfg.ScanMinute, 0, 59);
        // yield return new TaskTriggerInfo
        // {
        //     Type = TaskTriggerType.Daily,
        //     TimeOfDayTicks = new TimeSpan(hour, minute, 0).Ticks
        // };

        return Array.Empty<TaskTriggerInfo>();
    }
}
