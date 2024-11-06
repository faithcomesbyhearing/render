using System.Collections.Specialized;
using Render.Interfaces;
using Render.Models.Sections;
using Render.Models.Workflow;
using Render.Models.Workflow.Stage;
using Render.Repositories.Kernel;
using Render.Repositories.WorkflowRepositories;

namespace Render.Services.WorkflowService;

public class WorkflowService : IWorkflowService
{
    public OrderedDictionary WorkflowStatusListByWorkflow { get; } = new();
    public List<WorkflowStatus> FullWorkflowStatusList { get; } = [];

    public RenderWorkflow ProjectWorkflow { get; private set; }
    
    private readonly IDataPersistence<WorkflowStatus> _workflowStatusRepository;
    private readonly IWorkflowRepository _workflowRepository;
    private readonly IRenderLogger _renderLogger;
    
    private Guid CurrentProjectId { get; set; }
    
    public WorkflowService(
        IDataPersistence<WorkflowStatus> workflowStatusRepository,
        IWorkflowRepository workflowRepository, IRenderLogger renderLogger)
    {
        _workflowStatusRepository = workflowStatusRepository;
        _renderLogger = renderLogger;
        _workflowRepository = workflowRepository;
    }

    public async Task PopulateWorkflowLists(Guid userId, Guid currentProjectId)
    {
        CurrentProjectId = currentProjectId;
        
        WorkflowStatusListByWorkflow.Clear();
            
        var projectWorkflowStatusObjects = (await _workflowStatusRepository
            .QueryOnFieldAsync("ProjectId", CurrentProjectId.ToString(), 0)).Where(x => !x.IsCompleted);

        var projectWorkflows = await _workflowRepository
            .GetAllWorkflowsForProjectIdAsync(CurrentProjectId);

        ProjectWorkflow = projectWorkflows.SingleOrDefault(x => x.ParentWorkflowId == Guid.Empty);
        
        if (ProjectWorkflow == null)
        {
            _renderLogger.LogInfo("Project workflow is null", new Dictionary<string, string>
            {
                { "LoggedInUserId", userId.ToString() },
                { "ProjectId", currentProjectId.ToString() }
            });
        }
        else
        {
            //Order the workflows by parent hierarchy
            var orderedWorkflowList = new List<RenderWorkflow>();
            var secondaryWorkflows = projectWorkflows.Where(x => x.Id != ProjectWorkflow.Id).ToList();
            orderedWorkflowList.Add(ProjectWorkflow);
            
            AddChildWorkflows(ProjectWorkflow.Id, secondaryWorkflows, ref orderedWorkflowList);

            foreach (var workflow in orderedWorkflowList)
            {
                WorkflowStatusListByWorkflow.Add(workflow, new List<WorkflowStatus>());
            }
        }

        FullWorkflowStatusList.Clear();

        foreach (var workflowStatus in projectWorkflowStatusObjects)
        {
            var workflow = projectWorkflows.SingleOrDefault(x => x.Id == workflowStatus.WorkflowId);
            if (workflow != null)
            {
                ((List<WorkflowStatus>)WorkflowStatusListByWorkflow[workflow])?.Add(workflowStatus);
            }

            FullWorkflowStatusList.Add(workflowStatus);
        }
    }
    
    public async Task MoveForwardSectionAndGetStepAfterHoldingTankAsync(
        List<WorkflowStatus> statusList,
        RenderWorkflow workflow,
        Step nextStep,
        Guid sectionId,
        Guid currentProjectId)
    {
        var hasNotesNeedingInterpret = statusList.First().SectionHasNotesToInterpret;
        var statusToRemoveList = statusList.Where(x => x.ParentSectionId == sectionId).ToList();
        var scopeId = statusToRemoveList.First().ScopeId;

        foreach (var workflowStatus in statusToRemoveList)
        {
            workflowStatus.MarkAsCompleted();
            statusList.Remove(workflowStatus);
            await _workflowStatusRepository.UpsertAsync(workflowStatus.Id, workflowStatus);
        }

        var newWorkflowStatus = await CreateWorkflowStatusAsync(workflow.Id, sectionId, currentProjectId,
            nextStep.Id, scopeId, workflow.GetStage(nextStep.Id).Id,
            nextStep.RenderStepType, hasNotesNeedingInterpret);
        
        statusList.Add(newWorkflowStatus);
    }

