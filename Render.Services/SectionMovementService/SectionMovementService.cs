using Render.Models.Sections;
using Render.Models.Workflow;
using Render.Repositories.Extensions;
using Render.Repositories.SectionRepository;
using Render.Services.InterpretationService;
using Render.Services.SessionStateServices;
using Render.Services.SnapshotService;
using Render.Services.StageService;
using Render.Services.WorkflowService;

namespace Render.Services.SectionMovementService;

public class SectionMovementService : ISectionMovementService
{
    private readonly IWorkflowService _workflowService;
    private readonly IStageService _stageService;
    private readonly ISectionRepository _sectionRepository;
    private readonly ISnapshotService _snapshotService;
    private readonly ICommunityTestService _communityTestService;
    private readonly IInterpretationService _interpretationService;

    public SectionMovementService(
        IWorkflowService workflowService,
        IStageService stageService,
        ISectionRepository sectionRepository,
        ISnapshotService snapshotService,
        ICommunityTestService communityTestService,
        IInterpretationService interpretationService)
    {
        _workflowService = workflowService;
        _stageService = stageService;
        _sectionRepository = sectionRepository;
        _snapshotService = snapshotService;
        _communityTestService = communityTestService;
        _interpretationService = interpretationService;
    }

    public async Task AdvanceSectionAsync(
        Section section,
        Step step,
        Guid currentProjectId,
        Guid userId)
    {
        var workflow = _workflowService.FindWorkflow(step.Id);
        var nextSteps = workflow.GetNextSteps(step.Id);

        var needsInterpretation = await _interpretationService.IsNeedsInterpretation(workflow, section.Id, step, nextSteps);
        var statusList = (List<WorkflowStatus>)_workflowService.WorkflowStatusListByWorkflow[workflow];
        nextSteps = _stageService.CheckIfNextStepNeedsInterpretation(workflow, nextSteps, step.Id, section.Id, needsInterpretation);

        await _snapshotService.RemoveOldTemporarySnapshot(workflow, section, step.Id, nextSteps, userId);
        await AdvanceSectionToStepsAsync(nextSteps, section, step.Id, workflow, statusList, needsInterpretation, currentProjectId);
    }

    public async Task AdvanceSectionAfterRecoveryAsync(
        Section section,
        Step step,
        Guid currentProjectId,
        Guid userId)
    {
        var workflow = _workflowService.FindWorkflow(step.Id);
        var nextSteps = workflow.GetFirstStepsInNextStage(step.Id); // jump to next stage
        var needsInterpretation = (await _interpretationService.FindMessagesNeedingInterpretation(workflow, section.Id, step.Id)).Any();
        var statusList = (List<WorkflowStatus>)_workflowService.WorkflowStatusListByWorkflow[workflow];
        nextSteps = _stageService.CheckIfNextStepNeedsInterpretation(workflow, nextSteps, step.Id, section.Id, needsInterpretation);

        await _snapshotService.RemoveOldTemporarySnapshot(workflow, section, step.Id, nextSteps, userId,
            needToCreateSnapshotForCurrentStep: false);

        await AdvanceSectionToStepsAsync(
            nextSteps,
            section,
            step.Id,
            workflow,
            statusList,
            needsInterpretation, 
            currentProjectId);
    }

    public async Task AdvanceSectionAfterReviewAsync(
        Section section,
        Step step,
        Guid currentProjectId,
        Guid userId)
    {
        var workflow = _workflowService.FindWorkflow(step.Id);
        var workflowStatus = _workflowService.GetWorkflowStatus(workflow, section, step);
        
        if (workflowStatus.HasNewMessages)
        {
            await AdvanceSectionAsync(section, step, currentProjectId, userId);
        }
        else
        {
            var nextSteps = workflow.GetFirstStepsInNextStage(step.Id);
            var needsInterpretation = (await _interpretationService.FindMessagesNeedingInterpretation(workflow, section.Id, step.Id)).Any();
            var statusList = (List<WorkflowStatus>)_workflowService.WorkflowStatusListByWorkflow[workflow];
            nextSteps = _stageService.CheckIfNextStepNeedsInterpretation(workflow, nextSteps, step.Id, section.Id, needsInterpretation);

            await AdvanceSectionToStepsAsync(nextSteps, section, step.Id, workflow, statusList, needsInterpretation, currentProjectId);
            await _snapshotService.RemoveOldTemporarySnapshot(workflow, section, step.Id, nextSteps, userId);
        }
    }

