using FluentAssertions;
using Moq;
using Render.Models.Users;
using Render.Models.Workflow;
using Render.Models.Workflow.Stage;
using Render.Pages.Configurator.WorkflowAssignment.Stages;
using Render.UnitTests.App.Kernel;

namespace Render.UnitTests.App.Pages.Configurator.WorkflowAssignment
{
    public class WorkflowDraftStageColumnViewModelTests : ViewModelTestBase
    {
        private readonly RenderWorkflow _workflow;
        private readonly Stage _stage;
        private WorkflowDraftStageColumnViewModel _viewModel;

        public WorkflowDraftStageColumnViewModelTests()
        {
            _workflow = RenderWorkflow.Create(default);
            _stage = DraftingStage.Create();
        }

        [Fact]
        public void CreateViewModel_Succeeds()
        {
            //Arrange
            var teamList = _workflow.GetTeams().ToList();
            var users = new List<IUser>() { new User("Test User", "testuser") };

            //Act
            _viewModel = new WorkflowDraftStageColumnViewModel(
                _stage, teamList, default, default,
                default, default,
                users, _workflow, MockContextProvider.Object, It.IsAny<string>());

            //Assert
            _viewModel.Should().NotBeNull();
            _viewModel.TeamList.Count().Should().Be(2);
            _viewModel.StageType.Should().Be(StageTypes.Drafting);
            _viewModel.TeamList.Any(x => x.UserCardViewModel != null).Should().BeFalse();
        }

        [Fact]
        public void CreateViewModel_TeamAssignedTranslator()
        {
            //Arrange
            var teamList = _workflow.GetTeams().ToList();
            var users = new List<IUser>() { new User("Test User", "testuser") };
            var team = teamList.First();

            //Act
            team.UpdateTranslator(users.First().Id);
            _viewModel = new WorkflowDraftStageColumnViewModel(
                _stage, teamList, default, default,
                default, default,
                users, _workflow, MockContextProvider.Object, It.IsAny<string>());

            //Assert
            team.TranslatorId.Should().NotBeEmpty();
            _viewModel.TeamList.Any(x => x.UserCardViewModel != null).Should().BeTrue();

        }

        [Fact]
        public void CreateViewModel_DeleteTeamButton_False()
        {
            //Arrange
            var teamList = _workflow.GetTeams().ToList();
            teamList.Remove(teamList[0]);
            var users = new List<IUser>() { new User("Test User", "testuser") };

            //Act
            _viewModel = new WorkflowDraftStageColumnViewModel(
                _stage, teamList, default, default,
                default, default,
                users, _workflow, MockContextProvider.Object, It.IsAny<string>());

            //Assert
            _viewModel.TeamList.All(x => x.ShowDeleteButton).Should().BeFalse();
        }

        [Fact]
        public void CreateViewModel_DeleteTeamButton_True()
        {
            //Arrange
            var teamList = _workflow.GetTeams().ToList();
            var users = new List<IUser>() { new User("Test User", "testuser") };

            //Act
            _viewModel = new WorkflowDraftStageColumnViewModel(
                _stage, teamList, default, default,
                default, default,
                users, _workflow, MockContextProvider.Object, It.IsAny<string>());

            //Assert
            _viewModel.TeamList.All(x => x.ShowDeleteButton).Should().BeTrue();
        }

        [Fact]
        public void CreateViewModel_AddTeamToList()
        {
            //Arrange
            var teamList = _workflow.GetTeams().ToList();
            var users = new List<IUser>() { new User("Test User", "testuser") };
            var team = teamList.First();

            _viewModel = new WorkflowDraftStageColumnViewModel(
                _stage, teamList, default, default,
                default, default,
                users, _workflow, MockContextProvider.Object, It.IsAny<string>());
            var teamBeforeCount = _viewModel.TeamList.Count();

            //Act
            _viewModel.AddTeamToList(users, team, users.First(), default, default);

            //Assert
            _viewModel.TeamList.Count.Should().BeGreaterThan(teamBeforeCount);
            _viewModel.TeamList.Last().Role.Should().Be(Roles.Drafting);
        }

        [Fact]
        public void CreateViewModel_CheckMultipleTeams_True()
        {
            //Arrange
            var teamList = _workflow.GetTeams().ToList();
            var team = teamList.First();
            var users = new List<IUser>() { new User("Test User", "testuser") };
            var user = users.First();
            team.UpdateTranslator(user.Id);
            _viewModel = new WorkflowDraftStageColumnViewModel(
                _stage, teamList, default, default,
                default, default,
                users, _workflow, MockContextProvider.Object, It.IsAny<string>());

            //Act
            var result = _viewModel.CheckMultipleTeams(user.Id, $"Team {teamList[1].TeamNumber}");

            //Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void CreateViewModel_CheckMultipleTeams_False()
        {
            //Arrange
            var teamList = _workflow.GetTeams().ToList();
            var team = teamList.First();
            var users = new List<IUser>() { new User("Test User", "testuser"), new User("Test User 2", "testuser2") };
            var user = users.First();
            team.UpdateTranslator(user.Id);
            _viewModel = new WorkflowDraftStageColumnViewModel(
                _stage, teamList, default, default,
                default, default,
                users, _workflow, MockContextProvider.Object, It.IsAny<string>());

            //Act
            var result = _viewModel.CheckMultipleTeams(users[1].Id, $"Team {teamList[1].TeamNumber}");

            //Assert
            result.Should().BeFalse();
        }
    }
}