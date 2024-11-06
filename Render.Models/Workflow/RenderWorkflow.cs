using System.Data;

using Couchbase.Management.Users;
using Couchbase.Search;

using Newtonsoft.Json;
using Render.Models.Users;
using Render.Models.Workflow.Stage;
using Render.TempFromVessel.Kernel;
using Render.TempFromVessel.User;

namespace Render.Models.Workflow
{
    public class RenderWorkflow : ProjectDomainEntity, IAggregateRoot
    {
        [JsonProperty("DraftingStage")] public DraftingStage DraftingStage { get; private set; }

        /// <summary>
        /// The stages of the workflow
        /// </summary>
        [JsonProperty("Stages")]
        private List<Stage.Stage> Stages { get; set; } = new List<Stage.Stage>();
        
        [JsonIgnore]
        private IEnumerable<Stage.Stage> ActiveStages => Stages.Where(i => i.IsActive);

        [JsonProperty("ApprovalStage")] public ApprovalStage ApprovalStage { get; private set; }

        /// <summary>
        /// The workflow that a section should be sent back to when it finishes traversing this one. If this is the
        /// project level workflow, this id is an empty guid.
        /// </summary>
        [JsonProperty("ParentWorkflowId")]
        public Guid ParentWorkflowId { get; private set; }

        [JsonProperty("Teams")] private List<Team> Teams { get; set; } = new List<Team>();

        public IReadOnlyList<Team> GetTeams() => Teams.AsReadOnly();

        [JsonIgnore]
        public IReadOnlyList<SectionAssignment> AllSectionAssignments
        {
            get => GetTeams().SelectMany(x => x.SectionAssignments).OrderBy(x => x.Priority).ToList();
        }

        public static RenderWorkflow Create(Guid projectId, Guid parentWorkflowId = default)
        {
            var workflow = new RenderWorkflow(projectId, parentWorkflowId)
            {
                DraftingStage = DraftingStage.Create(),
                ApprovalStage = ApprovalStage.Create()
            };
            workflow.AddTeam();
            workflow.AddTeam();
            return workflow;
        }

        public RenderWorkflow(Guid projectId, Guid parentWorkflowId = default)
            : base(projectId, 0)
        {
            ParentWorkflowId = parentWorkflowId;
        }

        public IReadOnlyList<Stage.Stage> GetCustomStages(bool includeDeactivatedStages = false)
        {
            return includeDeactivatedStages ? (IReadOnlyList<Stage.Stage>)Stages : ActiveStages.ToList();
        }

        public IReadOnlyList<Stage.Stage> GetAllStages(bool includeDeactivatedStages = false)
        {
            var stages = new List<Stage.Stage>();
            if (DraftingStage != null)
            {
                stages.Add(DraftingStage);
            }

            if (Stages != null)
            {
                stages.AddRange(includeDeactivatedStages ? Stages : ActiveStages);
            }

            if (ApprovalStage != null)
            {
                stages.Add(ApprovalStage);
            }

            return stages.AsReadOnly();
        }

        public void AddStage(Stage.Stage stage)
        {
            if (stage.StageType == StageTypes.CommunityTest && stage.StageSettings.GetSetting(SettingType.AssignToTranslator))
            {
                var teamsWithTranslatorsAssigned = Teams.Where(x => x.TranslatorId != Guid.Empty);
                foreach (var team in teamsWithTranslatorsAssigned)
                {
                    team.AddAssignment(new WorkflowAssignment(team.TranslatorId,
                        stage.Id,
                        StageTypes.CommunityTest,
                        Roles.Review));
                }
            }

            Stages.Add(stage);
            ReNumberStages();
        }

        private void ReNumberStages()
        {
            foreach (var stageType in (StageTypes[])Enum.GetValues(typeof(StageTypes)))
            {
                var stages = GetCustomStages().Where(x => x.StageType == stageType).ToList();

                if (stages.Count > 1)
                {
                    var number = 1;
                    foreach (var stage in stages)
                    {
                        stage.StageNumber = number;
                        number++;
                    }
                }
                else if (stages.Count == 1)
                {
                    stages.Single().StageNumber = 0;
                }
            }
        }

