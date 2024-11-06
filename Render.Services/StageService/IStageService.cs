using Render.Models.Sections;
using Render.Models.Workflow;
using Render.Models.Workflow.Stage;

namespace Render.Services.StageService;

public interface IStageService
{
    Dictionary<Guid, List<Guid>> StepsWithWork { get; }

    void BuildStepsWithWork(Guid userId);

    Task OrganizeSectionsIntoSteps(Guid userId);
    
    void HideDeactivatedStagesAndSteps();

    IEnumerable<(Guid StageId, Guid StepId)> FindWorkflowStepsAssignedForUser(Guid userId,
        RenderWorkflow workflow);
    
    List<Step> CheckIfNextStepNeedsInterpretation(
        RenderWorkflow workflow,
        List<Step> nextSteps,
        Guid currentStepId, Guid sectionId,
        bool needsInterpretation);

    Task<List<Step>> SkipBackTranslateAndTranscribeStepsIfNeededAsync(Guid sectionId, Stage stage,
        RenderWorkflow workflow);

    Task<Step> GetStepToWorkAsync(Guid sectionId, RenderStepTypes stepType, Guid currentProjectId);

    IEnumerable<Stage> GetAllStages(RenderWorkflow workflow);

    List<Guid> GetAllAssignedSectionAtStep(Guid stepId, RenderStepTypes renderStepType);

    List<Guid> StepsAssignedToUser();
    
    bool IsUserAssignedToNextStep(RenderWorkflow workflow, WorkflowStatus status, Guid userId);

    bool IsUserAssignedToStepInNextStage(RenderWorkflow workflow, WorkflowStatus status, Guid userId);

    List<Guid> SectionsAtStep(Guid stepId);
}