    public async Task AdvanceSectionAfterReviseAsync(
        Section section,
        Step step,
        ISessionStateService sessionStateService,
        Guid currentProjectId,
        Guid userId)
    {
        var workflow = _workflowService.FindWorkflow(step.Id);
        var stage = workflow.GetStage(step.Id);
        
        if (stage.StageSettings.GetSetting(SettingType.LoopByDefault))
        {
            //If we need to force a loop OR there are new drafts OR there are new notes, loop back to review
            if (!stage.StageSettings.GetSetting(SettingType.TranslatorCanSkipCheck) ||
                _workflowService.GetWorkflowStatus(workflow, section, step).HasNewMessages ||
                _workflowService.GetWorkflowStatus(workflow, section, step).HasNewDrafts)
            {
                await LoopSectionAfterReviseAsync(section, step, sessionStateService, currentProjectId);
                return;
            }
        }

        await AdvanceSectionAsync(section, step, currentProjectId, userId);
    }
    
    public async Task AdvanceSectionsToNextActiveStepsAsync(
        RenderWorkflow workflow,
        Func<Guid, List<Step>> getNextSteps,
        List<WorkflowStatus> allWorkflowStatuses,
        List<WorkflowStatus> statusListOfSectionsToAdvance,
        Guid currentProjectId)
    {
        foreach (var workflowStatus in statusListOfSectionsToAdvance)
        {
            var section = await _sectionRepository.GetSectionWithDraftsAsync(
                workflowStatus.ParentSectionId, true, true);
            if (section is null)
            {
                continue;
            }

            var stepId = workflowStatus.CurrentStepId;

            var nextSteps = getNextSteps.Invoke(stepId);
            var needsInterpretation = (await _interpretationService.FindMessagesNeedingInterpretation(workflow, section.Id, stepId)).Any();
            nextSteps = _stageService.CheckIfNextStepNeedsInterpretation(workflow, nextSteps, stepId, section.Id, needsInterpretation);

            await AdvanceSectionToStepsAsync(
                nextSteps,
                section,
                stepId,
                workflow,
                allWorkflowStatuses,
                needsInterpretation,
                currentProjectId);

            //If we just advanced a section into a holding tank step, we need to make sure it's listed as being 
            //in the step after the holding tank
            if (nextSteps.Count == 1 && nextSteps[0].RenderStepType == RenderStepTypes.HoldingTank
                                     && _workflowService.AreAllWorkflowStatusObjectsForSectionAtHoldingTank(nextSteps[0], section.Id, allWorkflowStatuses))
            {
                //Should only be one next step after a parallel step (by design)
                var stepsAfterHoldingTank = workflow.GetNextSteps(nextSteps[0].Id);
                var step = _stageService.CheckIfNextStepNeedsInterpretation(workflow,
                    stepsAfterHoldingTank,
                    nextSteps[0].Id,
                    section.Id,
                    needsInterpretation).First();

                if (_stageService.StepsWithWork.ContainsKey(step.Id))
                {
                    _stageService.StepsWithWork[step.Id].Add(section.Id);
                }
            }
        }
    }
    