        public void RemoveStage(Stage.Stage stage)
        {
            Stages.Remove(stage);
            ReNumberStages();
        }
        
        public void DeactivateStage(Stage.Stage stage, StageState state)
        {
            stage.SetState(state);
            ReNumberStages();
        }

        public void InsertStage(Stage.Stage newStage, Guid priorStageId)
        {
            var index = 0;
            if (newStage.StageType == StageTypes.CommunityTest && newStage.StageSettings.GetSetting(SettingType.AssignToTranslator))
            {
                var teamsWithTranslatorsAssigned = Teams.Where(x => x.TranslatorId != Guid.Empty);
                foreach (var team in teamsWithTranslatorsAssigned)
                {
                    team.AddAssignment(new WorkflowAssignment(team.TranslatorId,
                        newStage.Id,
                        StageTypes.CommunityTest,
                        Roles.Review));
                }
            }
            if (priorStageId == DraftingStage.Id)
            {
                Stages.Insert(index, newStage);
            }
            else
            {
                foreach (var stage in Stages)
                {
                    index++;
                    if (priorStageId == stage.Id)
                    {
                        Stages.Insert(index, newStage);
                        break;
                    }
                }
            }
            ReNumberStages();
        }

        /// <summary>
        /// Recursively finds a step by its guid
        /// </summary>
        /// <returns>Yes, this is the step you're looking for.</returns>
        public Step GetStep(Guid stepId)
        {
            foreach (var stage in GetAllStages(true))
            {
                var output = stage.GetStep(stepId);
                if (output != null)
                {
                    return output;
                }
            }

            return null;
        }

        /// <summary>
        /// Recursively find the work step(s) that are next in the workflow after the current step
        /// </summary>
        public List<Step> GetNextSteps(Guid currentStepId)
        {
            var matched = false;
            
            foreach (var stage in GetAllStages(true))
            {
                if (stage.IsActive || stage.GetStep(currentStepId) != null)
                {
                    var steps = stage.GetNextSteps(currentStepId, ref matched);
                    if (steps.Any())
                    {
                        return steps;
                    }
                }
            }

            return new List<Step>();
        }
        
        public virtual Stage.Stage GetStage(Guid stepId)
        {
            var stages = GetAllStages(true);
            foreach (var stage in stages)
            {
                if (stage.GetStep(stepId) != null)
                {
                    return stage;
                }
            }

            return null;
        }

        /// <summary>
        /// Recursively find all multi steps that are workflow entry points that are active
        /// </summary>
        public List<Step> GetAllActiveWorkflowEntrySteps()
        {
            var steps = new List<Step>();
            foreach (var stage in GetAllStages())
            {
                steps.AddRange(stage.GetAllWorkflowEntrySteps());
            }

            return steps;
        }

        /// <summary>
        /// Find the first step(s) that kick off the stage that the current step is in
        /// </summary>
        public List<Step> GetFirstStepsInStage(Guid currentStepId)
        {
            foreach (var stage in GetAllStages(true))
            {
                var result = stage.GetStep(currentStepId);
                if (result != null)
                {
                    var matched = true;
                    return stage.GetNextSteps(Guid.Empty, ref matched);
                }
            }

            return new List<Step>();
        }

        /// <summary>
        /// Skips the rest of the current stage and gets the step(s) at the start of the next one
        /// </summary>
        public List<Step> GetFirstStepsInNextStage(Guid currentStepId)
        {
            var matched = false;
            foreach (var stage in GetAllStages(true))
            {
                if (stage.IsActive && matched)
                {
                    return stage.GetNextSteps(Guid.Empty, ref matched);
                }

                var result = stage.GetStep(currentStepId);
                if (result != null)
                {
                    matched = true;
                }
            }

            return new List<Step>();
        }

        public void BuildDefaultWorkflow()
        {
            Stages.Add(PeerCheckStage.Create());
            Stages.Add(CommunityTestStage.Create());
            Stages.Add(ConsultantCheckStage.Create());
        }