    public async Task MarkAsCompletedAndUpdateRepository(List<WorkflowStatus> workflowStatus)
    {
        // if there is a conflict mid stage we may have duplicate workflow statuses
        foreach (var status in workflowStatus.ToList())
        {
            status.MarkAsCompleted();
            workflowStatus.Remove(status);
            await _workflowStatusRepository.UpsertAsync(status.Id, status);
        }
    }

    public async Task RemoveStatus(WorkflowStatus readyStatus)
    {
        await _workflowStatusRepository.DeleteAsync(readyStatus.Id);
    }

    public async Task<WorkflowStatus> CreateWorkflowStatusAsync(
        Guid workflowId,
        Guid sectionId,
        Guid projectId,
        Guid currentStepId,
        Guid scopeId,
        Guid currentStageId,
        RenderStepTypes currentStepType,
        bool hasNotesNeedingInterpretation)
    {
        var workflowStatus = new WorkflowStatus(sectionId, workflowId, projectId, currentStepId, scopeId,
            currentStageId, currentStepType);
        workflowStatus.SetHasNotesToInterpret(hasNotesNeedingInterpretation);
        await _workflowStatusRepository.UpsertAsync(workflowStatus.Id, workflowStatus);
        
        FullWorkflowStatusList.Add(workflowStatus);
        
        return workflowStatus;
    }
    
    public async Task RemoveHoldingTankSectionStatusesAfterApproval(Guid sectionId)
    {
        //Find all Holding Tank workflow statuses leftover for the section
        var status = FullWorkflowStatusList
            .Where(x => x.ParentSectionId == sectionId && !x.IsCompleted &&
                        x.CurrentStepType == RenderStepTypes.HoldingTank).ToList();

        foreach (var workflowStatus in status)
        {
            workflowStatus.MarkAsCompleted();
            await _workflowStatusRepository.UpsertAsync(workflowStatus.Id, workflowStatus);
        }
    }
    
    public bool AreAllWorkflowStatusObjectsForSectionAtHoldingTank(Step step, Guid sectionId,
        List<WorkflowStatus> statusList)
    {
        return step != null && step.RenderStepType == RenderStepTypes.HoldingTank
                            && statusList.Where(x => x.ParentSectionId == sectionId)
                                .All(x => x.CurrentStepId == step.Id);
    }
    
    public WorkflowStatus GetWorkflowStatus(RenderWorkflow workflow, Section section, Step step)
    {
        var statusList = (List<WorkflowStatus>)WorkflowStatusListByWorkflow[workflow];
        var workflowStatus = statusList.FirstOrDefault(x => x.CurrentStepId == step.Id
                                                            && x.ParentSectionId == section.Id);

        return workflowStatus;
    }
    
    public RenderWorkflow FindWorkflow(Guid stepId)
    {
        var enumerator = WorkflowStatusListByWorkflow.GetEnumerator();
        while (enumerator.MoveNext())
        {
            var workflow = (RenderWorkflow)enumerator.Key;
            if (workflow?.GetStep(stepId) != null)
            {
                return workflow;
            }
        }

        return null;
    }
    
    public void UpdateWorkflow(RenderWorkflow workflow)
    {
        if (workflow.ProjectId == CurrentProjectId)
        {
            var workflowId = workflow.Id;
            var workflows = new RenderWorkflow[WorkflowStatusListByWorkflow.Count];
            var statusLists = new List<WorkflowStatus>[WorkflowStatusListByWorkflow.Count];
            WorkflowStatusListByWorkflow.Keys.CopyTo(workflows, 0);
            WorkflowStatusListByWorkflow.Values.CopyTo(statusLists, 0);
            for (var i = 0; i < WorkflowStatusListByWorkflow.Count; i++)
            {
                var keyWorkflow = workflows[i];
                if (keyWorkflow.Id == workflowId)
                {
                    var statusList = statusLists[i];
                    WorkflowStatusListByWorkflow.Remove(keyWorkflow);
                    WorkflowStatusListByWorkflow.Insert(i, workflow, statusList);
                    break;
                }
            }

            ProjectWorkflow = workflow;
        }
    }
    
