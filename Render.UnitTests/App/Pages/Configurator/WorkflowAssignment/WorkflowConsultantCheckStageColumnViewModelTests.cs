using FluentAssertions;
using Moq;
using Render.Models.Users;
using Render.Models.Workflow;
using Render.Models.Workflow.Stage;
using Render.Pages.Configurator.WorkflowAssignment.Cards;
using Render.Pages.Configurator.WorkflowAssignment.Stages;
using Render.UnitTests.App.Kernel;

namespace Render.UnitTests.App.Pages.Configurator.WorkflowAssignment
{
    public class WorkflowConsultantCheckStageColumnViewModelTests : ViewModelTestBase
    {
        private readonly RenderWorkflow _workflow;
        private readonly Stage _stage;
        private WorkflowConsultantCheckStageColumnViewModel _viewModel;

        public WorkflowConsultantCheckStageColumnViewModelTests()
        {
            _workflow = RenderWorkflow.Create(default);
            _stage = ConsultantCheckStage.Create();
        }

        [Fact]
        public void CreateViewModel_Succeeds()
        {
            //Arrange
            var teamList = _workflow.GetTeams().ToList();
            var users = new List<IUser>() { new User("Test User", "testuser") };

            //Act
            _viewModel = new WorkflowConsultantCheckStageColumnViewModel(
                _stage, teamList, default, default, default,
                users, _workflow, MockContextProvider.Object, It.IsAny<string>());

            //Assert
            _viewModel.Should().NotBeNull();
            _viewModel.TeamList.Count().Should().Be(0);
            _viewModel.StageType.Should().Be(StageTypes.ConsultantCheck);
            _viewModel.BackTranslateAssignmentCard.Should().NotBeNull();
            _viewModel.NoteTranslateAssignmentCard.Should().NotBeNull();
            _viewModel.ConsultantAssignmentCard.Should().NotBeNull();
            _viewModel.BackTranslateAssignmentCard.UserCardViewModel.Should().BeNull();
            _viewModel.NoteTranslateAssignmentCard.UserCardViewModel.Should().BeNull();
            _viewModel.ConsultantAssignmentCard.UserCardViewModel.Should().BeNull();
        }

        [Fact]
        public void CreateViewModel_TeamWorkAssignedBackTranslate()
        {
            //Arrange
            var teamList = _workflow.GetTeams().ToList();
            var users = new List<IUser>() { new User("Test User", "testuser") };
            var team = teamList.First();
            var workflowAssignment =
                new Models.Workflow.WorkflowAssignment(users.First().Id, _stage.Id, _stage.StageType,
                    Roles.BackTranslate);

            //Act
            team.AddAssignment(workflowAssignment);
            _viewModel = new WorkflowConsultantCheckStageColumnViewModel(
                _stage, teamList, default, default, default,
                users, _workflow, MockContextProvider.Object, It.IsAny<string>());

            //Assert
            _viewModel.BackTranslateAssignmentCard.UserCardViewModel.Should().NotBeNull();
        }

        [Fact]
        public void CreateViewModel_TeamWorkAssignedNoteTranslate()
        {
            //Arrange
            var teamList = _workflow.GetTeams().ToList();
            var users = new List<IUser>() { new User("Test User", "testuser") };
            var team = teamList.First();
            var workflowAssignment =
                new Models.Workflow.WorkflowAssignment(users.First().Id, _stage.Id, _stage.StageType,
                    Roles.NoteTranslate);

            //Act
            team.AddAssignment(workflowAssignment);
            _viewModel = new WorkflowConsultantCheckStageColumnViewModel(
                _stage, teamList, default, default, default,
                users, _workflow, MockContextProvider.Object, It.IsAny<string>());

            //Assert
            _viewModel.NoteTranslateAssignmentCard.UserCardViewModel.Should().NotBeNull();
        }