        public Team AddTeam()
        {
            var teamNumber = 1;
            var final = false;
            while (!final)
            {
                if (Teams.Count == 0)
                {
                    break;
                }

                final = Teams.All(x => x.TeamNumber != teamNumber);
                if (!final)
                    teamNumber++;
            }

            var newTeam = new Team(teamNumber);
            var firstTeam = Teams.FirstOrDefault();
            if (firstTeam != null)
            {
                var backTranslator = firstTeam.GetWorkflowAssignmentForRole(Roles.BackTranslate);
                if (backTranslator != null)
                {
                    newTeam.AddAssignment(backTranslator);
                }

                var noteTranslator = firstTeam.GetWorkflowAssignmentForRole(Roles.NoteTranslate);
                if (noteTranslator != null)
                {
                    newTeam.AddAssignment(noteTranslator);
                }

                var consultant = firstTeam.GetWorkflowAssignmentForRole(Roles.Consultant);
                if (consultant != null)
                {
                    newTeam.AddAssignment(consultant);
                }
            }

            Teams.Add(newTeam);
            return newTeam;
        }

        public void RemoveTeam(Team team)
        {
            Teams.Remove(team);
        }

        public bool AddWorkflowAssignmentToTeam(Guid stageId, Roles role, IUser user, Team team)
        {
            //Make sure we're not trying to assign a user to check their own work as a peer
            var userId = user.Id;
            var stage = GetAllStages().FirstOrDefault(x => x.Id == stageId);
            if (stage?.StageType == StageTypes.PeerCheck && stage.StageSettings.GetSetting(SettingType.NoSelfCheck)
                                                         && role == Roles.Review && userId == team.TranslatorId)
            {
                return false;
            }

            //A global user with Consultant role - can assign any Users to any roles in Render and can assign themselves as Consultant.
            //A global user without Consultant role - can assign any Users to any roles in Render, could not assign themselves as Consultant.
            //Render Users cannot be assigned as Consultant.
            
            var workflowAssignment = new WorkflowAssignment(userId, stageId, stage.StageType, role);
            
            var canBeConsultant = user.HasClaim(RenderRolesAndClaims.ProjectUserClaimType, ProjectId.ToString(), RoleName.Consultant.GetRoleId());
            
            switch (stage?.StageType)
            {
                case StageTypes.ConsultantApproval when role == Roles.Approval:
                    if (!canBeConsultant)
                    {
                        return false;   
                    }
                    team.AddAssignment(workflowAssignment);
                    return true;
                case StageTypes.ConsultantCheck when role == Roles.Consultant:
                default:
                    team.AddAssignment(workflowAssignment);
                    return true;
            }
        }

        public void RemoveWorkflowAssignmentFromTeam(Guid stageId, Roles role, Team team)
        {
            team.RemoveAssignment(stageId, role);
        }

        public bool AddTranslationAssignmentForTeam(Team team, Guid userId)
        {
            // check to make sure we're not trying to assign a translation user to a team so that it would check its own work.
            var peerStages = ActiveStages.Where(x => x.StageType == StageTypes.PeerCheck);
            if (peerStages.Any(peerStage => peerStage.StageSettings.GetSetting(SettingType.NoSelfCheck) &&
                                            team.WorkflowAssignments.Count > 0 &&
                                            team.GetWorkflowAssignmentForStageAndRole(peerStage.Id, Roles.Review)
                                                ?.UserId == userId))
            {
                return false;
            }

            team.UpdateTranslator(userId);

            var communityTestStagesToAssignTranslator =
                ActiveStages.Where(x => x.StageType == StageTypes.CommunityTest && x.StageSettings.GetSetting(SettingType.AssignToTranslator));

            if (communityTestStagesToAssignTranslator.Any())
            {
                foreach (var stage in communityTestStagesToAssignTranslator)
                {
                    team.AddAssignment(new WorkflowAssignment(userId, stage.Id, stage.StageType, Roles.Review));
                }
            }

            return true;
        }

        /// <summary>
        /// Assign translators to community test stage teams/assignments by rewriting. AssignToTranslator setting should be on.
        /// </summary>
        public void AssignCommunityTestTranslationTeam(Guid stageId)
        {
            var communityTestStagesToAssignTranslator =
                ActiveStages.Where(x =>
                    x.Id == stageId &&
                    x.StageType == StageTypes.CommunityTest && 
                    x.StageSettings.GetSetting(SettingType.AssignToTranslator));

            if (communityTestStagesToAssignTranslator.Any())
            {
                foreach (var stage in communityTestStagesToAssignTranslator)
                {
                    foreach (Team team in Teams)
                    {
                        RemoveWorkflowAssignmentFromTeam(stage.Id, Roles.Review, team);

                        var workflowAssignment = new WorkflowAssignment(team.TranslatorId, stage.Id, stage.StageType, Roles.Review);

                        team.AddAssignment(workflowAssignment);
                    }
                }
            }
        }

