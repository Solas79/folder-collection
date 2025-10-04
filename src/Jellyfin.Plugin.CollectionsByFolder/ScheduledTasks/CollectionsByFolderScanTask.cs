using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.CollectionsByFolder.Services;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;
using MediaBrowser.Model.Tasks;

namespace Jellyfin.Plugin.CollectionsByFolder.ScheduledTasks;

public class CollectionsByFolderScanTask : IScheduledTask
{
    private readonly CollectionBuilder _builder;
    private readonly ILogger<CollectionsByFolderScanTask> _logger;

    public string Name => "Collections by Folder – täglicher Scan";
    public string Description => "Erzeugt/aktualisiert Sammlungen anhand der letzten Ordnernamen";
    public string Category => "Library";

    public CollectionsByFolderScanTask(CollectionBuilder builder, ILogger<CollectionsByFolderScanTask> logger)
    {
        _builder = builder;
        _logger = logger;
    }

    public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        var cfg = Plugin.Instance.Configuration;
        progress.Report(0);
        var updated = await _builder.RunAsync(cfg, cancellationToken);
        progress.Report(100);
        _logger.LogInformation("CollectionsByFolder: geplanter Scan abgeschlossen – {Count} Collections aktualisiert", updated);
    }

    public TaskTriggers GetDefaultTriggers()
    {
        var cfg = Plugin.Instance.Configuration;
        if (!cfg.DailyScanEnabled)
        {
            return new TaskTriggers();
        }

        var time = ParseTime(cfg.DailyScanTime);
        return new TaskTriggers
        {
            Triggers = new ITaskTrigger[]
            {
                new DailyTrigger { TimeOfDayTicks = time.Ticks }
            }
        };
    }

    private static TimeSpan ParseTime(string hhmm)
    {
        if (TimeSpan.TryParseExact(hhmm, @"hh\:mm", CultureInfo.InvariantCulture, out var ts))
            return ts;
        return new TimeSpan(3,30,0);
    }
}