    public async Task ReplaceWorkflowStatus(
        Section section,
        Step newStep,
        RenderWorkflow workflow,
        Dictionary<Guid, List<Guid>> stepsWithWork)
    {
        var statusList = FullWorkflowStatusList;
        var sectionId = section.Id;
        var workflowStatuses = statusList.Where(x => x.ParentSectionId == sectionId).ToList();

        var newWorkflowStatus = await CreateWorkflowStatusAsync(workflow.Id, sectionId, CurrentProjectId,
            newStep.Id, section.ScopeId, GetStageFromWorkflow(workflow, newStep.Id).Id,
            newStep.RenderStepType, false);
            
        FullWorkflowStatusList.Add(newWorkflowStatus);
        statusList.Add(newWorkflowStatus);
        if (newStep.RenderStepType != RenderStepTypes.HoldingTank &&
            stepsWithWork.ContainsKey(newStep.Id))
        {
            stepsWithWork[newStep.Id].Add(section.Id);
        }

        await MarkAsCompletedAndUpdateRepository(workflowStatuses);
    }
    
    public async Task SetRemoveWorkState(Stage stage, RenderWorkflow workflow)
    {
        workflow.DeactivateStage(stage, StageState.RemoveWork);
        await _workflowRepository.SaveWorkflowAsync(workflow);
        UpdateWorkflow(workflow);
    }
    
    public async Task SetHasNewMessageForWorkflowStep(Section section, Step step, bool value)
    {
        var workflowStatus = GetWorkflowStatus(section, step);

        if (workflowStatus != null && workflowStatus.HasNewMessages != value)
        {
            workflowStatus.SetHasNewMessages(value);
            await _workflowStatusRepository.UpsertAsync(workflowStatus.Id, workflowStatus);
        }
    }

    public async Task SetHasNewDraftsForWorkflowStep(Section section, Step step, bool value)
    {
        var workflowStatus = GetWorkflowStatus(section, step);

        if (workflowStatus != null && workflowStatus.HasNewDrafts != value)
        {
            workflowStatus.SetHasNewDraft(value);
            await _workflowStatusRepository.UpsertAsync(workflowStatus.Id, workflowStatus);
        }
    }
    
    public bool NeedToShowDraftNotesOnConsultantCheck(RenderWorkflow workflow, Guid stageId, Section section)
    {
        if (workflow == null) return false;
        var allStages = workflow.GetAllStages();

        var firstConsultantCheckStage = allStages.FirstOrDefault(s => s.StageType == StageTypes.ConsultantCheck);

        if (firstConsultantCheckStage == null || firstConsultantCheckStage.Id != stageId)
        {
            return false;
        }

        var needToShowDraftNotesOnConsultantCheck = allStages.Count > 2 && allStages[1].Id == stageId;
        if (needToShowDraftNotesOnConsultantCheck is false)
        {
            var conversations = section.Passages.SelectMany(passage => passage.CurrentDraftAudio.Conversations);
            needToShowDraftNotesOnConsultantCheck = conversations.Any(conversation => conversation.StageId == stageId);
        }

        return needToShowDraftNotesOnConsultantCheck;
    }
    
    private void AddChildWorkflows(Guid parentWorkflowId, List<RenderWorkflow> remainingWorkflows,
        ref List<RenderWorkflow> orderedWorkflows)
    {
        var nextWorkflows = remainingWorkflows
            .Where(x => x.ParentWorkflowId == parentWorkflowId).ToList();
        foreach (var workflow in nextWorkflows)
        {
            orderedWorkflows.Insert(0, workflow);
            remainingWorkflows.Remove(workflow);
        }

        foreach (var workflow in nextWorkflows)
        {
            AddChildWorkflows(workflow.Id, remainingWorkflows, ref orderedWorkflows);
        }
    }
    
    private Stage GetStageFromWorkflow(RenderWorkflow workflow, Guid stepId)
    {
        var stages = workflow.GetAllStages();
        foreach (var stage in stages)
        {
            if (stage.GetStep(stepId) != null)
            {
                return stage;
            }
        }

        return null;
    }
    
    private WorkflowStatus GetWorkflowStatus(Section section, Step step)
    {
        var workflow = FindWorkflow(step.Id);

        return GetWorkflowStatus(workflow, section, step);
    }
}