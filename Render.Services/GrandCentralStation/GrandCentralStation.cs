using Render.Models.Sections;
using Render.Models.Workflow;
using Render.Services.SectionMovementService;
using Render.Services.SnapshotService;
using Render.Services.StageService;
using Render.Services.WorkflowService;

namespace Render.Services.GrandCentralStation
{
    public class GrandCentralStation : IGrandCentralStation
    {
        private readonly IWorkflowService _workflowService;
        private readonly IStageService _stageService;
        private readonly ISectionMovementService _sectionMovementService;
        private readonly ISnapshotService _snapshotService;

        private Guid _userId;
        public Guid CurrentProjectId { get; private set; }

        public GrandCentralStation(
            IWorkflowService workflowService,
            IStageService stageService,
            ISectionMovementService sectionMovementService,
            ISnapshotService snapshotService)
        {
            _sectionMovementService = sectionMovementService;
            _workflowService = workflowService;
            _stageService = stageService;
            _snapshotService = snapshotService;
        }

        public async Task FindWorkForUser(Guid projectId, Guid userId)
        {
            CurrentProjectId = projectId;
            _userId = userId;

            await _workflowService.PopulateWorkflowLists(userId, projectId);

            _stageService.BuildStepsWithWork(userId);
            await _stageService.OrganizeSectionsIntoSteps(userId);

            FilterSectionsForAssignments();
            await CheckForWorkAtRemovedStepsOrStages();

            await _snapshotService.CreateMissingSnapshotsForStagesWithPreviousRemovedStage(userId);

            _stageService.HideDeactivatedStagesAndSteps();
        }

        public void ResetWorkForUser()
        {
            CurrentProjectId = default;
            _userId = default;
        }

        private async Task CheckForWorkAtRemovedStepsOrStages()
        {
            if (_workflowService.WorkflowStatusListByWorkflow is null || _workflowService.WorkflowStatusListByWorkflow.Count < 1)
            {
                return;
            }

            var renderWorkflows = _workflowService.WorkflowStatusListByWorkflow.Keys.Cast<RenderWorkflow>().ToList();
            var workflowStatusLists = _workflowService.WorkflowStatusListByWorkflow.Values.Cast<List<WorkflowStatus>>().ToList();

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
                            .Contains(ws.ParentSectionId) || _stageService.IsUserAssignedToStepInNextStage(workflow, ws, _userId))).ToList();
                    await _sectionMovementService.AdvanceSectionsToNextActiveStepsAsync(workflow, workflow.GetFirstStepsInNextStage,
                        statusList, statusListPerStageAndCurrentUser, CurrentProjectId);
                }

                // Check for section in CompleteWorkState stages
                var completeWorkStages = allStages.Where(s => s.IsCompleteWork);
                foreach (var completeWorkStage in completeWorkStages)
                {
                    var stageHasInCompletedWork = statusList.Any(status => !status.IsCompleted
                                                                           && status.CurrentStageId == completeWorkStage.Id
                                                                           && assignedSections.Contains(status.ParentSectionId));

                    if (stageHasInCompletedWork) continue;
                    // If a stage is in the CompleteWork state and there are no sections in this stage, that means the stage is essentially in the RemoveWork state.
                    await _workflowService.SetRemoveWorkState(completeWorkStage, workflow);
                }

                //Check for work in inactive steps in all the other stages
                var remainingStages = allStages.Where(s => s.State != StageState.RemoveWork).ToList();
                foreach (var stage in remainingStages)
                {
                    foreach (var step in stage.GetAllWorkflowEntrySteps(false).Where(step => !step.IsActive()))
                    {
                        var statusListPerStepAndCurrentUser = statusList
                            .Where(ws => !ws.IsCompleted && ws.CurrentStepId == step.Id && (assignedSections
                                .Contains(ws.ParentSectionId) || _stageService.IsUserAssignedToNextStep(workflow, ws, _userId))).ToList();
                        await _sectionMovementService.AdvanceSectionsToNextActiveStepsAsync(workflow, workflow.GetNextSteps,
                            statusList, statusListPerStepAndCurrentUser, CurrentProjectId);
                    }
                }
            }
        }

        private void FilterSectionsForAssignments()
        {
            foreach (var stepPair in _stageService.StepsWithWork.ToList())
            {
                var enumerator = _workflowService.WorkflowStatusListByWorkflow.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    var workflow = (RenderWorkflow)enumerator.Key;
                    var step = workflow?.GetStep(stepPair.Key);
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

                        _stageService.StepsWithWork[stepPair.Key] = newSectionList.OrderBy(x => x.Priority)
                            .Select(s => s.SectionId).ToList();
                    }
                }
            }
        }

        public void Dispose()
        {
            _snapshotService?.Dispose();
            _sectionMovementService?.Dispose();
        }
    }
}