        /// <summary>
        /// Clean community test stage translator teams/assignments. AssignToTranslator setting should be off.
        /// </summary>
        public void CleanCommunityTestTranslationTeam(Guid stageId)
        {
            var communityTestStagesToAssignTranslator =
                ActiveStages.Where(x =>
                    x.Id == stageId &&
                    x.StageType == StageTypes.CommunityTest &&
                    !x.StageSettings.GetSetting(SettingType.AssignToTranslator));

            if (communityTestStagesToAssignTranslator.Any())
            {
                foreach (var stage in communityTestStagesToAssignTranslator)
                {
                    foreach (Team team in Teams)
                    {
                        team.RemoveAssignment(stage.Id, Roles.Review);
                    }
                }
            }
        }

        public void RemoveTranslationTeamAssignment(Team team)
        {
            team.RemoveTranslator();

            var communityTestStagesToAssignTranslator =
                ActiveStages.Where(x => x.StageType == StageTypes.CommunityTest && x.StageSettings.GetSetting(SettingType.AssignToTranslator));

            if (communityTestStagesToAssignTranslator.Any())
            {
                foreach (var stage in communityTestStagesToAssignTranslator)
                {
                    team.RemoveAssignment(stage.Id, Roles.Review);
                }
            }
        }

        public WorkflowAssignment GetWorkflowAssignmentForTeam(Guid stageId, Roles role, Guid teamId)
        {
            var team = Teams.FirstOrDefault(x => x.Id == teamId);
            return team?.GetWorkflowAssignmentForStageAndRole(stageId, role);
        }

        /// <summary>
        /// Gets a team based on the userId for the translator of the team. If this returns null, then the user is not
        /// assigned as a translator on a team.
        /// </summary>
        /// <param name="userId">User Id that is expected to be a translator on a team.</param>
        /// <returns>A <see cref="Team"/> for the given user's Id, or null if that user is not assigned as a translator.</returns>
        public Team GetTeamForTranslatorId(Guid userId)
        {
            return Teams.FirstOrDefault(x => x.TranslatorId == userId);
        }

        public void SetStepSetting(Step step, SettingType settingType, bool value, string stringValue = "")
        {
            if (settingType.ToString().StartsWith("Do"))
            {
                switch (settingType)
                {
                    case SettingType.DoSectionListen:
                        step.StepSettings.SetSetting(SettingType.RequireSectionListen, value);
                        break;
                    case SettingType.DoPassageListen:
                        step.StepSettings.SetSetting(SettingType.RequirePassageListen, value);
                        break;
                    case SettingType.DoSectionReview:
                        step.StepSettings.SetSetting(SettingType.RequireSectionReview, value);
                        break;
                    case SettingType.DoPassageReview:
                        step.StepSettings.SetSetting(SettingType.RequirePassageReview, value);
                        break;
                    case SettingType.IsActive:
                        break;
                    case SettingType.AssignToTranslator:
                        break;
                    case SettingType.RequireSectionListen:
                        break;
                    case SettingType.RequirePassageListen:
                        break;
                    case SettingType.NoSelfCheck:
                        break;
                    case SettingType.AssignToConsultant:
                        break;
                    case SettingType.DoPassageTranscribe:
                        break;
                    case SettingType.DoSegmentTranscribe:
                        break;
                    case SettingType.RequireSectionReview:
                        break;
                    case SettingType.RequirePassageReview:
                        break;
                    case SettingType.RequireNoteListen:
                        break;
                    case SettingType.DoNoteReview:
                        step.StepSettings.SetSetting(SettingType.RequireNoteReview, value);
                        break;
                    case SettingType.RequireNoteReview:
                        break;
                    case SettingType.DoCommunityRetell:
                        break;
                    case SettingType.DoCommunityResponse:
                        break;
                    case SettingType.DoRetellBackTranslate:
                        break;
                    case SettingType.DoSegmentBackTranslate:
                        break;
                    case SettingType.RequireRetellBTSectionListen:
                        break;
                    case SettingType.RequireRetellBTPassageListen:
                        break;
                    case SettingType.DoRetellBTPassageReview:
                        break;
                    case SettingType.RequireRetellBTPassageReview:
                        break;
                    case SettingType.RequireSegmentBTSectionListen:
                        break;
                    case SettingType.RequireSegmentBTPassageListen:
                        break;
                    case SettingType.DoSegmentBTPassageReview:
                        break;
                    case SettingType.RequireSegmentBTPassageReview:
                        break;
                    case SettingType.ConsultantLanguage:
                        break;
                    case SettingType.Consultant2StepLanguage:
                        break;
                    case SettingType.SegmentConsultantLanguage:
                        break;
                    case SettingType.SegmentConsultant2StepLanguage:
                        break;
                    default: 
                        throw new ArgumentOutOfRangeException(nameof(settingType), settingType, null);
                }
            }

            step.StepSettings.SetSetting(settingType, value, stringValue);
        }