    private async Task LoopSectionAfterReviseAsync(
        Section section,
        Step step,
        ISessionStateService sessionStateService,
        Guid currentProjectId)
    {
        //I've made the assumption that I don't need to check for a match with the workflow status, which might be bad
        var workflow = _workflowService.FindWorkflow(step.Id);
        var stage = _workflowService.FindWorkflow(step.Id).GetStage(step.Id);
        var nextSteps = await _stageService.SkipBackTranslateAndTranscribeStepsIfNeededAsync(section.Id, stage, workflow);
        if (nextSteps.IsNullOrEmpty())
        {
            nextSteps = workflow.GetFirstStepsInStage(step.Id);
        }

        var needsInterpretation = (await _interpretationService.FindMessagesNeedingInterpretation(workflow, section.Id, step.Id)).Any();
        var statusList = (List<WorkflowStatus>)_workflowService.WorkflowStatusListByWorkflow[workflow];
        nextSteps = _stageService.CheckIfNextStepNeedsInterpretation(workflow, nextSteps, step.Id, section.Id, needsInterpretation);

        await AdvanceSectionToStepsAsync(nextSteps, section, step.Id, workflow, statusList, needsInterpretation, currentProjectId);

        // reset selected passage on peer check
        await sessionStateService?.ResetPassageNumberForSectionStep(
            nextSteps.Any() ? nextSteps.First().Id : Guid.Empty, section.Id)!;
    }

    private async Task AdvanceSectionToStepsAsync(
        List<Step> nextSteps,
        Section section,
        Guid currentStepId,
        RenderWorkflow workflow,
        List<WorkflowStatus> statusList,
        bool needsInterpretation, 
        Guid currentProjectId)
    {
        var sectionId = section.Id;
        var currentStage = workflow.GetStage(currentStepId);
        var workflowStatus = statusList.Where(x => x.CurrentStepId == currentStepId
                                                   && x.ParentSectionId == sectionId).ToList();

        if (_stageService.StepsWithWork.ContainsKey(currentStepId))
        {
            _stageService.StepsWithWork[currentStepId].Remove(section.Id);
        }

        if (!nextSteps.Any() && workflowStatus.Count > 0)
        {
            // if there is a conflict mid stage we may have duplicate workflow statuses
            await _workflowService.MarkAsCompletedAndUpdateRepository(workflowStatus);

            return;
        }

        foreach (var nextStep in nextSteps)
        {
            var nextStage = workflow.GetStage(nextStep.Id);
            var newWorkflowStatus = await _workflowService.CreateWorkflowStatusAsync(
                workflow.Id,
                sectionId, currentProjectId,
                nextStep.Id, section.ScopeId, nextStage.Id,
                nextStep.RenderStepType, needsInterpretation);
            statusList.Add(newWorkflowStatus);

            if (nextStep.RenderStepType != RenderStepTypes.HoldingTank &&
                _stageService.StepsWithWork.ContainsKey(nextStep.Id) && !_stageService.StepsWithWork[nextStep.Id].Contains(section.Id))
            {
                //TODO: Missing a way to check if now all status objects are in the holding tank outside of calling FindWorkForUser again
                _stageService.StepsWithWork[nextStep.Id].Add(section.Id);
            }

            if (currentStage.StageType == StageTypes.CommunityTest
                && nextStage.StageType == StageTypes.CommunityTest
                && currentStage.Id != nextStage.Id)
            {
                await _communityTestService.CopyFlagsAsync(currentStage.Id, nextStage.Id, sectionId);
            }
        }

        if (workflowStatus.Count > 0)
        {
            // if there is a conflict mid stage we may have duplicate workflow statuses
            await _workflowService.MarkAsCompletedAndUpdateRepository(workflowStatus);
        }

        //check whether we have parallel steps
        var remainingPendingWorkflowStatuses = statusList.Where(x => x.ParentSectionId == sectionId).ToList();
        if (remainingPendingWorkflowStatuses.Count > 1)
        {
            var readyToMoveToTheNextNonParallelStep =
                remainingPendingWorkflowStatuses.All(x => x.CurrentStepType == RenderStepTypes.HoldingTank);
            if (readyToMoveToTheNextNonParallelStep)
            {
                //removes all completed statuses except last one, which uses to define what next non-parallel step is
                //logic below prevents the situation when there are more than one next step after a parallel step (by design)
                foreach (var readyStatus in remainingPendingWorkflowStatuses.SkipLast(1))
                {
                    statusList.Remove(readyStatus);
                    await _workflowService.RemoveStatus(readyStatus);
                }
            }
        }
    }

    public void Dispose()
    {
        _sectionRepository?.Dispose();
        _snapshotService?.Dispose();
    }
}