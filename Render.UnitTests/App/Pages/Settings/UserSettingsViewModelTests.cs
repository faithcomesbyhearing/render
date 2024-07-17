using FluentAssertions;
using Moq;
using Render.Kernel;
using Render.Models.LocalOnlyData;
using Render.Models.Sections;
using Render.Models.Users;
using Render.Models.Workflow;
using Render.Pages.AppStart.Home;
using Render.Pages.AppStart.ProjectSelect;
using Render.Pages.Settings.ManageUsers;
using Render.Repositories.Kernel;
using Render.Repositories.LocalDataRepositories;
using Render.Repositories.SectionRepository;
using Render.Repositories.UserRepositories;
using Render.Repositories.WorkflowRepositories;
using Render.Resources.Localization;
using Render.Services;
using Render.Services.SessionStateServices;
using Render.TempFromVessel.Project;
using Render.UnitTests.App.Kernel;

namespace Render.UnitTests.App.Pages.Settings
{
    public class UserSettingsViewModelTests : ViewModelTestBase
    {
        private IUser _user;
        private readonly Project _project;
        private readonly RenderWorkflow _renderWorkflow = RenderWorkflow.Create(Guid.Empty);

        private readonly Mock<IUserRepository> _mockUserRepository = new();
        private readonly Mock<IWorkflowRepository> _mockWorkflowRepository = new();
        private readonly Mock<ISectionRepository> _mockSectionRepository = new();

