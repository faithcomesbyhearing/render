using Render.Models.Sections;
using Render.Models.Workflow;
using Render.Models.Workflow.Stage;
using Render.Repositories.Extensions;
using Render.Repositories.SectionRepository;
using Render.Services.InterpretationService;
using Render.Services.WorkflowService;

namespace Render.Services.StageService;

public class StageService : IStageService
{
    private readonly IWorkflowService _workflowService;
    private readonly ISectionRepository _sectionRepository;
    private readonly IInterpretationService _interpretationService;

    /// <summary>
    /// Responsible for displaying stages on the Home Screen.
    /// </summary>
    /// <returns>
    /// Dictionary of StepIds and List of Section Ids that are currentrly in that step 
    /// </returns>
    public Dictionary<Guid, List<Guid>> StepsWithWork { get; } = new();

    /// <summary>
    /// The stages on 'Processes' tab of the 'Section Status' screen that should not be displayed because they have been removed
    /// </summary>
    private IEnumerable<Guid> StagesToHide { get; set; } = new List<Guid>();

    public StageService(
        IWorkflowService workflowService,
        ISectionRepository sectionRepository,
        IInterpretationService interpretationService)
    {
        _workflowService = workflowService;
        _sectionRepository = sectionRepository;
        _interpretationService = interpretationService;
    }


    /// <summary>
    /// Populates the StepsWithWork dictionary with step types based on what the current user is assigned
    /// </summary>
    public void BuildStepsWithWork(Guid userId)
    {
        StepsWithWork.Clear();
        var stepIds = new List<Guid>();
        var enumerator = _workflowService.WorkflowStatusListByWorkflow.GetEnumerator();
        while (enumerator.MoveNext())
        {
            var workflow = (RenderWorkflow)enumerator.Key;
            stepIds.AddRange(
                FindWorkflowStepsAssignedForUser(userId, workflow)
                    .Select(s => s.StepId));
        }

        //Duplicates may be present in step type list
        foreach (var stepId in stepIds)
        {
            if (!StepsWithWork.ContainsKey(stepId))
            {
                StepsWithWork.Add(stepId, new List<Guid>());
            }
        }
    }

    public async Task<Step> GetStepToWorkAsync(Guid sectionId, RenderStepTypes stepType, Guid currentProjectId)
    {
        var enumerator = _workflowService.WorkflowStatusListByWorkflow.GetEnumerator();
        while (enumerator.MoveNext())
        {
            var workflow = (RenderWorkflow)enumerator.Key;
            var statusList = (List<WorkflowStatus>)enumerator.Value;
            //Find the status by the step type, but if we don't find one that matches, we're likely at a holding tank
            var status = statusList?.FirstOrDefault(x => x.ParentSectionId == sectionId && !x.IsCompleted
                                                                                        && x.CurrentStepType == stepType)
                         ?? statusList?.FirstOrDefault(x => x.ParentSectionId == sectionId && !x.IsCompleted
                                                                                           && x.CurrentStepType == RenderStepTypes.HoldingTank);

            var step = workflow?.GetStep(status?.CurrentStepId ?? Guid.Empty);
            if (step != null)
            {
                //If status objects for this section are in the holding tank, it's ready to advance
                if (_workflowService.AreAllWorkflowStatusObjectsForSectionAtHoldingTank(step, sectionId, statusList))
                {
                    //Should only be one next step after a parallel step (by design)
                    var nextSteps = workflow.GetNextSteps(step.Id);
                    if (status != null)
                    {
                        var needsInterpretation = await _interpretationService.IsNeedsInterpretation(
                            workflow,
                            status.ParentSectionId,
                            step,
                            nextSteps);
                        step = CheckIfNextStepNeedsInterpretation(workflow, nextSteps, step.Id,
                            status.ParentSectionId, needsInterpretation).First();
                    }

                    if (step.RenderStepType == stepType)
                    {
                        await _workflowService.MoveForwardSectionAndGetStepAfterHoldingTankAsync(statusList, workflow, step, sectionId, currentProjectId);
                        return step;
                    }
                }

                //If the step type matches, we found the step we'll be working in
                if (step.RenderStepType == stepType)
                {
                    return step;
                }
            }
        }

        return null;
    }

