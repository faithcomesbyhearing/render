using Render.Models.Sections;
using Render.Models.Snapshot;
using Render.Repositories.Audio;
using Render.Repositories.Kernel;
using Render.Repositories.SectionRepository;

namespace Render.Repositories.SnapshotRepository
{
    public class SnapshotRepository : ISnapshotRepository
    {
        private readonly IDataPersistence<Snapshot> _snapshot;
        private readonly ISectionRepository _sectionRepository;
        private readonly IAudioRepository<Draft> _draftRepository;
        
        public SnapshotRepository(
            IDataPersistence<Snapshot> snapshot,
            ISectionRepository sectionRepository,
            IAudioRepository<Draft> draftRepository)
        {
            _snapshot = snapshot;
            _sectionRepository = sectionRepository;
            _draftRepository = draftRepository;
        }

        public async Task<List<Snapshot>> GetSnapshotsForSectionAsync(Guid sectionId) =>
            await GetSnapshots(x => !x.Deleted, sectionId);

        public async Task<List<Snapshot>> GetPermanentSnapshotsForSectionAsync(Guid sectionId) =>
            await GetSnapshots(x => !x.Deleted && !x.Temporary, sectionId);
        
        public List<Snapshot> FilterSnapshotsMatchingPassageNumber(
            PassageNumber passageNumber, List<Snapshot> snapshots)
        {
            var matchingSnapshots = new List<Snapshot>();
            foreach (var snapshot in snapshots)
            {
                if (snapshot.HasPassage(passageNumber))
                {
                    matchingSnapshots.Add(snapshot);
                }
            }

            return matchingSnapshots;
        }

        public List<Snapshot> FilterSnapshotByStageId(List<Snapshot> snapshots, Guid stageId)
        {
            //Sort order by date oldest to latest
            return snapshots.Where(x => x.StageId == stageId).OrderBy(x => x.DateUpdated).ToList();
        }

        public async Task SaveAsync(Snapshot snapshot)
        {
            await _snapshot.UpsertAsync(snapshot.Id, snapshot);
        }

        public async Task BatchDeleteAsync(List<Snapshot> snapshots, Section section)
        {
            await DeleteDraftFromSnapshots(snapshots, section);
            await _snapshot.BatchDeleteAsync(snapshots.Select(x => x.Id).ToList());
        }
        
        public async Task BatchSoftDeleteAsync(List<Snapshot> snapshots, Section section)
        {
            await DeleteDraftFromSnapshots(snapshots, section);
            foreach (var snapshot in snapshots)
            {
                snapshot.SetDelete(true);
                await _snapshot.UpsertAsync(snapshot.Id, snapshot);
            }
        }

        private async Task DeleteDraftFromSnapshots(List<Snapshot> snapshots, Section section)
        {
            if (snapshots.Count == 0) return;
            // return a list of snapshots for a given section which are not deleted
            var snapshotsInSection = await GetSnapshotsForSectionAsync(section.Id);
            if (snapshots.Any(snapshot => snapshot.Passages.Any(x => x.CurrentDraftAudioId != default)))
            {
                await DeleteOldDraftedAudio(snapshots, snapshotsInSection, section);
            }
            else
            {
                await DeleteNewDraftedAudio(snapshots, snapshotsInSection, section);
            }
            
        }

        private async Task DeleteOldDraftedAudio(List<Snapshot> snapshots, List<Snapshot> snapshotsInSection, Section section)
        {
            var draftIdsInSection = section.Passages.Select(x => x.CurrentDraftAudioId).ToList();
            var snapshotToDeleteIds = snapshots.Select(x => x.Id);
            var snapshotToKeep = snapshotsInSection.Where(x => !snapshotToDeleteIds.Contains(x.Id));
            var draftIdsToKeep = snapshotToKeep.SelectMany(x => x.Passages).Select(y => y.CurrentDraftAudioId).ToList();
            foreach (var snapshot in snapshots)
            {
                foreach (var passage in snapshot.Passages)
                {
                    if (!draftIdsToKeep.Contains(passage.CurrentDraftAudioId) && !draftIdsInSection.Contains(passage.CurrentDraftAudioId))
                    {
                        // delete unreferenced drafts and associated notes
                        await _sectionRepository.DeleteDraftAsync(passage.CurrentDraftAudioId);
                    }
                }
            }
        }
        
        private async Task DeleteNewDraftedAudio(List<Snapshot> snapshots, List<Snapshot> snapshotsInSection, Section section)
        {
            var draftIdsInSection = section.Passages.Select(x => x.CurrentDraftAudio?.Id ?? Guid.Empty).ToList();
            var snapshotToDeleteIds = snapshots.Select(x => x.Id);
            var snapshotToKeep = snapshotsInSection.Where(x => !snapshotToDeleteIds.Contains(x.Id)).ToList();
            var draftIdsToKeep = snapshotToKeep.SelectMany(x => x.PassageDrafts).Select(y => y.DraftId).ToList();
            foreach (var snapshot in snapshots)
            {
                foreach (var passage in snapshot.PassageDrafts.Where(passage => !draftIdsToKeep.Contains(passage.DraftId) && !draftIdsInSection.Contains(passage.DraftId)))
                {
                    // delete unreferenced drafts and associated notes
                    await _sectionRepository.DeleteDraftAsync(passage.DraftId);
                }
            }
        }
        
        private async Task LoadDrafts(Snapshot snapshot)
        {
            if (snapshot == null) return;

            foreach (var passage in snapshot.Passages)
            {
                Draft draft;
                //for backward compatibility support
                if (passage.CurrentDraftAudioId != Guid.Empty)
                {
                    draft = await _draftRepository.GetByIdAsync(passage.CurrentDraftAudioId);
                }
                else
                {
                    //Get a draft for a snapshot using the unique DraftId key
                    var draftId = snapshot.PassageDrafts.Single(passageDraft => passageDraft.PassageId == passage.Id).DraftId;
                    draft = await _draftRepository.GetByIdAsync(draftId);
                }
                if (draft is null) continue;
                passage.ChangeCurrentDraftAudio(draft);   
            }
        }

        public async Task<Snapshot> GetPassageDraftsForSnapshot(Snapshot snapshot, bool getRetellBackTranslations = false,
            bool getSegmentBackTranslations = false)
        {
            if (snapshot.Passages.Any(x => !x.HasAudio))
            {
                await LoadDrafts(snapshot);
            }
            
            return await _sectionRepository.GetPassageDraftsAsync(snapshot, getRetellBackTranslations, getSegmentBackTranslations);
        }

        public async Task Purge(Guid id)
        {
            await _snapshot.PurgeAllOfTypeForProjectId(id);
        }

        private async Task<List<Snapshot>> GetSnapshots(Func<Snapshot, bool> predicate, Guid sectionId)
        {
            var matchingSnapshots = new List<Snapshot>();
            var snapshots = await _snapshot.QueryOnFieldAsync("SectionId", sectionId.ToString(), 0);
            if (snapshots != null && snapshots.Any())
            {
                matchingSnapshots = snapshots.Where(predicate).OrderBy(x => x.DateUpdated).ToList();
            }
            
            return matchingSnapshots;
        }

        public void Dispose()
        {
            _snapshot?.Dispose();
            _draftRepository.Dispose();
            _sectionRepository.Dispose();
        }
    }
}