        public UserSettingsViewModelTests()
        {
            var mockProjectRepository = new Mock<IDataPersistence<Project>>();
            var mockLocalizationService = new Mock<ILocalizationService>();
            MockContextProvider.Setup(x => x.GetUserRepository()).Returns(_mockUserRepository.Object);
            MockContextProvider.Setup(x => x.GetWorkflowRepository()).Returns(_mockWorkflowRepository.Object);
            MockContextProvider.Setup(x => x.GetLocalizationService()).Returns(mockLocalizationService.Object);

            _project = new Project("Test", "TST", "ISO");
            mockProjectRepository.Setup(x => x.GetAsync(It.IsAny<Guid>())).ReturnsAsync(_project);
            mockProjectRepository.Setup(x => x.GetAllOfTypeAsync(It.IsAny<int>(),
                It.IsAny<bool>())).ReturnsAsync(new List<Project>());
            MockContextProvider.Setup(x => x.GetPersistence<Project>()).Returns(mockProjectRepository.Object);
            MockGrandCentralStation.Setup(x => x.ProjectWorkflow).Returns(new RenderWorkflow(Guid.Empty));
            MockGrandCentralStation.Setup(x => x.GetStepToWorkAsync(It.IsAny<Guid>(), It.IsAny<RenderStepTypes>()))
                .ReturnsAsync(new Step());
            MockContextProvider.Setup(x => x.GetSectionRepository()).Returns(_mockSectionRepository.Object);
            MockContextProvider.Setup(x => x.GetWorkflowRepository())
                .Returns(_mockWorkflowRepository.Object);

            var section = Section.UnitTestEmptySection();
            var sectionsToApprove = new List<Section> { section };
            _mockSectionRepository.Setup(x => x.GetSectionsForProjectAsync(It.IsAny<Guid>())).ReturnsAsync(sectionsToApprove);
            _mockWorkflowRepository.Setup(x => x.GetWorkflowForProjectIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync(_renderWorkflow);
            MockContextProvider.Setup(x => x.GetSessionStateService()).Returns(new Mock<ISessionStateService>().Object);
            var mockLocalProjectsRepository = new Mock<ILocalProjectsRepository>();
            var localProjects = new LocalProjects();
            mockLocalProjectsRepository.Setup(x => x.GetLocalProjectsForMachine())
                .ReturnsAsync(localProjects);
            MockContextProvider.Setup(x => x.GetLocalProjectsRepository())
                .Returns(mockLocalProjectsRepository.Object);
            var userName = "Test User";
            _user = new RenderUser(userName, _project.Id);
            MockContextProvider.Setup(x => x.GetLoggedInUser()).Returns(_user);
        }

        [Fact]
        public async Task UserSettings_Create_RenderUser()
        {
            //Arrange
            var userName = "Test User";
            _user = new RenderUser(userName, _project.Id);
            MockContextProvider.Setup(x => x.GetLoggedInUser()).Returns(_user);

            //Act
            var vm = await UserSettingsViewModel.CreateAsync(MockContextProvider.Object, _project.Id, false);

            //Assert
            vm.IsRenderUser.Should().BeTrue();
            vm.UserName.Should().Be(userName);
            vm.Password.Should().BeEmpty();
        }

        [Fact]
        public async Task ProceedButtonInactive_WhenUsernameIsBlank()
        {
            //Arrange
            var user = new RenderUser("", _project.Id);
            //Act
            var vm = await UserSettingsViewModel.CreateAsync(MockContextProvider.Object, _project.Id, true, user);
            vm.UserName = string.Empty;
            vm.Password = "0001";
            //Assert
            vm.ProceedButtonViewModel.ProceedActive.Should().BeFalse();
        }

        [Fact]
        public async Task ProceedButtonInactive_WhenPasswordIsBlank()
        {
            //Arrange
            var user = new RenderUser("", _project.Id);
            //Act
            var vm = await UserSettingsViewModel.CreateAsync(MockContextProvider.Object, _project.Id, true, user);
            vm.UserName = "Name";

            //Assert
            vm.ProceedButtonViewModel.ProceedActive.Should().BeFalse();
        }

        [Fact]
        public async Task ProceedButtonActive_GridPassword_WhenPasswordAndNameAreNotEmpty()
        {
            //Arrange
            var user = new RenderUser("", _project.Id);
            //Act
            var vm = await UserSettingsViewModel.CreateAsync(MockContextProvider.Object, _project.Id, true, user);
            vm.UserName = "Name";
            vm.PasswordGridViewModel.AddToPassword("00010203");

            //Assert
            vm.ProceedButtonViewModel.ProceedActive.Should().BeTrue();
        }

        [Fact]
        public async Task ProceedButtonActive_TextPassword_WhenPasswordAndNameAreNotEmpty()
        {
            //Arrange
            var user = new RenderUser("", _project.Id);
            //Act
            var vm = await UserSettingsViewModel.CreateAsync(MockContextProvider.Object, _project.Id, true, user);
            vm.UserName = "Name";
            vm.SelectedPasswordType = PasswordType.Text;
            vm.Password = "Password";

            //Assert
            vm.ProceedButtonViewModel.ProceedActive.Should().BeTrue();
        }

        [Fact]
        public async Task UserSettings_Create_GlobalUser()
        {
            //Arrange
            var userName = "Test User";
            _user = new User(userName, "TestUser");
            MockContextProvider.Setup(x => x.GetLoggedInUser()).Returns(_user);

            //Act
            var vm = await UserSettingsViewModel.CreateAsync(MockContextProvider.Object, _project.Id, false);
            vm.SelectedLocalizedPasswordType = new LocalizedPasswordTypes(PasswordType.Grid, AppResources.Grid);

            //Assert
            vm.IsRenderUser.Should().BeFalse();
            vm.UserName.Should().Be(userName);
            vm.Password.Should().BeEmpty();
        }

        [Fact]
        public async Task UserSettings_Save_Password_RenderUser()
        {
            //Arrange
            var userName = "Test User";
            _user = new RenderUser(userName, _project.Id);
            MockContextProvider.Setup(x => x.GetLoggedInUser()).Returns(_user);
            var vm = await UserSettingsViewModel.CreateAsync(MockContextProvider.Object, _project.Id, false);
            var newPassword = "new password";
            vm.Password = newPassword;

            //Act
            vm.ProceedButtonViewModel.NavigateToPageCommand.Execute().Subscribe();

            //Assert
            vm.IsRenderUser.Should().BeTrue();
            vm.UserName.Should().Be(userName);
            _user.HashedPassword.Should().Be(newPassword);
        }

        [Fact]
        public async Task UserSettings_Save_UserName_RenderUser()
        {
            //Arrange
            var userName = "Test User";
            _user = new RenderUser(userName, _project.Id);
            MockContextProvider.Setup(x => x.GetLoggedInUser()).Returns(_user);
            var vm = await UserSettingsViewModel.CreateAsync(MockContextProvider.Object, _project.Id, false);
            vm.UserName = "new name";
            SetupViewModelForNavigationTest(vm);

            //Act
            vm.ProceedButtonViewModel.NavigateToPageCommand.Execute().Subscribe();

            //Assert
            vm.IsRenderUser.Should().BeTrue();
            _mockUserRepository.Verify(x => x.SaveUserAsync(It.IsAny<IUser>()), Times.Once());
        }

        [Fact]
        public async Task UserSettings_Save_Localization_RenderUser()
        {
            //Arrange
            var userName = "Test User";
            _user = new RenderUser(userName, _project.Id);
            MockContextProvider.Setup(x => x.GetLoggedInUser()).Returns(_user);
            var vm = await UserSettingsViewModel.CreateAsync(MockContextProvider.Object, _project.Id, false);
            vm.SelectedLocalizationResource = new LocalizationResource("ta", "Tamil");
            SetupViewModelForNavigationTest(vm);

            //Act
            //Assert
            await VerifyNavigationResultAsync<ProjectSelectViewModel>(vm.ProceedButtonViewModel.NavigateToPageCommand);
            vm.IsRenderUser.Should().BeTrue();
            _mockUserRepository.Verify(x => x.SaveUserAsync(It.IsAny<IUser>()), Times.Once());
        }

        [Fact]
        public async Task UserSettings_Save_NavigateHome()
        {
            //Arrange
            var userName = "Test User";
            _user = new RenderUser(userName, _project.Id);
            MockContextProvider.Setup(x => x.GetLoggedInUser()).Returns(_user);
            var mockGrandCentralStation = new Mock<IGrandCentralStation>();
            MockContextProvider.Setup(x => x.GetGrandCentralStation())
                .Returns(mockGrandCentralStation.Object);
            mockGrandCentralStation.Setup(x => x.StepsAssignedToUser())
                .Returns(new List<Guid>());
            mockGrandCentralStation.Setup(x => x.SectionsAtStep(It.IsAny<Guid>()))
                .Returns(new List<Guid>());
            mockGrandCentralStation.Setup(x => x.ProjectWorkflow).Returns(MockRenderWorkflow.Object);
            var vm = await UserSettingsViewModel.CreateAsync(MockContextProvider.Object, _project.Id, false);
            var homeVm = await HomeViewModel.CreateAsync(_project.Id, MockContextProvider.Object);
            vm.SelectedLocalizationResource = new LocalizationResource("ta", "Tamil");
            SetupViewModelForNavigationTest(vm, homeVm);

            //Act
            //Assert
            await VerifyNavigationResultAsync<HomeViewModel>(vm.ProceedButtonViewModel.NavigateToPageCommand);
        }

        [Fact]
        public async Task UserSettings_Save_GlobalUser()
        {
            //Arrange
            var userName = "Test User";
            _user = new User(userName, "TestUser");
            MockContextProvider.Setup(x => x.GetLoggedInUser()).Returns(_user);
            var vm = await UserSettingsViewModel.CreateAsync(MockContextProvider.Object, _project.Id, false);
            vm.SelectedLocalizationResource = new LocalizationResource("ta", "Tamil");
            SetupViewModelForNavigationTest(vm);

            //Act
            vm.ProceedButtonViewModel.NavigateToPageCommand.Execute().Subscribe();

            //Assert
            vm.IsRenderUser.Should().BeFalse();
            vm.UserName.Should().Be(userName);
            _mockUserRepository.Verify(x => x.SaveUserAsync(It.IsAny<IUser>()), Times.Once());
        }

        [Fact]
        public async Task UserSettings_NotSave_Password_GlobalUser()
        {
            //Arrange
            var userName = "Test User";
            _user = new User(userName, "TestUser");
            MockContextProvider.Setup(x => x.GetLoggedInUser()).Returns(_user);
            var vm = await UserSettingsViewModel.CreateAsync(MockContextProvider.Object, _project.Id, false);
            vm.Password = "new password";
            SetupMocksForHomeViewModelNavigation();

            //Act
            vm.ProceedButtonViewModel.NavigateToPageCommand.Execute().Subscribe();

            //Assert
            vm.IsRenderUser.Should().BeFalse();
            vm.UserName.Should().Be(userName);
            _mockUserRepository.Verify(x => x.SaveUserAsync(It.IsAny<IUser>()), Times.Never);
        }

        [Fact]
        public async Task UserSettings_NotSave_Username_GlobalUser()
        {
            //Arrange
            var userName = "Test User";
            _user = new User(userName, "TestUser");
            MockContextProvider.Setup(x => x.GetLoggedInUser()).Returns(_user);
            var vm = await UserSettingsViewModel.CreateAsync(MockContextProvider.Object, _project.Id, false);
            vm.UserName = "new name";
            SetupMocksForHomeViewModelNavigation();

            //Act
            vm.ProceedButtonViewModel.NavigateToPageCommand.Execute().Subscribe();

            //Assert
            vm.IsRenderUser.Should().BeFalse();
            _mockUserRepository.Verify(x => x.SaveUserAsync(It.IsAny<IUser>()), Times.Never);
        }

        [Fact]
        public async Task UserSettings_DeleteUser_Succeeds()
        {
            //Arrange
            var userName = "Test User";
            _user = new RenderUser(userName, _project.Id);
            MockContextProvider.Setup(x => x.GetLoggedInUser()).Returns(_user);
            var vm = await UserSettingsViewModel.CreateAsync(MockContextProvider.Object, _project.Id, false);
            SetupMocksForHomeViewModelNavigation();

            //Act
            vm.DeleteUserCommand.Execute().Subscribe();

            //Assert
            _mockUserRepository.Verify(x => x.DeleteUserAsync(It.IsAny<IUser>()), Times.Once);
        }
    }
}