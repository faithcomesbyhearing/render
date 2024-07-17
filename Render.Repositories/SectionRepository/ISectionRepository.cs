using Render.Models.Sections;
using Render.Models.Snapshot;

namespace Render.Repositories.SectionRepository
{
    public interface ISectionRepository: IDisposable
    {
        Task<Section> GetSectionAsync(Guid id, bool withReferences = false);

        Task<Snapshot> GetPassageDraftsAsync(
            Snapshot snapshot, bool 
            getRetellBackTranslations = false,
            bool getSegmentBackTranslations = false);

        Task<Section> GetSectionWithDraftsAsync(
            Guid id, 
            bool getRetellBackTranslations = false, 
            bool getSegmentBackTranslations = false, 
            bool getCommunityTest = false, 
            bool withReferences = false);

        Task<List<Section>> GetSectionsForProjectAsync(Guid projectId);

        Task SaveSectionAsync(Section section);

        Task<Section> GetReferencesForSectionAsync(Section section);

        Task<Section> GetPassageSupplementaryMaterials(Section section, Passage passage);

        Task<Section> RevertSectionToSnapshotAsync(Snapshot snapshot, List<Snapshot> snapshots, Guid loggedInUserId, Guid projectId);

        Task<Section> RevertSectionToDefaultAsync(Section section, Guid loggedInUserId, Guid projectId);

        Task SaveSectionReferencesAsync(IEnumerable<SectionReferenceAudio> sectionReferences);
        
        Task<Section> GetSectionWithReferencesAsync(Guid id);

        Task SaveSectionAndDraftAsync(Section section, Draft newDraft);

        Task SaveSectionWithNewDivisionsAsync(Section section);

        Task Purge(Guid id);

        Task DeleteDraftAsync(Guid draftId);
    }
}