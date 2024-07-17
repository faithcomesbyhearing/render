using Render.Models.Sections;
using Render.Models.Workflow;
using Render.Models.Workflow.Stage;
using Render.Services.SessionStateServices;

namespace Render.Services
{
    public interface IGrandCentralStation : IDisposable
    {
        Guid CurrentProjectId { get; }

        List<WorkflowStatus> FullWorkflowStatusList { get; }

        RenderWorkflow ProjectWorkflow { get; }

        Task FindWorkForUser(Guid projectId, Guid userId);

        void UpdateWorkflow(RenderWorkflow workflow);

        List<Guid> StepsAssignedToUser();

        List<Guid> SectionsAtStep(Guid stepId);

        List<Guid> GetAllAssignedSectionAtStep(Guid stepId, RenderStepTypes renderStepType);

        Task AdvanceSectionAsync(Section section, Step step);

        Task AdvanceSectionAfterRecoveryAsync(Section section, Step step);

        Task<bool> CheckForSnapshotConflicts(Guid sectionId);

        Task<List<Guid>> FilterOutConflicts(Step step);

        Task AdvanceSectionAfterReviseAsync(Section section, Step step, ISessionStateService sessionStateService);

        Task AdvanceSectionAfterReviewAsync(Section section, Step step);

        Task<Step> GetStepToWorkAsync(Guid sectionId, RenderStepTypes stepType);

        Stage GetStageFromWorkflow(RenderWorkflow workflow, Guid stepId);

        Task ReplaceWorkflowStatus(Section section, Step newStep, RenderWorkflow workflow);

        IEnumerable<Stage> GetAllStages(RenderWorkflow workflow);
        
        Task<Dictionary<Draft, List<Message>>> FindMessagesNeedingInterpretation(Section section, Guid stageId, Guid stepId, StageTypes stageType);

        Dictionary<RenderWorkflow, Dictionary<Stage, Dictionary<Step, List<Guid>>>> GetProcessesData();

        void ResetWorkForUser();

        Task SetHasNewMessageForWorkflowStep(Section section, Step step, bool value);

        bool NeedToShowDraftNotesOnPeerCheck(Guid stageId, Guid stepId);

        bool NeedToShowDraftNotesOnConsultantCheck(Guid stageId, Guid stepId);

        Task CreateTemporarySnapshotAfterReRecording(Section section, Step step);

        Task<Stage> GetConflictedStage(Guid sectionId);

        Task RemoveHoldingTankSectionStatusesAfterApproval(Guid sectionId);
    }
}