using Render.Models.Audio;
using Render.Models.LocalOnlyData;
using Render.Models.Sections;
using Render.Models.Workflow;

namespace Render.Services.SessionStateServices
{
    public interface ISessionStateService
    {
        List<Guid> AudioIds { get; }

        UserProjectSession ActiveSession { get; set; }

        Task LoadUserProjectSessionAsync(Guid userId, Guid projectId);

        void AddDraftAudio(Audio audio);

        void AddRequirementCompletion(Guid completedRequirementId);

        void StartPage(
            Guid sectionId,
            string pageName,
            PassageNumber passageNumber,
            Guid nonDraftTranslationId,
            Step step);

        void RemoveDraftAudio(Guid audioId);

        Task RemoveDraftAudios(IEnumerable<Guid> audioIds);

        bool RequirementMetInSession(Guid requirementId);

        void SetCurrentStep(Guid stepId, Guid sectionId);

        string GetSessionPage(Guid stepId, Guid sectionId, PassageNumber passageNumber);

        Task ResetSessionAsync();

        Task FinishSessionAsync();

        void SetCurrentDraftId(Guid id);

        Task ResetPassageNumberForSectionStep(Guid stepId, Guid sectionId);

        Task DeleteAllTemporaryAudioAsync(List<Guid> ids);

        void SetCurrentStepForDraftOrRevise(Guid stepId, Guid sectionId);

	}
}