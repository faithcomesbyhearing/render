using Render.Models.Sections;
using Render.Models.Workflow;
using Render.Services.SessionStateServices;

namespace Render.Services.SectionMovementService;

public interface ISectionMovementService : IDisposable
{
    Task AdvanceSectionAsync(
        Section section,
        Step step,
        Guid currentProjectId,
        Guid userId);

    Task AdvanceSectionAfterRecoveryAsync(
        Section section,
        Step step,
        Guid currentProjectId,
        Guid userId);

    Task AdvanceSectionAfterReviewAsync(
        Section section,
        Step step,
        Guid currentProjectId,
        Guid userId);

    Task AdvanceSectionAfterReviseAsync(
        Section section,
        Step step,
        ISessionStateService sessionStateService,
        Guid currentProjectId,
        Guid userId);
    
    Task AdvanceSectionsToNextActiveStepsAsync(
        RenderWorkflow workflow,
        Func<Guid, List<Step>> getNextSteps,
        List<WorkflowStatus> allWorkflowStatuses,
        List<WorkflowStatus> statusListOfSectionsToAdvance,
        Guid currentProjectId);
}