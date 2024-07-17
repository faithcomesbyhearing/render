using System.Collections.Specialized;

using DynamicData;

using Render.Interfaces;
using Render.Models.Sections;
using Render.Models.Snapshot;
using Render.Models.Workflow;
using Render.Models.Workflow.Stage;
using Render.Repositories.Extensions;
using Render.Repositories.Kernel;
using Render.Repositories.SectionRepository;
using Render.Repositories.SnapshotRepository;
using Render.Repositories.WorkflowRepositories;
using Render.Services.SessionStateServices;

namespace Render.Services
{
    public class GrandCentralStation : IGrandCentralStation
    {
        private readonly ICommunityTestService _communityTestService;

        private IWorkflowRepository WorkflowRepository { get; }
        private IDataPersistence<WorkflowStatus> WorkflowStatusRepository { get; }
        private ISnapshotRepository SnapshotRepository { get; }
        private ISectionRepository SectionRepository { get; }
        private readonly IRenderLogger _logger;

        /// <summary>
        /// The stages on 'Processes' tab of the 'Section Status' screen that should not be displayed because they have been removed
        /// </summary>
        private IEnumerable<Guid> StagesToHide { get; set; } = new List<Guid>();

        /// <summary>
        /// Responsible for displaying stages on the Home Screen.
        /// </summary>
        /// <returns>
        /// Dictionary of StepIds and List of Section Ids that are currentrly in that step 
        /// </returns>
        private Dictionary<Guid, List<Guid>> StepsWithWork { get; } =
            new Dictionary<Guid, List<Guid>>();

        private OrderedDictionary WorkflowStatusListByWorkflow { get; } = new OrderedDictionary();

        private Guid _userId;

        public List<WorkflowStatus> FullWorkflowStatusList { get; }

        public Guid CurrentProjectId { get; private set; }

        public RenderWorkflow ProjectWorkflow { get; private set; }

        public GrandCentralStation(
            IWorkflowRepository workflowRepository,
            IDataPersistence<WorkflowStatus> workflowStatusRepository,
            ISnapshotRepository snapshotRepository,
            ISectionRepository sectionRepository,
            ICommunityTestService communityTestService,
            IRenderLogger logger)
        {
            _communityTestService = communityTestService;
            _logger = logger;

            WorkflowRepository = workflowRepository;
            WorkflowStatusRepository = workflowStatusRepository;
            SnapshotRepository = snapshotRepository;
            SectionRepository = sectionRepository;
            FullWorkflowStatusList = new List<WorkflowStatus>();
        }