    public void HideDeactivatedStagesAndSteps()
    {
        var stepsNotDisplayedAtHomePage = new List<Guid>();
        var stagesNotDisplayedAtSectionStatusProcesses = new List<Guid>();

        var inactiveSteps = GetInvactiveStepsForCurrentUser();
        stepsNotDisplayedAtHomePage.AddRange(inactiveSteps);

        var deactivatedStages = _workflowService.ProjectWorkflow.GetCustomStages(true)
            .Where(stage => !stage.IsActive)
            .Select(stage => stage.Id)
            .ToList();

        if (!deactivatedStages.Any())
        {
            foreach (var stepToRemove in stepsNotDisplayedAtHomePage)
            {
                StepsWithWork.Remove(stepToRemove);
            }
            return;
        }

        var stagesWithWork = GetCompleteWorkStagesWithWork();
        var notUsedAndDeactivatedStages = deactivatedStages.Except(stagesWithWork);

        foreach (var stageId in notUsedAndDeactivatedStages)
        {
            stagesNotDisplayedAtSectionStatusProcesses.Add(stageId);

            var stepsInDeactivatedStage = StepsWithWork.Keys.Where(stepId => _workflowService.ProjectWorkflow.GetStage(stepId).Id == stageId);

            foreach (var step in stepsInDeactivatedStage)
            {
                if (!stepsNotDisplayedAtHomePage.Contains(step))
                {
                    stepsNotDisplayedAtHomePage.Add(step);
                }
            }
        }

        foreach (var stepToRemove in stepsNotDisplayedAtHomePage)
        {
            StepsWithWork.Remove(stepToRemove);
        }

        StagesToHide = stagesNotDisplayedAtSectionStatusProcesses;
    }

    public IEnumerable<(Guid StageId, Guid StepId)> FindWorkflowStepsAssignedForUser(Guid userId,
        RenderWorkflow workflow)
    {
        var assignments = new List<(Guid stageId, Guid stepId)>();
        var teams = workflow.GetTeams();
        var stages = workflow.GetAllStages(true);

        var userIsTranslator = teams.Any(t => t.TranslatorId == userId);

        foreach (var stage in stages)
        {
            if (stage.StageType == StageTypes.Drafting && userIsTranslator)
            {
                assignments.AddRange(stage.Steps.Select(s => (stage.Id, s.Id)));
                continue;
            }

            var steps = stage.GetAllWorkflowEntrySteps(false);

            assignments.AddRange(steps
                .Where(s => teams.Any(t => t.WorkflowAssignments.Any(a => a.UserId == userId
                                                                          && a.StageId == stage.Id
                                                                          && a.Role == s.Role)))
                .Select(s => (stage.Id, s.Id)));

            if (stage.ReviseStep != null && userIsTranslator)
            {
                assignments.Add((stage.Id, stage.ReviseStep.Id));
            }
        }

        return assignments;
    }

