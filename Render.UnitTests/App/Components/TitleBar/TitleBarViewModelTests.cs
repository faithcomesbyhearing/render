using Moq;
using Render.Components.TitleBar;
using Render.Components.TitleBar.MenuActions;
using Render.Kernel.WrappersAndExtensions;
using Render.Models.Project;
using Render.Models.LocalOnlyData;
using Render.Models.Sections;
using Render.Models.Users;
using Render.Models.Workflow;
using Render.Pages.AppStart.Home;
using Render.Pages.AppStart.ProjectSelect;
using Render.Repositories.Kernel;
using Render.Repositories.LocalDataRepositories;
using Render.Repositories.SectionRepository;
using Render.Repositories.UserRepositories;
using Render.Repositories.WorkflowRepositories;
using Render.Services.GrandCentralStation;
using Render.Services.SessionStateServices;
using Render.Services.SyncService;
using Render.Services.UserServices;
using Render.TempFromVessel.Project;
using Render.TempFromVessel.User;
using Render.UnitTests.App.Kernel;

namespace Render.UnitTests.App.Components.TitleBar
{
    public class TitleBarViewModelTests : ViewModelTestBase
    {
        private readonly Guid _projectId;
        private readonly TitleBarMenuViewModel _titleBarMenuViewModel;

        private readonly Mock<IWorkflowRepository> _mockWorkflowRepository = new();
        private readonly Mock<ISectionRepository> _mockSectionRepository = new();
        private readonly Mock<IUserRepository> _mockUserRepository = new();
        private readonly RenderWorkflow _renderWorkflow = RenderWorkflow.Create(Guid.Empty);
        private readonly Section _section1 = Section.UnitTestEmptySection();
        private readonly Section _section2 = Section.UnitTestEmptySection();
        private readonly RenderUser _user1 = new("User1", Guid.Empty);
        private readonly RenderUser _user2 = new("User2", Guid.Empty);
        private readonly Project _project;

