using Render.Models.Sections;
using Render.Models.Snapshot;
using Render.Models.Workflow;
using Render.Models.Workflow.Stage;

namespace Render.Services.SnapshotService
{
    public interface ISnapshotService : IDisposable
    {
        Task ReturnBack(
            List<Stage> workflowStages,
            Section section,
            Stage currentStage,
            Stage previousStage,
            Step previousStep,
            Guid userId);

        Task CreateTemporarySnapshotAfterReRecording(Section section, Step step, Guid userId,
            Guid parentSnapshotId);

        Task CreateMissingSnapshotsForStagesWithPreviousRemovedStage(Guid userId);

        Task RemoveOldTemporarySnapshot(
            RenderWorkflow workflow,
            Section section,
            Guid stepId,
            List<Step> nextSteps,
            Guid userId,
            bool needToCreateSnapshotForCurrentStep = true,
            bool needToCreateSnapshotForNextStage = true);

        Task<bool> CheckForSnapshotConflicts(Guid sectionId);

        Task<Stage> GetConflictedStage(Guid sectionId);

        Task<List<Guid>> FilterOutConflicts(Step step);

        Task<List<ConflictedSnapshot>> GetLastConflictedSnapshots(Guid sectionId);
    }
}