    public async Task OrganizeSectionsIntoSteps(Guid userId)
    {
        var sectionIdsFoundInOtherWorkflows = new List<Guid>();
        var enumerator = _workflowService.WorkflowStatusListByWorkflow.GetEnumerator();
        while (enumerator.MoveNext())
        {
            var sectionIdsFoundInThisWorkflow = new List<Guid>();
            var workflow = (RenderWorkflow)enumerator.Key;
            var statusList = (List<WorkflowStatus>)enumerator.Value;

            //We need to find the exact stage ids the user is assigned under in case there are identical stages in the workflow
            var stageIds = FindWorkflowStepsAssignedForUser(userId, workflow)
                .Select(x => x.StageId).ToList();
            //filter out statuses that are completed, in a stage the user isn't assigned to, or isn't in a step type assigned to the user
            foreach (var status in statusList.Where(x => !x.IsCompleted
                                                         && stageIds.Contains(x.CurrentStageId)
                                                         && (StepsWithWork.ContainsKey(x.CurrentStepId)
                                                             || x.CurrentStepType == RenderStepTypes.HoldingTank)))
            {
                //If the section id is in the list, we already figured out where it's at in another workflow. Move along.
                if (!sectionIdsFoundInOtherWorkflows.Contains(status.ParentSectionId))
                {
                    var step = workflow.GetStep(status.CurrentStepId);
                    //If all other status objects for this section are in the holding tank, it's ready to advance
                    if (_workflowService.AreAllWorkflowStatusObjectsForSectionAtHoldingTank(step, status.ParentSectionId,
                            statusList))
                    {
                        //Should only be one next step after a parallel step (by design)
                        var nextSteps = workflow.GetNextSteps(step.Id);
                        var needsInterpretation =
                            await _interpretationService.IsNeedsInterpretation(workflow, status.ParentSectionId, step, nextSteps);
                        step = CheckIfNextStepNeedsInterpretation(workflow, nextSteps, step.Id,
                            status.ParentSectionId, needsInterpretation).First();
                    }

                    //Uses the new step if value was re-assigned in previous if
                    if (step.RenderStepType != RenderStepTypes.HoldingTank)
                    {
                        sectionIdsFoundInThisWorkflow.Add(status.ParentSectionId);
                        //Make sure the step is in the dictionary, and that this section was not already found to be 
                        //in this step
                        if (StepsWithWork.ContainsKey(step.Id) &&
                            !StepsWithWork[step.Id].Contains(status.ParentSectionId))
                        {
                            StepsWithWork[step.Id].Add(status.ParentSectionId);
                        }
                    }
                }
            }

            sectionIdsFoundInOtherWorkflows.AddRange(sectionIdsFoundInThisWorkflow);
        }
    }

    public List<Step> CheckIfNextStepNeedsInterpretation(
        RenderWorkflow workflow,
        List<Step> nextSteps,
        Guid currentStepId, Guid sectionId,
        bool needsInterpretation)
    {
        if (nextSteps.Count == 1
            && !needsInterpretation
            && (nextSteps.First().RenderStepType == RenderStepTypes.InterpretToConsultant
                || nextSteps.First().RenderStepType == RenderStepTypes.InterpretToTranslator))
        {
            //If the only next step is interpret and we have no notes to interpret, go to the next step
            return workflow.GetNextSteps(nextSteps.First().Id);
        }

        return nextSteps;
    }

