using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.CollectionsByFolder.Services;
using MediaBrowser.Model.Tasks;

namespace Jellyfin.Plugin.CollectionsByFolder.ScheduledTasks
{
    public class CollectionsByFolderScanTask : IScheduledTask
    {
        public string Name => "Collections by Folder: Scan";
        public string Description => "Scannt die konfigurierten Ordner und erstellt/aktualisiert Collections.";
        public string Category => "Library";
        public string Key => "CollectionsByFolder.Scan";

        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers() => Array.Empty<TaskTriggerInfo>();

        public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
        {
            progress?.Report(0);

            var builder = new CollectionBuilder();
            var applier = new CollectionsApplier();

            var candidates = await builder.BuildCollectionsAsync(cancellationToken);
            _ = await applier.ApplyAsync(candidates, cancellationToken);

            progress?.Report(100);
        }
    }
}