        [Fact]
        public void CreateViewModel_TeamWorkAssignedTranscribe()
        {
            //Arrange
            var teamList = _workflow.GetTeams().ToList();
            var users = new List<IUser>() { new User("Test User", "testuser") };
            var team = teamList.First();
            var workflowAssignment =
                new Models.Workflow.WorkflowAssignment(users.First().Id, _stage.Id, _stage.StageType,
                    Roles.Transcribe);

            //Act
            team.AddAssignment(workflowAssignment);
            _viewModel = new WorkflowConsultantCheckStageColumnViewModel(
                _stage, teamList, default, default, default,
                users, _workflow, MockContextProvider.Object, It.IsAny<string>());

            //Assert
            _viewModel.TranscribeAssignmentCard.UserCardViewModel.Should().NotBeNull();
        }

        [Fact]
        public void CreateViewModel_TeamWorkAssignedTranscribe2()
        {
            //Arrange
            var teamList = _workflow.GetTeams().ToList();
            var users = new List<IUser>() { new User("Test User", "testuser") };
            var team = teamList.First();
            var workflowAssignment =
                new Models.Workflow.WorkflowAssignment(users.First().Id, _stage.Id, _stage.StageType,
                    Roles.Transcribe2);

            //Act
            team.AddAssignment(workflowAssignment);
            _viewModel = new WorkflowConsultantCheckStageColumnViewModel(
                _stage, teamList, default, default, default,
                users, _workflow, MockContextProvider.Object, It.IsAny<string>());

            //Assert
            _viewModel.Transcribe2AssignmentCard.UserCardViewModel.Should().NotBeNull();
        }

        [Fact]
        public void CreateViewModel_TeamWorkAssignedConsultant()
        {
            //Arrange
            var teamList = _workflow.GetTeams().ToList();
            var users = new List<IUser>() { new User("Test User", "testuser") };
            var team = teamList.First();
            var workflowAssignment =
                new Models.Workflow.WorkflowAssignment(users.First().Id, _stage.Id, _stage.StageType,
                    Roles.Consultant);

            //Act
            team.AddAssignment(workflowAssignment);
            _viewModel = new WorkflowConsultantCheckStageColumnViewModel(
                _stage, teamList, default, default, default,
                users, _workflow, MockContextProvider.Object, It.IsAny<string>());

            //Assert
            _viewModel.ConsultantAssignmentCard.UserCardViewModel.Should().NotBeNull();
        }

        [Fact]
        public void AllStepsOn_ChecksForAllWorkAssignedForAllRoles()
        {
            //Arrange
            var teamList = _workflow.GetTeams().ToList();
            var user = new User("Test User", "testuser");
            var users = new List<IUser>() { user };
            var btMultiStep = _stage.Steps.First(x => x.Order == Step.Ordering.Parallel).GetSubSteps()
                .First(x => x.Role == Roles.BackTranslate);
            var secondStep = btMultiStep.GetSubSteps().First(x => x.Order == Step.Ordering.Parallel).GetSubSteps()
                .First(x => x.GetSubSteps().Count > 0).GetSubSteps()
                .First(x => x.Role == Roles.BackTranslate2);
            secondStep.StepSettings.SetSetting(SettingType.DoRetellBackTranslate, true);
            secondStep.StepSettings.SetSetting(SettingType.DoSegmentBackTranslate, true);

            //Act
            _viewModel = new WorkflowConsultantCheckStageColumnViewModel(
                _stage, teamList, default, default, default,
                users, _workflow, MockContextProvider.Object, It.IsAny<string>());
            _viewModel.ConsultantAssignmentCard.UserCardViewModel = new UserCardViewModel(user, MockContextProvider.Object);
            _viewModel.BackTranslateAssignmentCard.UserCardViewModel = new UserCardViewModel(user, MockContextProvider.Object);
            _viewModel.NoteTranslateAssignmentCard.UserCardViewModel = new UserCardViewModel(user, MockContextProvider.Object);
            _viewModel.BackTranslate2AssignmentCard.UserCardViewModel = new UserCardViewModel(user, MockContextProvider.Object);
            _viewModel.TranscribeAssignmentCard.UserCardViewModel = new UserCardViewModel(user, MockContextProvider.Object);
            _viewModel.Transcribe2AssignmentCard.UserCardViewModel = new UserCardViewModel(user, MockContextProvider.Object);

            //Assert
            _viewModel.AllWorkAssigned.Should().BeTrue();
        }

