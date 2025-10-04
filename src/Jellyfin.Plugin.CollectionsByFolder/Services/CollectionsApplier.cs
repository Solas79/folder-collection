using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.Plugin.CollectionsByFolder.Services
{
    public sealed class CollectionsApplier
    {
        public sealed class ApplyResult
        {
            public int Created { get; set; }
            public int Updated { get; set; }
            public int Skipped { get; set; }
            public int TotalCandidates { get; set; }
        }

        public async Task<ApplyResult> ApplyAsync(IReadOnlyList<CollectionBuilder.FolderCandidate> candidates, CancellationToken ct = default)
        {
            // TODO: Hier kannst du die echte Jellyfin-Collection-Integration einfügen (ICollectionManager etc.)
            // Für jetzt: zähle alles als „Created“, damit UI-Feedback vorhanden ist.
            var result = new ApplyResult
            {
                TotalCandidates = candidates.Count,
                Created = candidates.Count,
                Updated = 0,
                Skipped = 0
            };

            await Task.CompletedTask;
            return result;
        }
    }
}