    public async Task<List<Step>> SkipBackTranslateAndTranscribeStepsIfNeededAsync(Guid sectionId, Stage stage,
        RenderWorkflow workflow)
    {
        var section = await _sectionRepository.GetSectionWithDraftsAsync(sectionId, true, true);
        var nextSteps = new List<Step>();
        var activeSteps = stage.GetAllWorkflowEntrySteps();
        //We have to check all 4 of these steps because one of them might have been off the last revision
        var backTranslateStep = activeSteps.SingleOrDefault(x =>
            x.RenderStepType == RenderStepTypes.BackTranslate && x.Role == Roles.BackTranslate);
        var backTranslate2Step = activeSteps.SingleOrDefault(x =>
            x.RenderStepType == RenderStepTypes.BackTranslate && x.Role == Roles.BackTranslate2);
        var transcribeStep = activeSteps.SingleOrDefault(x =>
            x.RenderStepType == RenderStepTypes.Transcribe && x.Role == Roles.Transcribe);
        var transcribe2Step = activeSteps.SingleOrDefault(x =>
            x.RenderStepType == RenderStepTypes.Transcribe && x.Role == Roles.Transcribe2);
        if (backTranslateStep != null)
        {
            foreach (var passage in section.Passages.Where(x => x.CurrentDraftAudio != null))
            {
                //If segment is on and there's a passage with no segment back translations
                //OR if passage is on and there's a passage with no passage back translation
                //then we need to do back translate.
                if ((backTranslateStep.StepSettings.GetSetting(SettingType.DoSegmentBackTranslate)
                     && passage.CurrentDraftAudio.SegmentBackTranslationAudios.Count == 0) ||
                    (backTranslateStep.StepSettings.GetSetting(SettingType.DoRetellBackTranslate)
                     && passage.CurrentDraftAudio.RetellBackTranslationAudio == null))
                {
                    nextSteps.Add(backTranslateStep);
                    //Since back translate is first in the stage with no parallel steps, we can return
                    return nextSteps;
                }
            }

            //Transcribe and 2nd Step Back Translate are parallel steps and come after 1st Step Back Translate
            if (transcribeStep != null)
            {
                foreach (var passage in section.Passages.Where(x => x.CurrentDraftAudio != null))
                {
                    //If segment transcribe is on and there's a passage with no transcription on each of its
                    //segment back translations OR if passage transcribe is on and there's a passage with no
                    //transcription on its passage back translation
                    //then we need to do transcribe.
                    if (IsTranscriptionMissingForStep(transcribeStep,
                            passage.CurrentDraftAudio.SegmentBackTranslationAudios,
                            passage.CurrentDraftAudio.RetellBackTranslationAudio))
                    {
                        nextSteps.Add(transcribeStep);
                        break;
                    }
                }
            }

            if (backTranslate2Step != null)
            {
                foreach (var passage in section.Passages.Where(x => x.CurrentDraftAudio != null))
                {
                    //If segment is on and there's a passage that has a segment back translation that has no
                    //back translation OR if passage is on and there's a passage with a passage
                    //back translation that has no passage back translation
                    //then we need to do 2nd step back translate
                    if ((backTranslate2Step.StepSettings.GetSetting(SettingType.DoSegmentBackTranslate) &&
                         passage.CurrentDraftAudio.SegmentBackTranslationAudios.Any(x =>
                             x.RetellBackTranslationAudio == null)) ||
                        (backTranslate2Step.StepSettings.GetSetting(SettingType.DoRetellBackTranslate) &&
                         passage.CurrentDraftAudio.RetellBackTranslationAudio.RetellBackTranslationAudio == null))
                    {
                        nextSteps.Add(backTranslate2Step);
                        //If we need to do 2nd step back translate, we have to finish it before we can do 
                        //2nd step transcribe, so we can return here
                        return nextSteps;
                    }
                }

                //2nd Step Transcribe must come after 2nd Step Back Translate
                if (transcribe2Step != null)
                {
                    foreach (var passage in section.Passages.Where(x => x.CurrentDraftAudio != null))
                    {
                        //If segment transcribe is on and there's a passage with no transcription on each of its
                        //2nd step segment back translations OR if passage transcribe is on and there's a passage
                        //with no transcription on its 2nd step passage back translation
                        //then we need to do 2nd step transcribe.
                        if (IsTranscriptionMissingForStep(transcribe2Step,
                                passage.CurrentDraftAudio.SegmentBackTranslationAudios
                                    .Select(x => x.RetellBackTranslationAudio),
                                passage.CurrentDraftAudio.RetellBackTranslationAudio.RetellBackTranslationAudio))
                        {
                            nextSteps.Add(transcribe2Step);
                            break;
                        }
                    }
                }
            }
        }

        //If there are no back translate or transcribe steps needed, return the step after the holding tank
        if (nextSteps.IsNullOrEmpty() && stage.StageType == StageTypes.ConsultantCheck)
        {
            nextSteps.AddRange(workflow.GetNextSteps(stage.Steps
                .Single(x => x.RenderStepType == RenderStepTypes.HoldingTank).Id));
        }

        return nextSteps;
    }

    public IEnumerable<Stage> GetAllStages(RenderWorkflow workflow)
    {
        return workflow.GetAllStages(true).Where(i => !StagesToHide.Contains(i.Id));
    }

    public List<Guid> StepsAssignedToUser()
    {
        return StepsWithWork.Keys.ToList();
    }

