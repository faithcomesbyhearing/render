using Render.Models.Users;
using Render.Models.Workflow;
using Render.Models.Workflow.Stage;
using Render.TempFromVessel.User;
using Render.UnitTests.App.Kernel;

namespace Render.UnitTests.Domain.Models.Workflow
{
    public class WorkflowTests : TestBase
    {
        private RenderWorkflow Workflow { get; }
        private readonly IUser _user;

        public WorkflowTests()
        {
            Workflow = RenderWorkflow.Create(default);
            Workflow.BuildDefaultWorkflow();
            _user = new User("user", "user");
        }

        [Fact]
        public void GetAllActiveWorkflowEntrySteps_Succeeds()
        {
            //Arrange
            
            //Act
            var actual = Workflow.GetAllActiveWorkflowEntrySteps();
            
            //Assert
            //With 2nd step back translation off by default, this is 16. It's 20 with them on.
            actual.Count.Should().Be(10);
        }

        [Fact]
        public void GetStep_WhenStepDoesNotExist_ReturnsNull()
        {
            //Arrange
            
            //Act
            var result = Workflow.GetStep(Guid.NewGuid());
            
            //Assert
            result.Should().BeNull();
        }
        
        [Fact]
        public void GetFirstStepsInStage_WhenStepDoesNotExist_ReturnsEmptyList()
        {
            //Arrange
            
            //Act
            var result = Workflow.GetFirstStepsInStage(Guid.NewGuid());
            
            //Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public void SkipReviseAndGetFirstStepsInNextStage_WhenStepDoesNotExist_ReturnsEmptyList()
        {
            //Arrange
            
            //Act
            var result = Workflow.GetFirstStepsInNextStage(Guid.NewGuid());

            //Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public void BuildDefaultWorkflow()
        {
            //Arrange
            var newWorkflow = RenderWorkflow.Create(default);
            
            //Act
            newWorkflow.BuildDefaultWorkflow();
            
            //Assert
            newWorkflow.GetCustomStages().Count.Should().Be(3);
        }

        [Fact]
        public void AddStage_Succeeds()
        {
            //Arrange
            var workflow = RenderWorkflow.Create(Guid.Empty);
            var stage = new Stage();
            
            //Act
            workflow.AddStage(stage);
            
            //Assert
            workflow.GetAllStages()[1].Should().Be(stage);
        }
        
        [Fact]
        public void RemoveStage_Succeeds()
        {
            //Arrange
            var workflow = RenderWorkflow.Create(Guid.Empty);
            var stage = new Stage();
            workflow.AddStage(stage);
            
            //Act
            workflow.RemoveStage(stage);
            
            //Assert
            workflow.GetAllStages().Count.Should().Be(2);
        }
        
        [Fact]
        public void InsertStage_AfterDrafting_Succeeds()
        {
            //Arrange
            var workflow = RenderWorkflow.Create(Guid.Empty);
            var stage = new Stage();
            
            //Act
            workflow.InsertStage(stage, workflow.DraftingStage.Id);
            
            //Assert
            workflow.GetAllStages()[1].Should().Be(stage);
        }
        
        [Fact]
        public void InsertStage_AfterMiddleStage_Succeeds()
        {
            //Arrange
            var workflow = RenderWorkflow.Create(Guid.Empty);
            var firstStage = new Stage();
            var secondStage = new Stage();
            workflow.AddStage(firstStage);
            workflow.AddStage(secondStage);
            var newStage = new Stage();
            
            //Act
            workflow.InsertStage(newStage, firstStage.Id);
            
            //Assert
            workflow.GetAllStages()[2].Should().Be(newStage);
        }

        [Fact]
        public void Workflow_HasDefaultTeams()
        {
            //Arrange
            var workflow = RenderWorkflow.Create(Guid.Empty);
            
            //Assert
            workflow.GetTeams().Count.Should().Be(2);
            workflow.GetTeams().First().TeamNumber.Should().Be(1);
            workflow.GetTeams().Last().TeamNumber.Should().Be(2);
        }

        [Fact]
        public void AddTeam_Succeeds()
        {
            //Arrange
            var workflow = RenderWorkflow.Create(Guid.Empty);
            
            //Act
            var team = workflow.AddTeam();
            
            //Assert
            team.TeamNumber.Should().Be(3);
        }

        [Fact]
        public void AddTeam_NamesTeamCorrectly()
        {
            //Arrange
            var workflow = RenderWorkflow.Create(Guid.Empty);
            
            //Act
            workflow.RemoveTeam(workflow.GetTeams()[0]);
            var team = workflow.AddTeam();
            
            //Assert
            team.TeamNumber.Should().Be(1);
            workflow.GetTeams().Count().Should().Be(2);
            
            //Act 2
            var team2 = workflow.AddTeam();
            team2.TeamNumber.Should().Be(3);
        }

        [Fact]
        public void RemoveTeam_Succeeds()
        {
            //Arrange
            var workflow = RenderWorkflow.Create(Guid.Empty);
            
            //Act
            var team2 = workflow.GetTeams().Last();
            workflow.RemoveTeam(team2);
            
            //Assert
            workflow.GetTeams().Count.Should().Be(1);
            workflow.GetTeams()[0].TeamNumber.Should().Be(1);
        }

        [Fact]
        public void AddAssignmentToTeam_Succeeds()
        {
            //Arrange
            var workflow = RenderWorkflow.Create(Guid.Empty);
            workflow.BuildDefaultWorkflow();
            
            //Act
            var team1 = workflow.GetTeams()[0];
            var peerStage = workflow.GetCustomStages()[0];
            workflow.AddWorkflowAssignmentToTeam(peerStage.Id, Roles.Review, _user, team1);
            
            //Assert
            team1.WorkflowAssignments.Count.Should().Be(1);
            team1.WorkflowAssignments[0].Role.Should().Be(Roles.Review);
            team1.WorkflowAssignments[0].StageId.Should().Be(peerStage.Id);
            team1.WorkflowAssignments[0].UserId.Should().Be(_user.Id);
        }

        [Fact]
        public void AddTeamToBothTranslateAndPeerReview_Fails()
        {
            //Arrange
            var workflow = RenderWorkflow.Create(Guid.Empty);
            workflow.BuildDefaultWorkflow();
            
            //Act
            var team1 = workflow.GetTeams().First();
            var peerStage = workflow.GetCustomStages().First(x => x.StageType == StageTypes.PeerCheck);
            workflow.AddTranslationAssignmentForTeam(team1, _user.Id);
            
            var result = workflow.AddWorkflowAssignmentToTeam(peerStage.Id, Roles.Review, _user, team1);
            
            //Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void AddTeamToPeerAndThenTranslate_Fails()
        {
            //Arrange
            var workflow = RenderWorkflow.Create(Guid.Empty);
            workflow.BuildDefaultWorkflow();
            
            //Act
            var team1 = workflow.GetTeams().First();
            var peerStage = workflow.GetCustomStages().First(x => x.StageType == StageTypes.PeerCheck);
            workflow.AddWorkflowAssignmentToTeam(peerStage.Id, Roles.Review, _user, team1);
            var result = workflow.AddTranslationAssignmentForTeam(team1,_user.Id);
            
            //Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void GetTeamForTranslator_Succeeds()
        {
            //Arrange
            var workflow = RenderWorkflow.Create(Guid.Empty);
            var translatorId = Guid.NewGuid();
            //Act
            var team1 = workflow.AddTeam();
            team1.UpdateTranslator(translatorId);
            var result = workflow.GetTeamForTranslatorId(translatorId);
            
            //Assert
            result.Should().BeEquivalentTo(team1);
        }

        [Fact]
        public void RemoveAssignmentFromTeam_Succeeds()
        {
            //Arrange
            var workflow = RenderWorkflow.Create(Guid.Empty);
            workflow.BuildDefaultWorkflow();

            //Act
            var team1 = workflow.GetTeams().First();
            var peerStage = workflow.GetCustomStages().First();
            workflow.AddWorkflowAssignmentToTeam(peerStage.Id, Roles.Review, _user, team1);
            workflow.RemoveWorkflowAssignmentFromTeam(peerStage.Id, Roles.Review, team1);
            
            //Assert
            team1.WorkflowAssignments.Count.Should().Be(0);
        }

        [Fact]
        public void RemoveTranslationTeamAssignment_Succeeds()
        {
            //Arrange
            var workflow = RenderWorkflow.Create(Guid.Empty);
            workflow.BuildDefaultWorkflow();
            var userId = Guid.NewGuid();
            
            //Act
            var team1 = workflow.GetTeams().First();
            team1.UpdateTranslator(userId);
            
            //Assert 1
            team1.TranslatorId.Should().Be(userId);
            
            //Act 2
            workflow.RemoveTranslationTeamAssignment(team1);
            
            //Assert 2
            team1.TranslatorId.Should().Be(Guid.Empty);
        }

        [Fact]
        public void GetWorkflowAssignmentForTeam_Succeeds()
        {
            //Arrange
            var workflow = RenderWorkflow.Create(Guid.Empty);
            workflow.BuildDefaultWorkflow();
            
            //Act
            var team1 = workflow.GetTeams().First();
            var peerStage = workflow.GetCustomStages().First();
            workflow.AddWorkflowAssignmentToTeam(peerStage.Id, Roles.Review, _user, team1);
            var result = workflow.GetWorkflowAssignmentForTeam(peerStage.Id, Roles.Review, team1.Id);
            
            //Assert
            result.Role.Should().Be(Roles.Review);
            result.StageId.Should().Be(peerStage.Id);
            result.UserId.Should().Be(_user.Id);
        }

        [Fact]
        public void AddTranslationTeamAssignment_AlsoAssignsTeamToCommunityCheck()
        {
            //Arrange
            var workflow = RenderWorkflow.Create(Guid.Empty);
            workflow.BuildDefaultWorkflow();
            var userId = Guid.NewGuid();
            
            //Act
            var team1 = workflow.GetTeams().First();
            workflow.AddTranslationAssignmentForTeam(team1, userId);
            
            //Assert
            var communityCheckStage = workflow.GetCustomStages().First(x => x.StageType == StageTypes.CommunityTest);
            var communityAssignment = workflow.GetWorkflowAssignmentForTeam(communityCheckStage.Id, Roles.Review, team1.Id);
            communityAssignment.UserId.Should().Be(userId);
        }

        [Fact]
        public void RemoveTranslationTeamAssignment_AlsoRemovesTeamFromCommunityCheck()
        {
            //Arrange
            var workflow = RenderWorkflow.Create(Guid.Empty);
            workflow.BuildDefaultWorkflow();
            var userId = Guid.NewGuid();
            
            //Act
            var team1 = workflow.GetTeams().First();
            workflow.AddTranslationAssignmentForTeam(team1, userId);
            workflow.RemoveTranslationTeamAssignment(team1);
            
            //Assert
            var communityCheckStage = workflow.GetCustomStages().First(x => x.StageType == StageTypes.CommunityTest);
            var communityAssignment = workflow.GetWorkflowAssignmentForTeam(communityCheckStage.Id, Roles.Review, team1.Id);
            communityAssignment.Should().BeNull();
        }

        [Fact]
        public void AddTeam_AssignsExistingSingularRolesTOTeam()
        {
            //Arrange
            var workflow = RenderWorkflow.Create(Guid.Empty);
            workflow.BuildDefaultWorkflow();
            var backTranslator = new User("user", "user");
            var noteTranslator = new User("noteTranslator", "noteTranslator");
            var consultant = new User("consultant", "consultant");
            consultant.RoleIds.Add(RenderRolesAndClaims.GetRoleByName(RoleName.Consultant).Id);
            
            //Act
            var consultantStage = workflow.GetCustomStages().First(x => x.StageType == StageTypes.ConsultantCheck);
            foreach (var team in workflow.GetTeams())
            {
                workflow.AddWorkflowAssignmentToTeam(consultantStage.Id, Roles.BackTranslate, backTranslator, team);
                workflow.AddWorkflowAssignmentToTeam(consultantStage.Id, Roles.NoteTranslate, noteTranslator, team);
                workflow.AddWorkflowAssignmentToTeam(consultantStage.Id, Roles.Consultant, consultant, team);
            }
            var newTeam = workflow.AddTeam();
            
            //Assert
            var backTranslator1 = newTeam.GetWorkflowAssignmentForRole(Roles.BackTranslate);
            backTranslator1.UserId.Should().Be(backTranslator.Id);
            var noteTranslator1 = newTeam.GetWorkflowAssignmentForRole(Roles.NoteTranslate);
            noteTranslator1.UserId.Should().Be(noteTranslator.Id);
            var consultant1 = newTeam.GetWorkflowAssignmentForRole(Roles.Consultant);
            consultant1.UserId.Should().Be(consultant.Id);
        }

        [Fact]
        public void DraftingStage_DefaultSettings_AllStepsAreActive()
        {
            //Arrange
            var workflow = RenderWorkflow.Create(Guid.Empty);
            workflow.BuildDefaultWorkflow();
            
            //Assert
            var draftingStage = workflow.GetAllStages().Single(st => st.StageType == StageTypes.Drafting);

            foreach (var step in draftingStage.Steps)
            {
                step.StepSettings.GetSetting(SettingType.IsActive).Should().BeTrue();
            }
        }
        
        [Fact]
        public void PeerCheckStage_DefaultSettings_AllStepsAreActive()
        {
            //Arrange
            var workflow = RenderWorkflow.Create(Guid.Empty);
            workflow.BuildDefaultWorkflow();
            
            //Assert
            var peerCheckStage = workflow.GetAllStages().Single(st => st.StageType == StageTypes.PeerCheck);

            foreach (var step in peerCheckStage.Steps)
            {
                step.StepSettings.GetSetting(SettingType.IsActive).Should().BeTrue();
            }
        }
        
        [Fact]
        public void PeerCheckStage_DefaultSettings_AllowEditingForReviseStepIsOff()
        {
            //Arrange
            var workflow = RenderWorkflow.Create(Guid.Empty);
            workflow.BuildDefaultWorkflow();
            
            //Assert
            var allowEditingSetting = workflow
                .GetAllStages()
                .Single(st => st.StageType == StageTypes.PeerCheck)
                .ReviseStep
                .StepSettings
                .GetSetting(SettingType.AllowEditing);

            allowEditingSetting.Should().BeFalse();
        }
        
        [Fact]
        public void CommunityTest_DefaultSettings_AllStepsAreActive()
        {
            //Arrange
            var workflow = RenderWorkflow.Create(Guid.Empty);
            workflow.BuildDefaultWorkflow();
            
            //Assert
            var communityTestStage = workflow.GetAllStages().Single(st => st.StageType == StageTypes.CommunityTest);

            foreach (var step in communityTestStage.Steps)
            {
                step.StepSettings.GetSetting(SettingType.IsActive).Should().BeTrue();
            }
        }
        
        [Fact]
        public void CommunityTest_DefaultSettings_AllowEditingForReviseStepIsOff()
        {
            //Arrange
            var workflow = RenderWorkflow.Create(Guid.Empty);
            workflow.BuildDefaultWorkflow();
            
            //Assert
            var allowEditingSetting = workflow
                .GetAllStages()
                .Single(st => st.StageType == StageTypes.CommunityTest)
                .ReviseStep
                .StepSettings
                .GetSetting(SettingType.AllowEditing);

            allowEditingSetting.Should().BeFalse();
        }
        
        [Fact]
        public void ConsultantCheck_DefaultSettings_AllStepsButInterpretStepsAreActive()
        {
            //Arrange
            var workflow = RenderWorkflow.Create(Guid.Empty);
            workflow.BuildDefaultWorkflow();
            
            //Assert
            var consultantCheckStage = workflow.GetAllStages().Single(st => st.StageType == StageTypes.ConsultantCheck);

            foreach (var step in consultantCheckStage.Steps)
            {
                if (step.RenderStepType == RenderStepTypes.InterpretToConsultant || step.RenderStepType == RenderStepTypes.InterpretToTranslator)
                {
                    step.StepSettings.GetSetting(SettingType.IsActive).Should().BeFalse();
                }
                else
                {
                    step.StepSettings.GetSetting(SettingType.IsActive).Should().BeTrue();
                }
            }
        }

        [Fact]
        public void ConsultantCheck_DefaultSettings_AllInterpretToConsultantSettingsAreOff()
        {
            //Arrange
            var workflow = RenderWorkflow.Create(Guid.Empty);
            workflow.BuildDefaultWorkflow();
            
            //Assert
            var interpretToConsultantStepSettings = workflow
                .GetAllStages()
                .Single(st => st.StageType == StageTypes.ConsultantCheck)
                .Steps
                .Single(st => st.RenderStepType == RenderStepTypes.InterpretToConsultant)
                .StepSettings;

            foreach (var setting in interpretToConsultantStepSettings.Settings)
            {
                setting.Value.Should().BeFalse();
            }
        }
        
        [Fact]
        public void ConsultantApproval_DefaultSettings_AllStepsAreActive()
        {
            //Arrange
            var workflow = RenderWorkflow.Create(Guid.Empty);
            workflow.BuildDefaultWorkflow();
            
            //Assert
            var consultantApprovalStage = workflow.GetAllStages().Single(st => st.StageType == StageTypes.ConsultantApproval);

            foreach (var step in consultantApprovalStage.Steps)
            {
                step.StepSettings.GetSetting(SettingType.IsActive).Should().BeTrue();
            }
        }

        [Fact]
        public void SetSetting_OnStep_Succeeds()
        {
            //Arrange
            var workflow = RenderWorkflow.Create(Guid.Empty);
            workflow.BuildDefaultWorkflow();
            var peerStage = workflow.GetCustomStages().First(x => x.StageType == StageTypes.PeerCheck);
            var peerCheckStep = peerStage.Steps.First(x => x.Role == Roles.Review);
            
            //Act
            workflow.SetStepSetting(peerCheckStep, SettingType.IsActive, false);
                
            //Assert
            peerCheckStep.IsActive().Should().BeFalse();
        }
        
        [Fact]
        public void SetSetting_OnStep_WhenSettingDoesNotExist_AddsSetting()
        {
            //Arrange
            var workflow = RenderWorkflow.Create(Guid.Empty);
            workflow.BuildDefaultWorkflow();
            var peerStage = workflow.GetCustomStages().First(x => x.StageType == StageTypes.PeerCheck);
            var peerCheckStep = peerStage.Steps.First(x => x.Role == Roles.Review);

            //Act
            workflow.SetStepSetting(peerCheckStep, SettingType.DoSectionListen, true);
                
            //Assert
            var setting = peerCheckStep.StepSettings.GetSetting(SettingType.DoSectionListen);
            setting.Should().BeTrue();
        }

        [Fact]
        public void GetSetting_OnStep_WhenSettingDoesNotExist_ReturnsFalse()
        {
            //Arrange
            var workflow = RenderWorkflow.Create(Guid.Empty);
            workflow.BuildDefaultWorkflow();
            var peerStage = workflow.GetCustomStages().First(x => x.StageType == StageTypes.PeerCheck);
            var peerCheckStep = peerStage.Steps.First(x => x.Role == Roles.Review);

            //Act
            var result = peerCheckStep.StepSettings.GetSetting(SettingType.DoSectionListen);
            
            //Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void SetSetting_OnStage_SetsSetting()
        {
            //Arrange
            var workflow = RenderWorkflow.Create(Guid.Empty);
            workflow.BuildDefaultWorkflow();
            var peerStage = workflow.GetCustomStages().First(x => x.StageType == StageTypes.PeerCheck);
            
            //Act
            workflow.SetStageSetting(peerStage, SettingType.AssignToTranslator, false);
            
            //Assert
            var result = peerStage.StageSettings.GetSetting(SettingType.AssignToTranslator);
            result.Should().BeFalse();
        }

        [Fact]
        public void GetSettings_OnStage_WhenSettingDoesNotExist_ReturnsFalse()
        {
            //Arrange
            var workflow = RenderWorkflow.Create(Guid.Empty);
            workflow.BuildDefaultWorkflow();
            var peerStage = workflow.GetCustomStages().First(x => x.StageType == StageTypes.PeerCheck);
            
            //Act
            var result = peerStage.StageSettings.GetSetting(SettingType.DoSectionListen);
            
            //Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void DefaultWorkflow_HasDefaultStageSettings()
        {
            //Arrange
            var workflow = RenderWorkflow.Create(Guid.Empty);
            workflow.BuildDefaultWorkflow();
            var communityStage= workflow.GetCustomStages().First(x => x.StageType == StageTypes.CommunityTest);
            var peerStage = workflow.GetCustomStages().First(x => x.StageType == StageTypes.PeerCheck);
            //Assert
            communityStage.StageSettings.GetSetting(SettingType.AssignToTranslator).Should().BeTrue();
            peerStage.StageSettings.GetSetting(SettingType.NoSelfCheck).Should().BeTrue();
        }

        [Fact]
        public void AddCommunityTestStage_WhenTranslatorsAreAlreadyAssigned_AssignsTranslatorIdToReview()
        {
            //Arrange
            var workflow = RenderWorkflow.Create(Guid.Empty);
            workflow.BuildDefaultWorkflow();
            var userId = Guid.NewGuid();
            var team = workflow.GetTeams().First();
            workflow.AddTranslationAssignmentForTeam(team, userId);
            
            //Act
            var commTestStage = CommunityTestStage.Create();
            workflow.AddStage(commTestStage);
            
            //Assert
            var result = team.WorkflowAssignments.FirstOrDefault(x => x.StageId == commTestStage.Id);
            result.UserId.Should().Be(userId);
            result.Role.Should().Be(Roles.Review);
        }
        
        [Fact]
        public void InsertCommunityTestStage_WhenTranslatorsAreAlreadyAssigned_AssignsTranslatorIdToReview()
        {
            //Arrange
            var workflow = RenderWorkflow.Create(Guid.Empty);
            workflow.BuildDefaultWorkflow();
            var userId = Guid.NewGuid();
            var team = workflow.GetTeams().First();
            workflow.AddTranslationAssignmentForTeam(team, userId);
            
            //Act
            var commTestStage = CommunityTestStage.Create();
            workflow.InsertStage(commTestStage, workflow.DraftingStage.Id);
            
            //Assert
            var result = team.WorkflowAssignments.FirstOrDefault(x => x.StageId == commTestStage.Id);
            result.UserId.Should().Be(userId);
            result.Role.Should().Be(Roles.Review);
        }
    }
}