using Render.Models.Sections;
using Render.Models.Snapshot;

namespace Render.Repositories.SnapshotRepository
{
    public interface ISnapshotRepository : IDisposable
    {
        Task<List<Snapshot>> GetSnapshotsForSectionAsync(Guid sectionId);

        List<Snapshot> FilterSnapshotsMatchingPassageNumber(PassageNumber passageNumber, List<Snapshot> snapshots);

        Task<List<Snapshot>> GetPermanentSnapshotsForSectionAsync(Guid sectionId);

        Task SaveAsync(Snapshot snapshot);

        Task BatchDeleteAsync(List<Snapshot> snapshots, Section section);

        Task Purge(Guid id);

        List<Snapshot> FilterSnapshotByStageId(List<Snapshot> snapshots, Guid stageId);

        Task BatchSoftDeleteAsync(List<Snapshot> snapshots, Section section);
        
        Task<Snapshot> GetPassageDraftsForSnapshot(Snapshot snapshot, bool getRetellBackTranslations = false,
            bool getSegmentBackTranslations = false);
    }
}