    public List<Guid> SectionsAtStep(Guid stepId)
    {
        //TODO: We're missing a way to order the sections in terms of priority
        var sections = new List<Guid>();
        if (StepsWithWork.ContainsKey(stepId))
        {
            sections.AddRange(StepsWithWork[stepId]);
        }

        return sections;
    }
    
    public List<Guid> GetAllAssignedSectionAtStep(Guid stepId, RenderStepTypes renderStepType)
    {
        if (_workflowService.ProjectWorkflow.AllSectionAssignments == null || !_workflowService.ProjectWorkflow.AllSectionAssignments.Any())
            return new List<Guid>();

        var sectionsOnStep = _workflowService.FullWorkflowStatusList
            .Where(x => x.CurrentStepId == stepId && x.CurrentStepType == renderStepType)
            .Select(x => x.ParentSectionId)
            .ToList();

        return sectionsOnStep.Select(assignedSection => _workflowService.ProjectWorkflow.AllSectionAssignments
                .Where(x => x.SectionId == assignedSection)
                .Select(x => x.SectionId))
            .SelectMany(x => x)
            .ToList();
    }

    public bool IsUserAssignedToStepInNextStage(RenderWorkflow workflow, WorkflowStatus status, Guid userId)
    {
        var stepId = status.CurrentStepId;
        var stepIds = FindWorkflowStepsAssignedForUser(userId, workflow).Select(x => x.StepId).ToList();

        var stepsInNextStage = workflow.GetFirstStepsInNextStage(stepId);
        if (stepsInNextStage.IsNullOrEmpty())
        {
            return false;
        }

        var isWorkForUserExistOnNextStage = stepsInNextStage.Any(x => stepIds.Contains(x.Id));
        return isWorkForUserExistOnNextStage;
    }

    public bool IsUserAssignedToNextStep(RenderWorkflow workflow, WorkflowStatus status, Guid userId)
    {
        var stepId = status.CurrentStepId;
        var stepIds = FindWorkflowStepsAssignedForUser(userId, workflow).Select(x => x.StepId).ToList();

        var nextSteps = workflow.GetNextSteps(stepId);
        if (nextSteps.IsNullOrEmpty())
        {
            return false;
        }

        if (nextSteps.Count == 1 && nextSteps[0].RenderStepType == RenderStepTypes.HoldingTank)
        {
            nextSteps = workflow.GetNextSteps(nextSteps[0].Id);
        }

        var isUserAssignedToNextSteps = nextSteps.Any(x => stepIds.Contains(x.Id));
        return isUserAssignedToNextSteps;
    }
    
    private IEnumerable<Guid> GetInvactiveStepsForCurrentUser()
    {
        return StepsWithWork.Keys.Select(stepId => stepId)
            .Where(IsInactiveStep);
    }

    private bool IsInactiveStep(Guid stepId)
    {
        var step = _workflowService.ProjectWorkflow.GetStep(stepId);
        return step != null && !step.IsActive();
    }

    private IEnumerable<Guid> GetCompleteWorkStagesWithWork()
    {
        return _workflowService.ProjectWorkflow.GetCustomStages(true)
            .Where(stage => stage.IsCompleteWork)
            .SelectMany(completeWorkStage => _workflowService.FullWorkflowStatusList
                .Where(status => !status.IsCompleted && status.CurrentStageId == completeWorkStage.Id)
                .Select(status => completeWorkStage.Id));
    }

    private bool IsTranscriptionMissingForStep(
        Step step,
        IEnumerable<BackTranslation> segmentBackTranslations,
        RetellBackTranslation retellBackTranslation)
    {
        return (step.StepSettings.GetSetting(SettingType.DoSegmentTranscribe)
                && segmentBackTranslations.Any(x => string.IsNullOrEmpty(x.Transcription))) ||
               (step.StepSettings.GetSetting(SettingType.DoPassageTranscribe)
                && string.IsNullOrEmpty(retellBackTranslation.Transcription));
    }
}