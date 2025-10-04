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
        public string Description => "Scannt die konfigurierten Ordner und plant Collections.";
        public string Category => "Library";

        // Ein stabiler Schlüsselname (wird u.a. im UI verwendet)
        public string Key => "CollectionsByFolder.Scan";

        // Minimal: keine Default-Trigger setzen → planbar im UI.
        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers() => Array.Empty<TaskTriggerInfo>();

        public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
        {
            progress?.Report(0);

            var builder = new CollectionBuilder();
            var _ = await builder.BuildCollectionsAsync(cancellationToken);

            // TODO: Hier später die echten Collection-Operationen (Erstellen/Aktualisieren) ausführen.

            progress?.Report(100);
        }
    }
}