        public void SetStageSetting(Stage.Stage stage, SettingType settingType, bool value)
        {
            stage.StageSettings.SetSetting(settingType, value);
        }

        public void AddSectionAssignmentToTeam(Team team, Guid sectionId, int priority)
        {
            team.AddSectionAssignment(sectionId, priority);
        }

        private void ReorderSections()
        {
            int priority = 1;
            foreach (var sectionAssignment in AllSectionAssignments)
            {
                sectionAssignment.Priority = priority;
                priority++;
            }
        }

        public void RemoveSectionAssignmentFromTeam(Team team, Guid sectionId)
        {
            team.RemoveSectionAssignment(sectionId);
        }
    }

    public class GenericStageBuilder
    {
        private Stage.Stage Stage;

        public GenericStageBuilder()
        {
            Stage = new Stage.Stage();
        }

        public Stage.Stage GetStage()
        {
            return Stage;
        }

        public GenericStageBuilder WithInterpret()
        {
            Stage.AddSequentialReviewPreparationStep(new Step(RenderStepTypes.InterpretToConsultant,
                Roles.NoteTranslate));
            Stage.AddRevisePreparationStep(new Step(RenderStepTypes.InterpretToTranslator, Roles.NoteTranslate));
            return this;
        }

        public GenericStageBuilder WithBackTranslate(bool twoStep, bool transcribe)
        {
            var segmentMultiStep = new Step(role: Roles.BackTranslate);
            var segmentFollowup = new Step(order: Step.Ordering.Parallel);
            var segment2MultiStep = new Step(role: Roles.BackTranslate2);
            segmentMultiStep.AddStep(new Step(RenderStepTypes.BackTranslate, Roles.BackTranslate));

            if (transcribe || twoStep)
            {
                segmentMultiStep.AddStep(segmentFollowup);
            }

            if (transcribe)
            {
                segmentFollowup.AddStep(new Step(RenderStepTypes.Transcribe, Roles.Transcribe));
            }

            if (twoStep)
            {
                segmentFollowup.AddStep(segment2MultiStep);
                segment2MultiStep.AddStep(new Step(RenderStepTypes.BackTranslate, Roles.BackTranslate2));
            }

            if (transcribe && twoStep)
            {
                segment2MultiStep.AddStep(new Step(RenderStepTypes.Transcribe, Roles.Transcribe2));
            }

            Stage.AddParallelReviewPreparationStep(segmentMultiStep);
            return this;
        }

        public GenericStageBuilder WithCommunityCheckSetup()
        {
            Stage.AddParallelReviewPreparationStep(new Step(RenderStepTypes.CommunitySetup));
            return this;
        }

        public GenericStageBuilder WithReview(RenderStepTypes renderStepType)
        {
            Stage.AddReviewStep(new Step(renderStepType));
            return this;
        }

        public GenericStageBuilder WithRevise(RenderStepTypes renderStepType)
        {
            Stage.SetReviseStep(new Step(renderStepType));
            return this;
        }
    }
}