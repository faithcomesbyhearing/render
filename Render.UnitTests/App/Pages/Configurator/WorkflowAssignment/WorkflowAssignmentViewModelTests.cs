using FluentAssertions;
using Moq;
using ReactiveUI;
using Render.Models.Sections;
using Render.Models.Users;
using Render.Models.Workflow;
using Render.Pages.AppStart.Home;
using Render.Pages.Configurator.WorkflowAssignment;
using Render.Pages.Configurator.WorkflowAssignment.Stages;
using Render.Repositories.Kernel;
using Render.Repositories.SectionRepository;
using Render.Repositories.UserRepositories;
using Render.Repositories.WorkflowRepositories;
using Render.Services.SessionStateServices;
using Render.Services.UserServices;
using Render.TempFromVessel.Project;
using Render.UnitTests.App.Kernel;

namespace Render.UnitTests.App.Pages.Configurator.WorkflowAssignment
{
    public class WorkflowAssignmentViewModelTests : ViewModelTestBase
    {
        private readonly Project _project;
        private readonly RenderUser _user1;
        private readonly Mock<IWorkflowRepository> _workflowPersistence;
        private readonly Section _section1 = Section.UnitTestEmptySection();
        private readonly Section _section2 = Section.UnitTestEmptySection();

        private readonly Mock<ISectionRepository> _mockSectionRepository = new Mock<ISectionRepository>();

        public WorkflowAssignmentViewModelTests()
        {
            _project = new Project("Test Project", "Ref", "iso");
            var workflow = RenderWorkflow.Create(_project.Id);
            workflow.BuildDefaultWorkflow();
            _user1 = new RenderUser("Render User 1", _project.Id);
            var user2 = new RenderUser("Render User 2", _project.Id);
            var projectPersistence = new Mock<IDataPersistence<Project>>();
            projectPersistence.Setup(x => x.GetAsync(_project.Id)).ReturnsAsync(_project);
            _workflowPersistence = new Mock<IWorkflowRepository>();
            _workflowPersistence.Setup(x =>
                    x.GetWorkflowForProjectIdAsync(_project.Id)).ReturnsAsync(workflow);
            var userRepo = new Mock<IUserRepository>();
            userRepo.Setup(x => x.GetUsersForProjectAsync(_project)).ReturnsAsync(new List<IUser> { _user1, user2 });
            var mockUserMembershipService = new Mock<IUserMembershipService>();
            MockContextProvider.Setup(x => x.GetPersistence<Project>()).Returns(projectPersistence.Object);
            MockContextProvider.Setup(x => x.GetWorkflowRepository()).Returns(_workflowPersistence.Object);
            MockContextProvider.Setup(x => x.GetUserRepository()).Returns(userRepo.Object);
            MockContextProvider.Setup(x => x.GetUserMembershipService())
                .Returns(mockUserMembershipService.Object);
            MockContextProvider.Setup(x => x.GetSectionRepository())
                .Returns(_mockSectionRepository.Object);
        }

        [Fact]
        public async Task ViewModelCreation_CreatesProperly()
        {
            //Arrange

            //Act
            var vm = await WorkflowAssignmentViewModel.CreateAsync(MockContextProvider.Object, _project.Id);

            //Assert
            vm.Users.Items.Count.Should().Be(2);
            vm.StageColumns.Count.Should().Be(5);
        }

        [Fact]
        public async Task NavigateHome_WhenNotAllWorkIsAssigned_DoesNotNavigate()
        {
            //Arrange

            //Act
            var vm = await WorkflowAssignmentViewModel.CreateAsync(MockContextProvider.Object, _project.Id);
            SetupViewModelForNavigationTest(vm);
            //Assert

            //proceed button always active
            vm.ProceedButtonViewModel.ProceedActive.Should().BeTrue();
            IRoutableViewModel result = null;
            vm.ProceedButtonViewModel.NavigateToPageCommand.Execute().Subscribe(item => result = item);
            result.Should().BeNull();
        }

        [Fact]
        public async Task NavigateHome_WhenAllWorkAssigned_Navigates()
        {
            //Arrange
            MockContextProvider.Setup(x => x.GetSessionStateService()).Returns(new Mock<ISessionStateService>().Object);
            _mockSectionRepository.Setup(x => x.GetSectionsForProjectAsync(It.IsAny<Guid>()))
                .ReturnsAsync(new List<Section> { _section1, _section2 });

            //Act
            var vm = await WorkflowAssignmentViewModel.CreateAsync(MockContextProvider.Object, _project.Id);
            foreach (var stageColumn in vm.StageColumns)
            {
                stageColumn.AllWorkAssigned = true;
            }
            SetupViewModelForNavigationTest(vm);
            SetupMocksForHomeViewModelNavigation();

            //Assert
            vm.ProceedButtonViewModel.ProceedActive.Should().BeTrue();
            await VerifyNavigationResultAsync<HomeViewModel>(vm.ProceedButtonViewModel.NavigateToPageCommand);
        }