        [Fact]
        public void BackTranslate2Off_SetsAllWorkAssignedWithoutIt()
        {
            //Arrange
            var teamList = _workflow.GetTeams().ToList();
            var user = new User("Test User", "testuser");
            var users = new List<IUser>() { user };
            var segmentMultiStep = _stage.Steps.First(x => x.Order == Step.Ordering.Parallel).GetSubSteps()
                .First(x => x.Role == Roles.BackTranslate);
            var secondStep = segmentMultiStep.GetSubSteps().First(x => x.Order == Step.Ordering.Parallel)
                .GetSubSteps().First(x => x.GetSubSteps().Count > 0).GetSubSteps()
                .First(x => x.Role == Roles.BackTranslate2);
            secondStep.StepSettings.SetSetting(SettingType.DoSegmentBackTranslate, false);
            secondStep.StepSettings.SetSetting(SettingType.DoRetellBackTranslate, false);

            //Act
            _viewModel = new WorkflowConsultantCheckStageColumnViewModel(
                _stage, teamList, default, default, default,
                users, _workflow, MockContextProvider.Object, It.IsAny<string>());
            _viewModel.ConsultantAssignmentCard.UserCardViewModel = new UserCardViewModel(user, MockContextProvider.Object);
            _viewModel.BackTranslateAssignmentCard.UserCardViewModel = new UserCardViewModel(user, MockContextProvider.Object);
            _viewModel.NoteTranslateAssignmentCard.UserCardViewModel = new UserCardViewModel(user, MockContextProvider.Object);
            _viewModel.TranscribeAssignmentCard.UserCardViewModel = new UserCardViewModel(user, MockContextProvider.Object);
            _viewModel.Transcribe2AssignmentCard.UserCardViewModel = new UserCardViewModel(user, MockContextProvider.Object);
            //Assert
            _viewModel.AllWorkAssigned.Should().BeTrue();
        }

        [Fact]
        public void BackTranslateOff_SetsAllWorkAssignedWithoutIt()
        {
            //Arrange
            var teamList = _workflow.GetTeams().ToList();
            var user = new User("Test User", "testuser");
            var users = new List<IUser>() { user };
            var btMultiStep = _stage.Steps.First(x => x.Order == Step.Ordering.Parallel).GetSubSteps()
                .First(x => x.Role == Roles.BackTranslate);
            var firstStep = btMultiStep.GetSubSteps()
                .First(x => x.RenderStepType == RenderStepTypes.BackTranslate);
            var secondStep = btMultiStep.GetSubSteps().First(x => x.Order == Step.Ordering.Parallel).GetSubSteps()
                .First(x => x.GetSubSteps().Count > 0).GetSubSteps()
                .First(x => x.Role == Roles.BackTranslate2);
            firstStep.StepSettings.SetSetting(SettingType.DoSegmentBackTranslate, false);
            firstStep.StepSettings.SetSetting(SettingType.DoRetellBackTranslate, false);
            secondStep.StepSettings.SetSetting(SettingType.DoSegmentBackTranslate, false);
            secondStep.StepSettings.SetSetting(SettingType.DoRetellBackTranslate, false);

            //Act
            _viewModel = new WorkflowConsultantCheckStageColumnViewModel(
                _stage, teamList, default, default, default,
                users, _workflow, MockContextProvider.Object, It.IsAny<string>());
            _viewModel.ConsultantAssignmentCard.UserCardViewModel = new UserCardViewModel(user, MockContextProvider.Object);
            _viewModel.NoteTranslateAssignmentCard.UserCardViewModel = new UserCardViewModel(user, MockContextProvider.Object);
            _viewModel.TranscribeAssignmentCard.UserCardViewModel = new UserCardViewModel(user, MockContextProvider.Object);
            _viewModel.Transcribe2AssignmentCard.UserCardViewModel = new UserCardViewModel(user, MockContextProvider.Object);
            //Assert
            _viewModel.AllWorkAssigned.Should().BeTrue();
        }
    }
}