        public async Task FindWorkForUser(Guid projectId, Guid userId)
        {
            CurrentProjectId = projectId;
            _userId = userId;

            WorkflowStatusListByWorkflow.Clear();

            var projectWorkflowStatusObjects = (await WorkflowStatusRepository
                .QueryOnFieldAsync("ProjectId", CurrentProjectId.ToString(), 0)).Where(x => !x.IsCompleted);

            var projectWorkflows = await WorkflowRepository
                .GetAllWorkflowsForProjectIdAsync(CurrentProjectId);

            ProjectWorkflow = projectWorkflows.SingleOrDefault(x => x.ParentWorkflowId == Guid.Empty);

            if (ProjectWorkflow == null)
            {
                _logger.LogInfo("Project workflow is null", new Dictionary<string, string>
                {
                    {"LoggedInUserId", userId.ToString()},
                    {"ProjectId", projectId.ToString()}
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

            BuildStepsWithWork();
            OrganizeSectionsIntoSteps();

            FilterSectionsForAssignments();
            await CheckForWorkAtRemovedStepsOrStages();

            await CreateMissingSnapshotsForStagesWithPreviousRemovedStage();

            HideDeactivatedStages();
        }

        /// <summary>
        /// Create snapshot for stages with missing snapshot when previous stage was removed (RemoveWork).
        /// </summary>
        /// <returns></returns>
        private async Task CreateMissingSnapshotsForStagesWithPreviousRemovedStage()
        {
            if (WorkflowStatusListByWorkflow?.Count < 1)
            {
                return;
            }

            var renderWorkflows = WorkflowStatusListByWorkflow.Keys.Cast<RenderWorkflow>().ToList();
            var workflowStatusLists = WorkflowStatusListByWorkflow.Values.Cast<List<WorkflowStatus>>().ToList();

            for (int i = 0; i < renderWorkflows.Count; i++)
            {
                var workflow = renderWorkflows[i];
                var allStages = workflow.GetCustomStages(true).ToArray();
                var stageSteps = FindWorkflowStepsAssignedForUser(_userId, workflow);

                if (allStages.Count() < 2 || stageSteps.Count() < 1) { continue; }
                
                foreach (var stepPair in StepsWithWork)
                {
                    var stepId = stepPair.Key;

                    foreach (var sectionId in stepPair.Value)
                    {
                        if (stageSteps.All(s => s.StepId != stepId)) { continue; }
                        var stageId = stageSteps.Where(s => s.StepId == stepId).First().StageId;

                        if (allStages.All(s => s.Id != stageId)) { continue; }
						var stage = allStages.First(s => s.Id == stageId);
                        var stageIndex = allStages.IndexOf(stage);

                        var previousStage = stageIndex >= 1 ? allStages[stageIndex - 1] : null;

                        if (previousStage == null || previousStage.State != StageState.RemoveWork)
                        {
                            continue;
                        }

                        var snapshots = await SnapshotRepository.GetSnapshotsForSectionAsync(sectionId);
                        var filteredSnapshots = SnapshotRepository.FilterSnapshotByStageId(snapshots, stageId);

                        if (filteredSnapshots.Count > 0)
                        {
                            continue;
                        }

                        var section = await SectionRepository.GetSectionWithReferencesAsync(sectionId);
						// create temp snapshot for the current step, we do not need to create snapshot for the next stage (RemoveOldTemporarySnapshot)
						// becouse this will be done during current stage finalization and this will cause snapshot conflict
						await CreateSnapshotAsync(section, stageId, stepId, stage.Name, deleteFlag: true);
                    }
                }
            } 
        }

        private void FilterSectionsForAssignments()
        {
            foreach (var stepPair in StepsWithWork.ToList())
            {
                var enumerator = WorkflowStatusListByWorkflow.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    var workflow = (RenderWorkflow)enumerator.Key;
                    var step = workflow.GetStep(stepPair.Key);
                    if (step != null)
                    {
                        var stage = workflow.GetStage(step.Id);
                        var newSectionList = new List<SectionAssignment>();
                        foreach (var id in stepPair.Value)
                        {
                            var team = workflow.GetTeams().SingleOrDefault(x =>
                                x.SectionAssignments.Any(y => y.SectionId == id));
                            var workflowAssignment = team?.WorkflowAssignments.SingleOrDefault(y =>
                                y.Role == step.Role && y.StageId == stage.Id && y.UserId == _userId);
                            if (workflowAssignment != null
                                || (step.Role == Roles.Drafting && team != null && team.TranslatorId == _userId))
                            {
                                var assignment = team.SectionAssignments.FirstOrDefault(x => x.SectionId == id);
                                if (assignment != null)
                                {
                                    newSectionList.Add(assignment);
                                }
                            }
                        }

                        StepsWithWork[stepPair.Key] = newSectionList.OrderBy(x => x.Priority)
                            .Select(s => s.SectionId).ToList();
                    }
                }
            }
        }

        private void HideDeactivatedStages()
        {
            var stepsNotDisplayedAtHomePage = new List<Guid>();
            var stagesNotDisplayedAtSectionStatusProcesses = new List<Guid>();

            var inactiveSteps = GetInvactiveStepsForCurrentUser();
            stepsNotDisplayedAtHomePage.AddRange(inactiveSteps);

            var deactivatedStages = ProjectWorkflow.GetCustomStages(true)
                .Where(stage => !stage.IsActive)
                .Select(stage => stage.Id);

            if (!deactivatedStages.Any()) return;

            var stagesWithWork = GetCompleteWorkStagesWithWork();
            var notUsedAndDeactivatedStages = deactivatedStages.Except(stagesWithWork);

            IEnumerable<Guid> stepsInDeactivatedStage;
            foreach (var stageId in notUsedAndDeactivatedStages)
            {
                stagesNotDisplayedAtSectionStatusProcesses.Add(stageId);

                stepsInDeactivatedStage = StepsWithWork.Keys.Where(stepId => ProjectWorkflow.GetStage(stepId).Id == stageId);
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

        /// <summary>
        /// Populates the StepsWithWork dictionary with step types based on what the current user is assigned
        /// </summary>
        private void BuildStepsWithWork()
        {
            StepsWithWork.Clear();
            var userAssignments = new List<WorkflowAssignment>();
            var stepIds = new List<Guid>();
            var enumerator = WorkflowStatusListByWorkflow.GetEnumerator();
            while (enumerator.MoveNext())
            {
                var workflow = (RenderWorkflow)enumerator.Key;
                stepIds.AddRange(
                    FindWorkflowStepsAssignedForUser(_userId, workflow)
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

        private IEnumerable<(Guid StageId, Guid StepId)> FindWorkflowStepsAssignedForUser(Guid userId,
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

        private void OrganizeSectionsIntoSteps()
        {
            var sectionIdsFoundInOtherWorkflows = new List<Guid>();
            var enumerator = WorkflowStatusListByWorkflow.GetEnumerator();
            while (enumerator.MoveNext())
            {
                var sectionIdsFoundInThisWorkflow = new List<Guid>();
                var workflow = (RenderWorkflow)enumerator.Key;
                var statusList = (List<WorkflowStatus>)enumerator.Value;

                //We need to find the exact stage ids the user is assigned under in case there are identical stages in the workflow
                var stageIds = FindWorkflowStepsAssignedForUser(_userId, workflow)
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
                        if (AreAllWorkflowStatusObjectsForSectionAtHoldingTank(step, status.ParentSectionId, statusList))
                        {
                            //Should only be one next step after a parallel step (by design)
                            var nextSteps = workflow.GetNextSteps(step.Id);
                            step = CheckIfNextStepNeedsInterpretation(workflow, nextSteps, step.Id,
                                status.ParentSectionId, statusList, false).First();
                        }

                        //Uses the new step if value was re-assigned in previous if
                        if (step.RenderStepType != RenderStepTypes.HoldingTank)
                        {
                            sectionIdsFoundInThisWorkflow.Add(status.ParentSectionId);
                            //Make sure the step is in the dictionary, and that this section was not already found to be 
                            //in this step
                            if (StepsWithWork.ContainsKey(step.Id) && !StepsWithWork[step.Id].Contains(status.ParentSectionId))
                            {
                                StepsWithWork[step.Id].Add(status.ParentSectionId);
                            }
                        }
                    }
                }

                sectionIdsFoundInOtherWorkflows.AddRange(sectionIdsFoundInThisWorkflow);
            }
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

        private bool AreAllWorkflowStatusObjectsForSectionAtHoldingTank(Step step, Guid sectionId,
            List<WorkflowStatus> statusList)
        {
            return step != null && step.RenderStepType == RenderStepTypes.HoldingTank
                                && statusList.Where(x => x.ParentSectionId == sectionId)
                                    .All(x => x.CurrentStepId == step.Id);
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
            if (ProjectWorkflow.AllSectionAssignments == null || !ProjectWorkflow.AllSectionAssignments.Any())
                return new List<Guid>();

            var sectionsOnStep = FullWorkflowStatusList.Where(x => x.CurrentStepId == stepId && x.CurrentStepType == renderStepType)
                .Select(x => x.ParentSectionId)
                .ToList();

            return sectionsOnStep.Select(assignedSection => ProjectWorkflow.AllSectionAssignments.Where(x => x.SectionId == assignedSection)
                .Select(x => x.SectionId))
                .SelectMany(x => x)
                .ToList();
        }

        private async Task<Dictionary<Draft, List<Message>>> FindMessagesNeedingInterpretation(
            Section section, Guid stepId)
        {
            var workflow = FindWorkflow(stepId);
            var stage = workflow.GetStage(stepId);

            return await FindMessagesNeedingInterpretation(section, stage.Id, stepId, stage.StageType);
        }

        public async Task<Dictionary<Draft, List<Message>>> FindMessagesNeedingInterpretation(
            Section section,
            Guid stageId,
            Guid stepId,
            StageTypes stageType)
        {
            var backTranslateSecondStep = ProjectWorkflow.GetAllActiveWorkflowEntrySteps().SingleOrDefault(step => step.Role is Roles.BackTranslate2);
            var isTwoStepBackTranslateEnabled = backTranslateSecondStep != null && 
                (backTranslateSecondStep.StepSettings.GetSetting(SettingType.DoRetellBackTranslate) ||
                 backTranslateSecondStep.StepSettings.GetSetting(SettingType.DoSegmentBackTranslate));

            section = await SectionRepository.GetSectionWithDraftsAsync(section.Id, true, true);

            var dictionary = new Dictionary<Draft, List<Message>>();
            foreach (var passage in section.Passages)
            {
                //Check for messages on draft
                if (stageType == StageTypes.Drafting || stageType == StageTypes.ConsultantCheck && NeedToShowDraftNotesOnConsultantCheck(stageId, stepId))
                {
                    FindMessagesInDraft(passage.CurrentDraftAudio, dictionary);
                }

                //Check for messages on retell
                if (isTwoStepBackTranslateEnabled)
                {
                    if (passage.CurrentDraftAudio.RetellBackTranslationAudio != null)
                    {
                        FindMessagesInDraft(passage.CurrentDraftAudio.RetellBackTranslationAudio, dictionary);
                    }

                    //Check for messages on segments
                    foreach (var segment in passage.CurrentDraftAudio.SegmentBackTranslationAudios)
                    {
                        FindMessagesInDraft(segment, dictionary);
                    }
                }
            }

            return dictionary;
        }

        private void FindMessagesInDraft(Draft draft, Dictionary<Draft, List<Message>> dictionary)
        {
            foreach (var message in draft.Conversations.SelectMany(conversation => conversation.Messages))
            {
                if (message.NeedsInterpretation)
                {
                    if (!dictionary.ContainsKey(draft))
                    {
                        dictionary.Add(draft, new List<Message>());
                    }

                    dictionary[draft].Add(message);
                }
            }
        }

        private RenderWorkflow FindWorkflow(Guid stepId)
        {
            var enumerator = WorkflowStatusListByWorkflow.GetEnumerator();
            while (enumerator.MoveNext())
            {
                var workflow = (RenderWorkflow)enumerator.Key;
                if (workflow.GetStep(stepId) != null)
                {
                    return workflow;
                }
            }

            return null;
        }

        public async Task AdvanceSectionAsync(Section section, Step step)
        {
            var workflow = FindWorkflow(step.Id);
            var nextSteps = workflow.GetNextSteps(step.Id);

            var needsInterpretation = await IsNeedsInterpretation(workflow, section, step, nextSteps);
            var statusList = (List<WorkflowStatus>)WorkflowStatusListByWorkflow[workflow];
            nextSteps = CheckIfNextStepNeedsInterpretation(workflow, nextSteps, step.Id, section.Id, statusList, needsInterpretation);

            await RemoveOldTemporarySnapshot(workflow, section, step.Id, nextSteps);
            await AdvanceSectionToStepsAsync(nextSteps, section, step.Id, workflow, statusList, needsInterpretation);
        }

        private async Task<bool> IsNeedsInterpretation(RenderWorkflow workflow, Section section, Step step, List<Step> nextSteps)
        {
            var btSteps = workflow.GetAllActiveWorkflowEntrySteps()
                .Where(x => x.RenderStepType == RenderStepTypes.BackTranslate).ToList();

            // Outgoing notes from "Consultant Check" sub-stage should trigger note interpretation
            // if note interpret is turned on, regardless if there is no BT step or single BT step.
            var currentStep = workflow.GetStep(step.Id);

            var needFindMessages =  workflow.GetAllActiveWorkflowEntrySteps()
                .Any(x => x.RenderStepType == RenderStepTypes.InterpretToConsultant
                          || x.RenderStepType == RenderStepTypes.InterpretToTranslator);

            var messageToInterpretExists = (await FindMessagesNeedingInterpretation(section, step.Id)).Any();

            if (currentStep.RenderStepType == RenderStepTypes.ConsultantCheck && btSteps.Count <= 1)
            {
                return needFindMessages && messageToInterpretExists;
            }

            if (currentStep.RenderStepType == RenderStepTypes.Draft && nextSteps.Any() && !btSteps.Any() && needFindMessages)
            {
                var nextStage = workflow.GetStage(nextSteps.First().Id);

                if (nextStage.StageType == StageTypes.ConsultantCheck)
                {
                    return messageToInterpretExists;
                }
            }

            return btSteps.Count >= 1 && messageToInterpretExists;
        }

        public async Task AdvanceSectionAfterRecoveryAsync(Section section, Step step)
        {
            var workflow = FindWorkflow(step.Id);
            var nextSteps = workflow.GetFirstStepsInNextStage(step.Id); // jump to next stage
            var needsInterpretation = (await FindMessagesNeedingInterpretation(section, step.Id)).Any();
            var statusList = (List<WorkflowStatus>)WorkflowStatusListByWorkflow[workflow];
            nextSteps = CheckIfNextStepNeedsInterpretation(workflow, nextSteps, step.Id, section.Id, statusList, needsInterpretation);

            await RemoveOldTemporarySnapshot(workflow, section, step.Id, nextSteps, needToCreateSnapshotForCurrentStep: false);
            await AdvanceSectionToStepsAsync(nextSteps, section, step.Id, workflow, statusList, needsInterpretation);
        }

        public async Task AdvanceSectionAfterReviseAsync(Section section, Step step, ISessionStateService sessionStateService)
        {
            var workflow = FindWorkflow(step.Id);
            var stage = workflow.GetStage(step.Id);

            if (stage.StageSettings.GetSetting(SettingType.LoopByDefault))
            {
                //If we need to force a loop OR there are new drafts OR there are new notes, loop back to review
                if (!stage.StageSettings.GetSetting(SettingType.TranslatorCanSkipCheck) ||
                    GetWorkflowStatus(workflow, section, step).HasNewMessages ||
                    GetWorkflowStatus(workflow, section, step).HasNewDrafts)
                {
                    await LoopSectionAfterReviseAsync(section, step, sessionStateService);
                    return;
                }
            }

            await AdvanceSectionAsync(section, step);
        }

        private async Task LoopSectionAfterReviseAsync(Section section, Step step, ISessionStateService sessionStateService)
        {
            //I've made the assumption that I don't need to check for a match with the workflow status, which might be bad
            var workflow = FindWorkflow(step.Id);
            var stage = workflow.GetStage(step.Id);
            var nextSteps = await SkipBackTranslateAndTranscribeStepsIfNeededAsync(section.Id, stage, workflow);
            if (nextSteps.IsNullOrEmpty())
            {
                nextSteps = workflow.GetFirstStepsInStage(step.Id);
            }

            var needsInterpretation = (await FindMessagesNeedingInterpretation(section, step.Id)).Any();
            var statusList = (List<WorkflowStatus>)WorkflowStatusListByWorkflow[workflow];
            nextSteps = CheckIfNextStepNeedsInterpretation(workflow, nextSteps, step.Id, section.Id, statusList,
                needsInterpretation);

            await AdvanceSectionToStepsAsync(nextSteps, section, step.Id, workflow, statusList, needsInterpretation);

            // reset selected passage on peer check
            await sessionStateService?.ResetPassageNumberForSectionStep(nextSteps.Any() ? nextSteps.First().Id : Guid.Empty, section.Id);
        }

        public async Task AdvanceSectionAfterReviewAsync(Section section, Step step)
        {
            var workflow = FindWorkflow(step.Id);

            if (GetWorkflowStatus(workflow, section, step).HasNewMessages)
            {
                await AdvanceSectionAsync(section, step);
            }
            else
            {
                var nextSteps = workflow.GetFirstStepsInNextStage(step.Id);
                var needsInterpretation = (await FindMessagesNeedingInterpretation(section, step.Id)).Any();
                var statusList = (List<WorkflowStatus>)WorkflowStatusListByWorkflow[workflow];
                nextSteps = CheckIfNextStepNeedsInterpretation(workflow, nextSteps, step.Id, section.Id, statusList,
                    needsInterpretation);

                await AdvanceSectionToStepsAsync(nextSteps, section, step.Id, workflow, statusList, needsInterpretation);
                await RemoveOldTemporarySnapshot(workflow, section, step.Id, nextSteps);
            }
        }

        private async Task AdvanceSectionToStepsAsync(List<Step> nextSteps, Section section, Guid currentStepId,
            RenderWorkflow workflow, List<WorkflowStatus> statusList, bool needsInterpretation)
        {
            var sectionId = section.Id;
            var currentStage = workflow.GetStage(currentStepId);
            var workflowStatus = statusList.Where(x => x.CurrentStepId == currentStepId
                                                    && x.ParentSectionId == sectionId).ToList();

            if (StepsWithWork.ContainsKey(currentStepId))
            {
                StepsWithWork[currentStepId].Remove(section.Id);
            }

            if (!nextSteps.Any() && workflowStatus.Count > 0)
            {
                // if there is a conflict mid stage we may have duplicate workflow statuses
                foreach (var status in workflowStatus)
                {
                    status.MarkAsCompleted();
                    statusList.Remove(status);
                    await WorkflowStatusRepository.UpsertAsync(status.Id, status);
                }
                return;
            }

            foreach (var nextStep in nextSteps)
            {
                var nextStage = workflow.GetStage(nextStep.Id);
                var newWorkflowStatus = await CreateWorkflowStatusAsync(workflow.Id, sectionId, CurrentProjectId,
                    nextStep.Id, section.ScopeId, nextStage.Id,
                    nextStep.RenderStepType, needsInterpretation);
                statusList.Add(newWorkflowStatus);
                if (nextStep.RenderStepType != RenderStepTypes.HoldingTank &&
                    StepsWithWork.ContainsKey(nextStep.Id) && !StepsWithWork[nextStep.Id].Contains(section.Id))
                {
                    //TODO: Missing a way to check if now all status objects are in the holding tank outside of calling FindWorkForUser again
                    StepsWithWork[nextStep.Id].Add(section.Id);
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
                foreach (var status in workflowStatus)
                {
                    status.MarkAsCompleted();
                    statusList.Remove(status);
                    await WorkflowStatusRepository.UpsertAsync(status.Id, status);
                }
            }

            //check whether we have parallel steps
            var remainingPendingWorkflowStatuses = statusList.Where(x => x.ParentSectionId == sectionId).ToList();
            if (remainingPendingWorkflowStatuses.Count > 1)
            {
                var readyToMoveToTheNextNonParallelStep = remainingPendingWorkflowStatuses.All(x => x.CurrentStepType == RenderStepTypes.HoldingTank);
                if (readyToMoveToTheNextNonParallelStep)
                {
                    //removes all completed statuses except last one, which uses to define what next non-parallel step is
                    //logic below prevents the situation when there are more than one next step after a parallel step (by design)
                    foreach (var readyStatus in remainingPendingWorkflowStatuses.SkipLast(1))
                    {
                        statusList.Remove(readyStatus);
                        await WorkflowStatusRepository.DeleteAsync(readyStatus.Id);
                    }
                }
            }
        }

        public async Task ReplaceWorkflowStatus(Section section, Step newStep, RenderWorkflow workflow)
        {
            var statusList = FullWorkflowStatusList;
            var sectionId = section.Id;
            var workflowStatuses = statusList.Where(x => x.ParentSectionId == sectionId).ToList();

            var newWorkflowStatus = await CreateWorkflowStatusAsync(workflow.Id, sectionId, CurrentProjectId,
                newStep.Id, section.ScopeId, GetStageFromWorkflow(workflow, newStep.Id).Id,
                newStep.RenderStepType, false);
            statusList.Add(newWorkflowStatus);
            if (newStep.RenderStepType != RenderStepTypes.HoldingTank &&
                StepsWithWork.ContainsKey(newStep.Id))
            {
                StepsWithWork[newStep.Id].Add(section.Id);
            }

            foreach (var workflowStatus in workflowStatuses)
            {
                workflowStatus.MarkAsCompleted();
                statusList.Remove(workflowStatus);
                await WorkflowStatusRepository.UpsertAsync(workflowStatus.Id, workflowStatus);
            }
        }

        public Stage GetStageFromWorkflow(RenderWorkflow workflow, Guid stepId)
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

        private async Task CreateSnapshotAsync(
            Section section,
            Guid stageId,
            Guid stepId,
            string stageName,
            Guid parentSnapshotId = default,
            bool deleteFlag = false)
        {
            var sectionReferenceAudioSnapshots = new List<SectionReferenceAudioSnapshot>();
            if (section.References != null)
            {
                sectionReferenceAudioSnapshots = section.References.Select(
                    reference => new SectionReferenceAudioSnapshot(reference.Id, reference.LockedReferenceByPassageNumbersList,
                        reference.PassageReferences.ToList())).ToList();
            }

            var noteInterpretationIds = section.GetAllNoteInterpretationIdsForSection();
            var snapshot = new Snapshot(section.Id, section.CheckedBy, section.ApprovedBy, section.ApprovedDate,
                _userId, section.ScopeId, section.ProjectId, stageId, stepId, section.Passages.ToList(),
                sectionReferenceAudioSnapshots, noteInterpretationIds, stageName, parentSnapshotId, deleteFlag);
            await SnapshotRepository.SaveAsync(snapshot);
        }

        public async Task<bool> CheckForSnapshotConflicts(Guid sectionId)
        {
            var conflictedStageIds = await GetConflictedSnapshots(sectionId);
            return conflictedStageIds.Any();
        }

        public async Task<Stage> GetConflictedStage(Guid sectionId)
        {
            var conflictedStageIds = await GetConflictedSnapshots(sectionId);
            return ProjectWorkflow.GetAllStages().FirstOrDefault(x => x.Id == conflictedStageIds.FirstOrDefault());
        }

        private async Task<List<Guid>> GetConflictedSnapshots(Guid sectionId)
        {
            var snapshots = await SnapshotRepository.GetPermanentSnapshotsForSectionAsync(sectionId);
            return snapshots.GroupBy(x => x.StageId)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key).ToList();
        }

        /// <summary>
        /// This method gets all sections at a particular step that do not have snapshot conflicts. This allows us
        /// to skip sections with conflicts both in displaying the icon color, and navigation.
        /// </summary>
        public async Task<List<Guid>> FilterOutConflicts(Step step)
        {
            var allSectionsAtStep = SectionsAtStep(step.Id);
            //Check for snapshot conflicts
            var sectionsAtStep = new List<Guid>();
            foreach (var sectionId in allSectionsAtStep)
            {
                if (!await CheckForSnapshotConflicts(sectionId))
                {
                    sectionsAtStep.Add(sectionId);
                }
            }

            return sectionsAtStep;
        }

        private async Task<WorkflowStatus> CreateWorkflowStatusAsync(Guid workflowId, Guid sectionId, Guid projectId,
            Guid currentStepId, Guid scopeId, Guid currentStageId, RenderStepTypes currentStepType,
            bool hasNotesNeedingInterpretation)
        {
            var workflowStatus = new WorkflowStatus(sectionId, workflowId, projectId, currentStepId, scopeId,
                currentStageId, currentStepType);
            workflowStatus.SetHasNotesToInterpret(hasNotesNeedingInterpretation);
            await WorkflowStatusRepository.UpsertAsync(workflowStatus.Id, workflowStatus);
            FullWorkflowStatusList.Add(workflowStatus);
            return workflowStatus;
        }

        public async Task<Step> GetStepToWorkAsync(Guid sectionId, RenderStepTypes stepType)
        {
            var enumerator = WorkflowStatusListByWorkflow.GetEnumerator();
            while (enumerator.MoveNext())
            {
                var workflow = (RenderWorkflow)enumerator.Key;
                var statusList = (List<WorkflowStatus>)enumerator.Value;
                //Find the status by the step type, but if we don't find one that matches, we're likely at a holding tank
                var status = statusList.FirstOrDefault(x => x.ParentSectionId == sectionId && !x.IsCompleted
                                                                                           && x.CurrentStepType == stepType)
                             ?? statusList.FirstOrDefault(x => x.ParentSectionId == sectionId && !x.IsCompleted
                                                                                              && x.CurrentStepType == RenderStepTypes.HoldingTank);

                var step = workflow.GetStep(status?.CurrentStepId ?? Guid.Empty);
                if (step != null)
                {
                    //If status objects for this section are in the holding tank, it's ready to advance
                    if (AreAllWorkflowStatusObjectsForSectionAtHoldingTank(step, sectionId, statusList))
                    {
                        //Should only be one next step after a parallel step (by design)
                        var nextSteps = workflow.GetNextSteps(step.Id);
                        step = CheckIfNextStepNeedsInterpretation(workflow, nextSteps, step.Id,
                            status.ParentSectionId, statusList, false).First();
                        if (step.RenderStepType == stepType)
                        {
                            await AdvanceSectionAndGetStepAfterHoldingTankAsync(statusList, workflow, step, sectionId);
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

        public async Task RemoveHoldingTankSectionStatusesAfterApproval(Guid sectionId)
        {
            //Find all Holding Tank workflow statuses leftover for the section
            var status = FullWorkflowStatusList
                .Where(x => x.ParentSectionId == sectionId && !x.IsCompleted && x.CurrentStepType == RenderStepTypes.HoldingTank).ToList();

            foreach (var workflowStatus in status)
            {
                workflowStatus.MarkAsCompleted();
                await WorkflowStatusRepository.UpsertAsync(workflowStatus.Id, workflowStatus);
            }
        }

        private async Task AdvanceSectionAndGetStepAfterHoldingTankAsync(List<WorkflowStatus> statusList,
            RenderWorkflow workflow, Step nextStep, Guid sectionId)
        {
            var hasNotesNeedingInterpret = statusList.First().SectionHasNotesToInterpret;
            var statusToRemoveList = statusList.Where(x => x.ParentSectionId == sectionId).ToList();
            var scopeId = statusToRemoveList.First().ScopeId;

            foreach (var workflowStatus in statusToRemoveList)
            {
                workflowStatus.MarkAsCompleted();
                statusList.Remove(workflowStatus);
                await WorkflowStatusRepository.UpsertAsync(workflowStatus.Id, workflowStatus);
            }

            var newWorkflowStatus = await CreateWorkflowStatusAsync(workflow.Id, sectionId, CurrentProjectId,
                nextStep.Id, scopeId, workflow.GetStage(nextStep.Id).Id,
                nextStep.RenderStepType, hasNotesNeedingInterpret);
            statusList.Add(newWorkflowStatus);
        }

        public IEnumerable<Stage> GetAllStages(RenderWorkflow workflow)
        {
            return workflow.GetAllStages(true).Where(i => !StagesToHide.Contains(i.Id));
        }

        public async Task<List<Step>> SkipBackTranslateAndTranscribeStepsIfNeededAsync(Guid sectionId, Stage stage, RenderWorkflow workflow)
        {
            var section = await SectionRepository.GetSectionWithDraftsAsync(sectionId, true, true);
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
                        if (IsTranscriptionMissingForStep(transcribeStep, passage.CurrentDraftAudio.SegmentBackTranslationAudios,
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
                             passage.CurrentDraftAudio.SegmentBackTranslationAudios.Any(x => x.RetellBackTranslationAudio == null)) ||
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

        private bool IsTranscriptionMissingForStep(Step step, IEnumerable<BackTranslation> segmentBackTranslations,
            RetellBackTranslation retellBackTranslation)
        {
            return (step.StepSettings.GetSetting(SettingType.DoSegmentTranscribe)
                    && segmentBackTranslations.Any(x => string.IsNullOrEmpty(x.Transcription))) ||
                   (step.StepSettings.GetSetting(SettingType.DoPassageTranscribe)
                    && string.IsNullOrEmpty(retellBackTranslation.Transcription));
        }

        private List<Step> CheckIfNextStepNeedsInterpretation(RenderWorkflow workflow, List<Step> nextSteps,
            Guid currentStepId, Guid sectionId, List<WorkflowStatus> statusList, bool needsInterpretation)
        {
            //Need all statuses in a case where our only note is on a retell bt (for example), only one of the two
            //statuses will show as needing interpretation
            if (nextSteps.Count == 1 && !needsInterpretation && !statusList.Any(x =>
                    x.ParentSectionId == sectionId && x.CurrentStepId == currentStepId && x.SectionHasNotesToInterpret)
                && (nextSteps.First().RenderStepType == RenderStepTypes.InterpretToConsultant
                    || nextSteps.First().RenderStepType == RenderStepTypes.InterpretToTranslator))
            {
                //If the only next step is interpret and we have no notes to interpret, go to the next step
                return workflow.GetNextSteps(nextSteps.First().Id);
            }

            return nextSteps;
        }

        public Dictionary<RenderWorkflow, Dictionary<Stage, Dictionary<Step, List<Guid>>>> GetProcessesData()
        {
            var data = new Dictionary<RenderWorkflow, Dictionary<Stage, Dictionary<Step, List<Guid>>>>();
            var sectionIdsFoundInOtherWorkflows = new List<Guid>();
            var enumerator = WorkflowStatusListByWorkflow.GetEnumerator();
            while (enumerator.MoveNext())
            {
                var sectionIdsFoundInThisWorkflow = new List<Guid>();
                var workflow = (RenderWorkflow)enumerator.Key;
                var statusList = (List<WorkflowStatus>)enumerator.Value;
                var stages = GetAllStages(workflow);
                var stepsByStage = new Dictionary<Stage, Dictionary<Step, List<Guid>>>();
                foreach (var stage in stages)
                {
                    var sectionsByStep = new Dictionary<Step, List<Guid>>();
                    foreach (var step in stage.GetAllWorkflowEntrySteps(false))
                    {
                        sectionsByStep.Add(step, new List<Guid>());
                    }

                    stepsByStage.Add(stage, sectionsByStep);
                }

                foreach (var status in statusList.Where(x => !x.IsCompleted))
                {
                    //If the section id is in the list, we already figured out where it's at in another workflow. Move along.
                    if (!sectionIdsFoundInOtherWorkflows.Contains(status.ParentSectionId))
                    {
                        var step = workflow.GetStep(status.CurrentStepId);
                        if (step != null)
                        {
                            //If all other status objects for this section are in the holding tank, it's ready to advance
                            if (AreAllWorkflowStatusObjectsForSectionAtHoldingTank(step, status.ParentSectionId, statusList))
                            {
                                //Should only be one next step after a parallel step (by design)
                                var nextSteps = workflow.GetNextSteps(step.Id);
                                step = CheckIfNextStepNeedsInterpretation(workflow, nextSteps, step.Id,
                                    status.ParentSectionId, statusList, false).First();
                            }

                            //Uses the new step if value was re-assigned in previous if
                            if (step.RenderStepType != RenderStepTypes.HoldingTank)
                            {
                                sectionIdsFoundInThisWorkflow.Add(status.ParentSectionId);
                                var stage = stepsByStage
                                    .SingleOrDefault(x => x.Key.Id == status.CurrentStageId).Key;
                                if (stage != null && stepsByStage.ContainsKey(stage) && stepsByStage[stage].ContainsKey(step)
                                    && !stepsByStage[stage][step].Contains(status.ParentSectionId))
                                {
                                    stepsByStage[stage][step].Add(status.ParentSectionId);
                                }
                            }
                        }
                    }
                }

                sectionIdsFoundInOtherWorkflows.AddRange(sectionIdsFoundInThisWorkflow);
                data.Add(workflow, stepsByStage);
            }

            return data;
        }

        private async Task CheckForWorkAtRemovedStepsOrStages()
        {
			if (WorkflowStatusListByWorkflow?.Count < 1)
			{
                return;
            }

            var renderWorkflows = WorkflowStatusListByWorkflow.Keys.Cast<RenderWorkflow>().ToList();
            var workflowStatusLists = WorkflowStatusListByWorkflow.Values.Cast<List<WorkflowStatus>>().ToList();

            for (int i = 0; i < renderWorkflows.Count; i++)
            {
                var workflow = renderWorkflows[i];
                var allStages = workflow.GetCustomStages(true);
                var statusList = workflowStatusLists[i];
                var assignedSections = workflow.AllSectionAssignments.Select(s => s.SectionId);

                //Check for sections in removed stages
                var removeWorkStages = allStages.Where(s => s.State == StageState.RemoveWork).ToList();
                foreach (var stage in removeWorkStages)
                {
                    var statusListPerStageAndCurrentUser = statusList
                        .Where(ws => !ws.IsCompleted && ws.CurrentStageId == stage.Id && (assignedSections
                            .Contains(ws.ParentSectionId) || IsUserAssignedToStepInNextStage(workflow, ws))).ToList();
                    await AdvanceSectionsToNextActiveStepsAsync(workflow, workflow.GetFirstStepsInNextStage,
                        statusList, statusListPerStageAndCurrentUser);
                }

                // Check for section in CompleteWorkState stages
                var completeWorkStages = allStages.Where(s => s.IsCompleteWork);
                foreach (var completeWorkStage in completeWorkStages)
                {
                    var stageHasIncompletedWork = statusList.Any(status => !status.IsCompleted && status.CurrentStageId == completeWorkStage.Id);
                    if (stageHasIncompletedWork) continue;
                    // If a stage is in the CompleteWork state and there are no sections in this stage, that means the stage is essentially in the RemoveWork state.
                    await SetRemoveWorkState(completeWorkStage, workflow);
                }

                //Check for work in inactive steps in all the other stages
                var remainingStages = allStages.Where(s => s.State != StageState.RemoveWork).ToList();
                foreach (var stage in remainingStages)
                {
                    foreach (var step in stage.GetAllWorkflowEntrySteps(false).Where(step => !step.IsActive()))
                    {
                        var statusListPerStepAndCurrentUser = statusList
                            .Where(ws => !ws.IsCompleted && ws.CurrentStepId == step.Id && (assignedSections
                                .Contains(ws.ParentSectionId) || IsUserAssignedToNextStep(workflow, ws))).ToList();
                        await AdvanceSectionsToNextActiveStepsAsync(workflow, workflow.GetNextSteps,
                            statusList, statusListPerStepAndCurrentUser);
                    }
                }
            }
        }

        private bool IsUserAssignedToStepInNextStage(RenderWorkflow workflow, WorkflowStatus status)
        {
            var stepId = status.CurrentStepId;
            var stepIds = FindWorkflowStepsAssignedForUser(_userId, workflow).Select(x => x.StepId).ToList();

            var stepsInNextStage = workflow.GetFirstStepsInNextStage(stepId);
            if (stepsInNextStage.IsNullOrEmpty())
            {
                return false;
            }

            var isWorkForUserExistOnNextStage = stepsInNextStage.Any(x => stepIds.Contains(x.Id));
            return isWorkForUserExistOnNextStage;
        }

        private bool IsUserAssignedToNextStep(RenderWorkflow workflow, WorkflowStatus status)
        {
            var stepId = status.CurrentStepId;
            var stepIds = FindWorkflowStepsAssignedForUser(_userId, workflow).Select(x => x.StepId).ToList();

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

        private async Task AdvanceSectionsToNextActiveStepsAsync(RenderWorkflow workflow, Func<Guid, List<Step>> getNextSteps,
            List<WorkflowStatus> allWorkflowStatuses, List<WorkflowStatus> statusListOfSectionsToAdvance)
        {
            foreach (var workflowStatus in statusListOfSectionsToAdvance)
            {
                var section = await SectionRepository.GetSectionWithDraftsAsync(
                    workflowStatus.ParentSectionId, true, true);
                if (section is null)
                {
                    continue;
                }
                var stepId = workflowStatus.CurrentStepId;

                var nextSteps = getNextSteps.Invoke(stepId);
                var needsInterpretation = (await FindMessagesNeedingInterpretation(section, stepId)).Any();
                nextSteps = CheckIfNextStepNeedsInterpretation(workflow, nextSteps, stepId, section.Id, allWorkflowStatuses,
                    needsInterpretation);

                await AdvanceSectionToStepsAsync(nextSteps, section, stepId, workflow, allWorkflowStatuses, needsInterpretation);

                //If we just advanced a section into a holding tank step, we need to make sure it's listed as being 
                //in the step after the holding tank
                if (nextSteps.Count == 1 && nextSteps[0].RenderStepType == RenderStepTypes.HoldingTank
                                         && AreAllWorkflowStatusObjectsForSectionAtHoldingTank(nextSteps[0], section.Id, allWorkflowStatuses))
                {
                    //Should only be one next step after a parallel step (by design)
                    var stepsAfterHoldingTank = workflow.GetNextSteps(nextSteps[0].Id);
                    var step = CheckIfNextStepNeedsInterpretation(workflow, stepsAfterHoldingTank,
                        nextSteps[0].Id, section.Id, allWorkflowStatuses, false).First();
                    if (StepsWithWork.ContainsKey(step.Id))
                    {
                        StepsWithWork[step.Id].Add(section.Id);
                    }
                }
            }
        }

        public void ResetWorkForUser()
        {
            CurrentProjectId = default;
            _userId = default;
        }

        public async Task SetHasNewMessageForWorkflowStep(Section section, Step step, bool value)
        {
            var workflowStatus = GetWorkflowStatus(section, step);

            if (workflowStatus != null && workflowStatus.HasNewMessages != value)
            {
                workflowStatus.SetHasNewMessages(value);
                await WorkflowStatusRepository.UpsertAsync(workflowStatus.Id, workflowStatus);
            }
        }

        private async Task SetHasNewDraftsForWorkflowStep(Section section, Step step, bool value)
        {
            var workflowStatus = GetWorkflowStatus(section, step);

            if (workflowStatus != null && workflowStatus.HasNewDrafts != value)
            {
                workflowStatus.SetHasNewDraft(value);
                await WorkflowStatusRepository.UpsertAsync(workflowStatus.Id, workflowStatus);
            }
        }

        private WorkflowStatus GetWorkflowStatus(Section section, Step step)
        {
            var workflow = FindWorkflow(step.Id);

            return GetWorkflowStatus(workflow, section, step);
        }

        private WorkflowStatus GetWorkflowStatus(RenderWorkflow workflow, Section section, Step step)
        {
            var statusList = (List<WorkflowStatus>)WorkflowStatusListByWorkflow[workflow];
            var workflowStatus = statusList.FirstOrDefault(x => x.CurrentStepId == step.Id
                                                                 && x.ParentSectionId == section.Id);

            return workflowStatus;
        }

        public bool NeedToShowDraftNotesOnPeerCheck(Guid stageId, Guid stepId)
        {
            var workflow = FindWorkflow(stepId);

            if (workflow == null) return false;
            var allStages = workflow.GetAllStages();

            var firstPeerCheckStage = allStages.FirstOrDefault(s => s.StageType == StageTypes.PeerCheck);

            if (firstPeerCheckStage == null || firstPeerCheckStage.Id != stageId)
            {
                return false;
            }

            // stage[0] - drafting,  peer check should be stage[1] 
            return allStages.Count > 2 && allStages[1].Id == stageId;
        }

        public bool NeedToShowDraftNotesOnConsultantCheck(Guid stageId, Guid stepId)
        {
            var workflow = FindWorkflow(stepId);

            if (workflow == null) return false;
            var allStages = workflow.GetAllStages();

            var firstConsultantCheckStage = allStages.FirstOrDefault(s => s.StageType == StageTypes.ConsultantCheck);

            if (firstConsultantCheckStage == null || firstConsultantCheckStage.Id != stageId)
            {
                return false;
            }

            return allStages.Count > 2 && allStages.Any(x => x.Id == stageId);
        }

		/// Delete old temp snapshots created in the revise loop
		private async Task RemoveOldTemporarySnapshot(RenderWorkflow workflow, Section section, Guid stepId, List<Step> nextSteps,
			bool needToCreateSnapshotForCurrentStep = true, bool needToCreateSnapshotForNextstage = true)
		{
			var currentStage = workflow.GetStage(stepId);
			var nextStage = workflow.GetStage(nextSteps.Any() ? nextSteps.First().Id : Guid.Empty);

			if (nextStage != null && nextStage.Id != currentStage.Id)
			{
				if (needToCreateSnapshotForCurrentStep)
				{
					var previousSnapshot = (await SnapshotRepository.GetPermanentSnapshotsForSectionAsync(section.Id))
						.LastOrDefault(x => x.StageId != currentStage.Id);
					var parentSnapshotId = previousSnapshot?.Id ?? Guid.Empty;
					await CreateSnapshotAsync(section, currentStage.Id, stepId, currentStage.Name, parentSnapshotId);
				}

				var snapshotsToDelete = (await SnapshotRepository.GetSnapshotsForSectionAsync(section.Id))
					.Where(x => x.Temporary).ToList();
				if (snapshotsToDelete.Any())
				{
					await SnapshotRepository.BatchSoftDeleteAsync(snapshotsToDelete, section);
				}

				// create temporary snapshot upon entry of next stage
				if (needToCreateSnapshotForNextstage)
				{
					await CreateSnapshotAsync(section, nextStage.Id, stepId, nextStage.Name, deleteFlag: true);
				}
			}
		}

		public async Task CreateTemporarySnapshotAfterReRecording(Section section, Step step)
        {
            //Look for new drafts
            var snapshot = (await SnapshotRepository.GetSnapshotsForSectionAsync(section.Id)).Last();
            snapshot = await SnapshotRepository.GetPassageDraftsForSnapshot(snapshot);
            var latestDraftIds = snapshot.PassageDrafts.Select(x => x.DraftId);
            var currentDraftIds = section.Passages.Select(x => x.CurrentDraftAudio.Id);

            if (!latestDraftIds.SequenceEqual(currentDraftIds))
            {
                //Create snapshot within the revise loop and set its delete flag to true to be cleaned up once we advance the
                //section to the another stage
                var workflow = FindWorkflow(step.Id);
                var stage = workflow.GetStage(step.Id);
                await CreateSnapshotAsync(section, stage.Id, step.Id, stage.Name, deleteFlag: true);
                await SetHasNewDraftsForWorkflowStep(section, step, true);
            }
        }

        private async Task SetRemoveWorkState(Stage stage, RenderWorkflow workflow)
        {
            workflow.DeactivateStage(stage, StageState.RemoveWork);
            await WorkflowRepository.SaveWorkflowAsync(workflow);
            UpdateWorkflow(workflow);
        }

        private IEnumerable<Guid> GetInvactiveStepsForCurrentUser()
        {
            return StepsWithWork.Keys.Select(stepId => stepId)
                                     .Where(IsInactiveStep);
        }
        private bool IsInactiveStep(Guid stepId)
        {
            var step = ProjectWorkflow.GetStep(stepId);
            return step != null && !step.IsActive();
        }

        private IEnumerable<Guid> GetCompleteWorkStagesWithWork()
        {
            return ProjectWorkflow.GetCustomStages(true)
                .Where(stage => stage.IsCompleteWork)
                .SelectMany(completeWorkStage => FullWorkflowStatusList
                    .Where(status => !status.IsCompleted && status.CurrentStageId == completeWorkStage.Id)
                    .Select(status => completeWorkStage.Id));
        }

        public void Dispose()
        {
            WorkflowRepository.Dispose();
            SnapshotRepository.Dispose();
            SectionRepository.Dispose();
            WorkflowStatusListByWorkflow.Clear();
        }
    }
}