        [Fact]
        public async Task UpdateTranslatorAssignment_UpdatesCommunityCheckCards()
        {
            //Arrange
            MockContextProvider.Setup(x => x.GetCurrentDeviceIdiom()).Returns(DeviceIdiom.Tablet);
            //Act
            var vm = await WorkflowAssignmentViewModel.CreateAsync(MockContextProvider.Object, _project.Id);
            var tabletTeamAssigmentVm = vm.StageColumns.First().TeamList.First() as TabletTeamAssignmentCardViewModel;
            tabletTeamAssigmentVm?.OnDrop(_user1);

            //Assert
            vm.StageColumns.First(x => x.StageType == StageTypes.CommunityTest)
                .TeamList.First().UserCardViewModel.User.Should().Be(_user1);
        }

        [Fact]
        public async Task RemoveTranslatorAssignment_UpdatesCommunityCheckCards()
        {
            //Arrange
            MockContextProvider.Setup(x => x.GetCurrentDeviceIdiom()).Returns(DeviceIdiom.Tablet);

            //Act
            var vm = await WorkflowAssignmentViewModel.CreateAsync(MockContextProvider.Object, _project.Id);
            var tabletTeamAssignmentVm = vm.StageColumns.First().TeamList.First() as TabletTeamAssignmentCardViewModel;
            tabletTeamAssignmentVm?.OnDrop(_user1);
            vm.StageColumns.First().TeamList.First().RemoveAssignmentCommand.Execute().Subscribe();

            //Assert
            vm.StageColumns.First(x => x.StageType == StageTypes.CommunityTest)
                .TeamList.First().UserCardViewModel.Should().Be(null);
        }

        [Fact]
        public async Task Workflow_WithoutCommunityCheck_DoesNotBreakWhenTranslatorUpdated()
        {
            //Arrange
            MockContextProvider.Setup(x => x.GetCurrentDeviceIdiom()).Returns(DeviceIdiom.Tablet);

            var workflow = RenderWorkflow.Create(_project.Id);
            _workflowPersistence.Setup(x => x.GetWorkflowForProjectIdAsync(_project.Id)).ReturnsAsync(workflow);

            //Act
            var vm = await WorkflowAssignmentViewModel.CreateAsync(MockContextProvider.Object, _project.Id);
            var tabletTeamAssignmentVm = vm.StageColumns.First().TeamList.First() as TabletTeamAssignmentCardViewModel;
            tabletTeamAssignmentVm?.OnDrop(_user1);

            //Assert
            vm.StageColumns.Count.Should().Be(2);
        }

        [Fact]
        public async Task AddTeam_AddsTeamToAllStageColumns()
        {
            //Arrange

            //Act
            var vm = await WorkflowAssignmentViewModel.CreateAsync(MockContextProvider.Object, _project.Id);
            var draftColumn = (WorkflowDraftStageColumnViewModel)vm.StageColumns.First();
            draftColumn.AddTeamCommand.Execute().Subscribe();

            //Assert
            vm.StageColumns.Skip(1).First().TeamList.Count().Should().Be(3);
            ((WorkflowConsultantCheckStageColumnViewModel)vm.StageColumns.First(x => x.StageType == StageTypes
            .ConsultantCheck)).ConsultantAssignmentCard.TeamList.Count.Should().Be(3);
        }

        [Fact]
        public async Task DeleteTeam_RemovesTeamFromAllColumns()
        {
            //Arrange

            //Act
            var vm = await WorkflowAssignmentViewModel.CreateAsync(MockContextProvider.Object, _project.Id);
            var draftColumn = (WorkflowDraftStageColumnViewModel)vm.StageColumns.First();
            draftColumn.TeamList.First().RemoveTeamCommand.Execute().Subscribe();

            //Assert
            draftColumn.TeamList.Count.Should().Be(1);
            vm.StageColumns.Skip(1).First().TeamList.Count.Should().Be(1);
            vm.StageColumns.Skip(2).First().TeamList.Count.Should().Be(1);
            ((WorkflowConsultantCheckStageColumnViewModel)vm.StageColumns.Skip(3).First()).ConsultantAssignmentCard
            .TeamList.Count.Should().Be(1);
            ((WorkflowConsultantApprovalStageColumnViewModel)vm.StageColumns.Skip(4).First()).TeamList
            .Count.Should().Be(1);
        }
    }
}