using FluentAssertions;
using Moq;
using Render.Models.Users;
using Render.Models.Workflow;
using Render.Models.Workflow.Stage;
using Render.Pages.Configurator.WorkflowAssignment.Stages;
using Render.UnitTests.App.Kernel;

namespace Render.UnitTests.App.Pages.Configurator.WorkflowAssignment
{
    public class WorkflowCommunityCheckStageColumnViewModelTests : ViewModelTestBase
    {
        private readonly RenderWorkflow _workflow;
        private readonly Stage _stage;
        private WorkflowCommunityCheckStageColumnViewModel _viewModel;

        public WorkflowCommunityCheckStageColumnViewModelTests()
        {
            _workflow = RenderWorkflow.Create(default);
            _stage = CommunityTestStage.Create();
        }

        [Fact]
        public void CreateViewModel_Succeeds()
        {
            var teamList = _workflow.GetTeams().ToList();
            var users = new List<IUser>() { new User("Test User", "testuser") };

            //Act
            _viewModel = new WorkflowCommunityCheckStageColumnViewModel(
                _stage, teamList, default, default, default,
                users, _workflow, MockContextProvider.Object, It.IsAny<string>());

            //Assert
            _viewModel.Should().NotBeNull();
            _viewModel.TeamList.Count().Should().Be(2);
            _viewModel.StageType.Should().Be(StageTypes.CommunityTest);
            _viewModel.TeamList.Any(x => x.UserCardViewModel != null).Should().BeFalse();
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
                    Roles.Review);

            //Act
            team.AddAssignment(workflowAssignment);
            _viewModel = new WorkflowCommunityCheckStageColumnViewModel(
                _stage, teamList, default, default, default,
                users, _workflow, MockContextProvider.Object, It.IsAny<string>());

            //Assert
            _viewModel.TeamList.Any(x => x.UserCardViewModel != null).Should().BeTrue();
        }
    }
}