        public TitleBarViewModelTests()
        {
            _project = new Project("Test Project", "TP", "TPP");
            var mockLocalProjectsRepository = new Mock<ILocalProjectsRepository>();
            var mockModalService = new Mock<IModalService>();

            _projectId = Guid.NewGuid();
            var localProjects = new LocalProjects();
            localProjects.AddDownloadedProject(_projectId);
            mockLocalProjectsRepository.Setup(x => x.GetLocalProjectsForMachine())
                .ReturnsAsync(localProjects);
            MockContextProvider.Setup(x => x.GetLocalProjectsRepository()).Returns(mockLocalProjectsRepository.Object);
            MockContextProvider.Setup(x => x.GetModalService()).Returns(mockModalService.Object);
            MockContextProvider.Setup(x => x.GetSessionStateService()).Returns(new Mock<ISessionStateService>().Object);
            _titleBarMenuViewModel = new TitleBarMenuViewModel(new List<IMenuActionViewModel>(), MockContextProvider
            .Object, "Home", "test");
            
            _renderWorkflow.AddTranslationAssignmentForTeam(_renderWorkflow.GetTeams().First(), _user1.Id);
            _renderWorkflow.AddTranslationAssignmentForTeam(_renderWorkflow.GetTeams().Last(), _user2.Id);
            _mockWorkflowRepository.Setup(x => x.GetWorkflowForProjectIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync(_renderWorkflow);
            _mockSectionRepository.Setup(x => x.GetSectionsForProjectAsync(It.IsAny<Guid>()))
                .ReturnsAsync(new List<Section>{_section1, _section2});
            _mockUserRepository.Setup(x => x.GetUserAsync(_user1.Id)).ReturnsAsync(_user1);
            _mockUserRepository.Setup(x => x.GetUserAsync(_user2.Id)).ReturnsAsync(_user2);
            
            MockContextProvider.Setup(x => x.GetWorkflowRepository())
                .Returns(_mockWorkflowRepository.Object);
            MockContextProvider.Setup(x => x.GetUserRepository())
                .Returns(_mockUserRepository.Object);
            MockContextProvider.Setup(x => x.GetSectionRepository())
                .Returns(_mockSectionRepository.Object);
            
            var projectRepositoryMock = new Mock<IDataPersistence<Project>>();
            var project = new Project("Project Name", string.Empty, string.Empty);
            projectRepositoryMock.Setup(x => x.GetAsync(_projectId)).ReturnsAsync(project);
            
            MockContextProvider.Setup(x => x.GetPersistence<Project>()).Returns(projectRepositoryMock.Object);
        }

        [Fact]
        public void CreateViewModel_WithJustMenuIcon_ShowsOnlyMenu()
        {
            //Arrange
            var elements = new List<TitleBarElements>();
            
            //Act
            var vm = new TitleBarViewModel(elements, _titleBarMenuViewModel, MockContextProvider
            .Object, "", null, 0);
            
            //Assert
            vm.ShowLogo.Should().Be(false);
            vm.ShowSettings.Should().Be(false);
        }

        [Fact]
        public void CreateViewModel_WithAllIcons_SetsValuesForShow()
        {
            //Arrange
            var elements = new List<TitleBarElements>
            {
                TitleBarElements.RenderLogo,
                TitleBarElements.SettingsButton
            };
            
            //Act
            var vm = new TitleBarViewModel(elements, _titleBarMenuViewModel, MockContextProvider
                .Object, "", null, 0);
            
            //Assert
            vm.ShowLogo.Should().Be(true);
            vm.ShowSettings.Should().Be(true);
        }

        [Fact]
        public async Task NavigateBackOrToProjectSelect_OnHome_WorksCorrectly()
        {
            //Arrange
            var elements = new List<TitleBarElements>
            {
                TitleBarElements.RenderLogo,
                TitleBarElements.SettingsButton,
                TitleBarElements.BackButton
            };
            
            //Act
            var vm = new TitleBarViewModel(elements, _titleBarMenuViewModel, MockContextProvider
                .Object, "", null, 0);
            SetupViewModelForNavigationTest(vm);

            var homeViewModel = await HomeViewModel.CreateAsync(_project.Id, MockContextProvider.Object);
            vm.HostScreen.Router.NavigationStack.Add(homeViewModel);
            SetupMocksForProjectSelect();
            
            //Assert
            await VerifyNavigationResultAsync<ProjectSelectViewModel>(vm.NavigateBackCommand);
        }
        
        [Fact]
        public void NavigatesHome_FalseConditionSet_NavigationNotExecuted()
        {
            //Arrange
            SetupNavigateHome(_projectId);
            var elements = new List<TitleBarElements>
            {
                TitleBarElements.RenderLogo,
                TitleBarElements.SettingsButton
            };
            
            var vm = new TitleBarViewModel(elements, _titleBarMenuViewModel, MockContextProvider
                .Object, "", null, 0);

            //Act //Assert
            vm.Should().NotBeNull();
            vm.SetNavigationCondition(FalseCondition);
            VerifyNavigationResultNull(vm.NavigateHomeCommand);
        }

        private void SetupNavigateHome(Guid projectId)
        {
            var grandCentralStation = new Mock<IGrandCentralStation>();
            grandCentralStation.Setup(x => x.CurrentProjectId).Returns(projectId);
            MockStageService.Setup(x => x.StepsAssignedToUser()).Returns(new List<Guid>());
            MockContextProvider.Setup(x => x.GetGrandCentralStation()).Returns(grandCentralStation.Object);
            MockContextProvider.Setup(x => x.GetLoggedInUser()).Returns(new Mock<IUser>().Object);
            var sessionStateService = new Mock<ISessionStateService>();
            sessionStateService.Setup(x => x.LoadUserProjectSessionAsync(It.IsAny<Guid>(), It.IsAny<Guid>()));
            MockContextProvider.Setup(x => x.GetSessionStateService()).Returns(sessionStateService.Object);
            MockContextProvider.Setup(x => x.GetModalService()).Returns(new Mock<IModalService>().Object);
            var mockMachineStateRepo = new Mock<IMachineLoginStateRepository>();
            MockContextProvider.Setup(mock => mock.GetMachineLoginStateRepository())
                .Returns(mockMachineStateRepo.Object);
            mockMachineStateRepo.Setup(mock => mock.GetMachineLoginState())
                .ReturnsAsync(new MachineLoginState());
        }

        private void SetupMocksForProjectSelect()
        {
            var user = new User("Test", "test");
            var claim = new VesselClaim(RenderRolesAndClaims.ProjectUserClaimType, _project.Id.ToString(), RoleName.Configure.GetRoleId());
            user.Claims.Add(claim);
            var userMembershipService = new Mock<IUserMembershipService>();
            var renderProjectRepository = new Mock<IDataPersistence<RenderProject>>();
            var mockProjectRepository = new Mock<IDataPersistence<Project>>();
            var mockUserMembershipService = new Mock<IUserMembershipService>();
            var mockLocalProjectsRepository = new Mock<ILocalProjectsRepository>();
            var mockDownloader = new Mock<IOneShotReplicator>();
            var renderProject = new RenderProject(_projectId);
            renderProject.SetLanguage("english");
            renderProjectRepository
                .Setup(x => x.QueryOnFieldAsync("ProjectId", It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()))
                .ReturnsAsync(renderProject);

            var localProjects = new LocalProjects();
            localProjects.AddDownloadedProject(_project.Id);
            
            mockProjectRepository.Setup(x => x.GetAsync(_project.Id))
                .ReturnsAsync(_project);
            mockProjectRepository.Setup(x => x.GetAllOfTypeAsync(It.IsAny<int>(), It.IsAny<bool>()))
                .ReturnsAsync(new List<Project>{_project});
            mockUserMembershipService.Setup(x =>
                x.HasExplicitPermissionForProject(It.IsAny<IUser>(), It.IsAny<Guid>())).Returns(true);

            mockLocalProjectsRepository.Setup(x => x.GetLocalProjectsForMachine())
                .ReturnsAsync(localProjects);
            MockContextProvider.Setup(x => x.GetLocalProjectsRepository())
                .Returns(mockLocalProjectsRepository.Object);
            MockContextProvider.Setup(x => x.GetPersistence<Project>())
                .Returns(mockProjectRepository.Object);
            MockContextProvider.Setup(x => x.GetPersistence<RenderProject>())
                .Returns(renderProjectRepository.Object);
            MockContextProvider.Setup(x => x.GetUserMembershipService())
                .Returns(mockUserMembershipService.Object);
            MockContextProvider.Setup(x => x.GetLoggedInUser())
                .Returns(user);
            MockContextProvider.Setup(x => x.GetUserMembershipService())
                .Returns(userMembershipService.Object);
            MockContextProvider.Setup(x => x.GetOneShotReplicator(
                It.IsAny<List<Guid>>(), It.IsAny<List<Guid>>(), It.IsAny<string>(), 
                It.IsAny<string>())).Returns(mockDownloader.Object);
        }

        private static async Task<bool> FalseCondition() => await Task.FromResult(false);
    }
}