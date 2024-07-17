using FluentAssertions;
using Moq;
using Render.Models.Users;
using Render.Models.Workflow;
using Render.Models.Workflow.Stage;
using Render.Pages.Configurator.WorkflowAssignment;
using Render.Repositories.WorkflowRepositories;
using Render.UnitTests.App.Kernel;

namespace Render.UnitTests.App.Pages.Configurator.WorkflowAssignment
{
    public class TeamAssignmentCardViewModelTests : ViewModelTestBase
    {
        private readonly Stage _stage;
        private readonly List<Team> _teamList;
        private readonly RenderWorkflow _workflow;
        private readonly IUser _user;

        public TeamAssignmentCardViewModelTests()
        {
            _stage = new Stage();
            _workflow = RenderWorkflow.Create(default);
            _teamList = _workflow.GetTeams().ToList();
            _user = new User("user", "user");

            MockContextProvider.Setup(x => x.GetWorkflowRepository())
                .Returns(new Mock<IWorkflowRepository>().Object);
        }

        [Fact]
        public void CreateViewModel_Succeeds()
        {
            //Arrange

            //Act
            var viewModel = new TabletTeamAssignmentCardViewModel(_stage.Id, default, _teamList,
                DeleteTeamTest, UpdateTranslationTeamTest, _workflow,
                MockContextProvider.Object, _user);

            //Assert
            viewModel.Should().NotBeNull();
            viewModel.UserCardViewModel.Should().NotBeNull();
        }

        [Fact]
        public void OnDrop_Locked()
        {
            //Arrange
            var newUser = new User("user", "user");

            var viewModel = new TabletTeamAssignmentCardViewModel(_stage.Id, default, _teamList,
                DeleteTeamTest, UpdateTranslationTeamTest, _workflow,
                MockContextProvider.Object, _user, default, true);

            //Act
            viewModel.OnDrop(newUser);

            //Assert
            viewModel.UserCardViewModel.User.Should().NotBe(newUser);
        }

        [Fact]
        public void OnDrop_NoLock_Success()
        {
            //Arrange
            _workflow.AddStage(_stage);
            var team = _teamList.First();
            var team2 = _teamList.Last();
            team.AddAssignment(new Models.Workflow.WorkflowAssignment(_user.Id, _stage.Id, _stage.StageType,
                Roles.Drafting));
            team2.AddAssignment(new Models.Workflow.WorkflowAssignment(_user.Id, _stage.Id, _stage.StageType,
                Roles.Approval));
            var newUser = new User("user", "user");
            var viewModel = new TabletTeamAssignmentCardViewModel(_stage.Id, default, _teamList,
                DeleteTeamTest, UpdateTranslationTeamTest, _workflow,
                MockContextProvider.Object, _user);

            //Act
            viewModel.OnDrop(newUser);

            //Assert
            viewModel.UserCardViewModel.User.Should().Be(newUser);
        }

        [Fact]
        public void OnDrop_NoLock_Fail()
        {
            //Arrange
            var newUser = new User("user", "user");
            var viewModel = new TabletTeamAssignmentCardViewModel(_stage.Id, default, new List<Team>(),
                DeleteTeamTest, UpdateTranslationTeamTest, _workflow,
                MockContextProvider.Object, _user);

            //Act
            viewModel.OnDrop(newUser);

            //Assert
            viewModel.UserCardViewModel.Should().NotBe(newUser);
        }

        [Fact]
        public void ShowHideDeleteButton_ShowDeleteButton_False()
        {
            //Assert
            var viewModel = new TabletTeamAssignmentCardViewModel(_stage.Id, Roles.Approval, _teamList,
                DeleteTeamTest, UpdateTranslationTeamTest, _workflow,
                MockContextProvider.Object, _user);

            //Act
            viewModel.ShowHideDeleteButton(true);

            //Assert
            viewModel.ShowDeleteButton.Should().BeFalse();
        }

        [Fact]
        public void ShowHideDeleteButton_ShowDeleteButton_True()
        {
            //Assert
            var viewModel = new TabletTeamAssignmentCardViewModel(_stage.Id, Roles.Drafting, _teamList,
                DeleteTeamTest, UpdateTranslationTeamTest, _workflow,
                MockContextProvider.Object, _user);

            //Act
            viewModel.ShowHideDeleteButton(true);

            //Assert
            viewModel.ShowDeleteButton.Should().BeTrue();
        }

        [Fact]
        public void RemoveAssignment_Drafting_Succeeds()
        {
            //Arrange
            _teamList.First().UpdateTranslator(_user.Id);
            var viewModel = new TabletTeamAssignmentCardViewModel(_stage.Id, Roles.Drafting, new List<Team>
                {
                    _teamList.First
                        ()
                },
                DeleteTeamTest, UpdateTranslationTeamTest, _workflow,
                MockContextProvider.Object, _user);

            //Act
            viewModel.RemoveAssignmentCommand.Execute().Subscribe();

            //Assert
            viewModel.UserCardViewModel.Should().BeNull();
            viewModel.TeamList.First().TranslatorId.Should().Be(Guid.Empty);
        }

        [Fact]
        public void RemoveAssignment_NotDrafting_Succeeds()
        {
            //Arrange
            _teamList.First().AddAssignment(new Models.Workflow.WorkflowAssignment(_user.Id, _stage.Id,
                _stage.StageType, Roles.Review));
            var viewModel = new TabletTeamAssignmentCardViewModel(_stage.Id, Roles.Review, new List<Team>
                {
                    _teamList.First()
                },
                DeleteTeamTest, UpdateTranslationTeamTest, _workflow,
                MockContextProvider.Object, _user);

            //Act
            viewModel.RemoveAssignmentCommand.Execute().Subscribe();

            //Assert
            viewModel.UserCardViewModel.Should().BeNull();
            viewModel.TeamList.First().WorkflowAssignments.Count.Should().Be(0);
        }

        [Fact]
        public void RemoveAssignment_MultipleTeams_Consultant_Succeeds()
        {
            //Arrange
            foreach (var team in _teamList)
            {
                team.AddAssignment(new Models.Workflow.WorkflowAssignment(_user.Id, _stage.Id, _stage.StageType,
                    Roles.Consultant));
            }

            var viewModel = new TabletTeamAssignmentCardViewModel(_stage.Id, Roles.Review, _teamList,
                DeleteTeamTest, UpdateTranslationTeamTest, _workflow,
                MockContextProvider.Object, _user);

            //Act
            viewModel.RemoveAssignmentCommand.Execute().Subscribe();

            //Assert
            viewModel.TeamList
                .All(x => x.GetWorkflowAssignmentForRole(Roles.Consultant).UserId == _user.Id).Should().BeTrue();
        }

        private void DeleteTeamTest(Team team)
        {
            //shoulder shrug
        }

        private void UpdateTranslationTeamTest(Guid userId, Team team)
        {
            //scratches head
        }
    }
}