using System.Collections.Specialized;
using Render.Models.Sections;
using Render.Models.Workflow;
using Render.Models.Workflow.Stage;

namespace Render.Services.WorkflowService;

public interface IWorkflowService
{
    OrderedDictionary WorkflowStatusListByWorkflow { get; }
    
    List<WorkflowStatus> FullWorkflowStatusList { get; }
    
    RenderWorkflow ProjectWorkflow { get; }
    
    Task PopulateWorkflowLists(Guid userId, Guid currentProjectId);
    
    Task<WorkflowStatus> CreateWorkflowStatusAsync(
        Guid workflowId,
        Guid sectionId,
        Guid projectId,
        Guid currentStepId,
        Guid scopeId,
        Guid currentStageId,
        RenderStepTypes currentStepType,
        bool hasNotesNeedingInterpretation);

    bool AreAllWorkflowStatusObjectsForSectionAtHoldingTank(
        Step step,
        Guid sectionId,
        List<WorkflowStatus> statusList);

    void UpdateWorkflow(RenderWorkflow workflow);
    
    Task ReplaceWorkflowStatus(
        Section section,
        Step newStep,
        RenderWorkflow workflow,
        Dictionary<Guid, List<Guid>> stepsWithWork);

    Task RemoveHoldingTankSectionStatusesAfterApproval(Guid sectionId);
    
    WorkflowStatus GetWorkflowStatus(RenderWorkflow workflow, Section section, Step step);

    RenderWorkflow FindWorkflow(Guid stepId);

    Task SetRemoveWorkState(Stage stage, RenderWorkflow workflow);

    Task SetHasNewDraftsForWorkflowStep(Section section, Step step, bool value);

    Task SetHasNewMessageForWorkflowStep(Section section, Step step, bool value);

    Task MoveForwardSectionAndGetStepAfterHoldingTankAsync(
        List<WorkflowStatus> statusList,
        RenderWorkflow workflow,
        Step nextStep,
        Guid sectionId,
        Guid currentProjectId);

    Task MarkAsCompletedAndUpdateRepository(List<WorkflowStatus> workflowStatus);

    Task RemoveStatus(WorkflowStatus workflowStatus);

    public bool NeedToShowDraftNotesOnConsultantCheck(RenderWorkflow workflow, Guid stageId, Section section);
}