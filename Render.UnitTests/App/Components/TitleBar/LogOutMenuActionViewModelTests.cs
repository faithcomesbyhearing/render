using Moq;
using Render.Components.TitleBar.MenuActions;
using Render.Kernel.WrappersAndExtensions;
using Render.Models.LocalOnlyData;
using Render.Models.Users;
using Render.Pages.AppStart.Login;
using Render.Repositories.Kernel;
using Render.Repositories.LocalDataRepositories;
using Render.Repositories.UserRepositories;
using Render.Services.SyncService;
using Render.Services.UserServices;
using Render.TempFromVessel.Project;
using Render.UnitTests.App.Kernel;

namespace Render.UnitTests.App.Components.TitleBar
{
    public class LogOutMenuActionViewModelTests : ViewModelTestBase
    {
        public LogOutMenuActionViewModelTests()
        {
            var mockLocalProjectsRepository = new Mock<ILocalProjectsRepository>();
            MockContextProvider.Setup(x => x.GetLocalProjectsRepository())
                .Returns(mockLocalProjectsRepository.Object);
            var mockModalService = new Mock<IModalService>();
            MockContextProvider.Setup(x => x.GetModalService()).Returns(mockModalService.Object);
            var localSyncService = new Mock<ILocalSyncService>();
            MockContextProvider.Setup(x => x.GetLocalSyncService())
                .Returns(localSyncService.Object);
            
            var userPersistence = new Mock<IDataPersistence<User>>();
            var mockUserRepository = new Mock<IUserRepository>();
            var syncService = new Mock<ISyncService>();
            var localProjectsRepository = new Mock<ILocalProjectsRepository>();
            var userMembershipService = new Mock<IUserMembershipService>();
            var projectsRepository = new Mock<IDataPersistence<Project>>();
            var mockUserMachineSettingsRepository = new Mock<IUserMachineSettingsRepository>();

            var user = new User("Test User", "testuser"); 
            mockUserRepository.Setup(x => x.GetAllUsersAsync()).ReturnsAsync(new List<IUser>{user});
            var project = new Project("Test Project", "ref", "iso");
            var userMachineSettings = new UserMachineSettings(user.Id);
            mockUserMachineSettingsRepository.Setup(x => x.GetUserMachineSettingsForUserAsync(user.Id))
            .ReturnsAsync(userMachineSettings);
            userPersistence.Setup(x => x.GetAllOfTypeAsync(It.IsAny<int>(), It.IsAny<bool>()))
                .ReturnsAsync(new List<User>(){user});
            userPersistence.Setup(x => x.QueryOnFieldAsync("Username", user.Username, It.IsAny<bool>(), It.IsAny<bool>()))
                .ReturnsAsync(user);
            userMembershipService.Setup(x => x.HasInheritedPermissionForProject(user, project)).ReturnsAsync(true);
            projectsRepository.Setup(x => x.GetAsync(project.Id)).ReturnsAsync(project);
            projectsRepository.Setup(x => x.GetAllOfTypeAsync(It.IsAny<int>(), It.IsAny<bool>()))
                .ReturnsAsync(new List<Project>{project});
            localProjectsRepository.Setup(x => x.GetLocalProjectsForMachine())
                .ReturnsAsync(new LocalProjects());
            MockContextProvider.Setup(x => x.GetPersistence<User>())
                .Returns(userPersistence.Object);
            MockContextProvider.Setup(x => x.GetLocalProjectsRepository())
                .Returns(localProjectsRepository.Object);
            MockContextProvider.Setup(x => x.GetPersistence<Project>())
                .Returns(projectsRepository.Object);
            MockContextProvider.Setup(x => x.GetUserMembershipService()).Returns(userMembershipService.Object);
            MockContextProvider.Setup(x => x.GetSyncService()).Returns(syncService.Object);
            MockContextProvider.Setup(x => x.GetLoggedInUser()).Returns(user);
            MockContextProvider.Setup(x => x.GetUserMachineSettingsRepository()).Returns
            (mockUserMachineSettingsRepository.Object);
            MockContextProvider.Setup(x => x.GetUserRepository()).Returns(mockUserRepository.Object);
        }
        
        [Fact]
        public async Task Command_LogsOutAndNavigatesToLogin()
        {
            //Arrange
            var vm = new LogOutActionViewModel(MockContextProvider.Object, string.Empty);
            SetupViewModelForNavigationTest(vm);
            
            //Act/Assert
            await VerifyNavigationResultAsync<LoginViewModel>(vm.Command);
            MockContextProvider.Verify(x => x.ClearLoggedInUser());
        }
        
        [Fact]
        public async Task Command_WhenOnDifferentStack_LogsOutAndNavigatesToLogin()
        {
            //Arrange
            var vm = new LogOutActionViewModel(MockContextProvider.Object,string.Empty);
            SetupViewModelForNavigationTest(vm);

            //Act/Assert
            await VerifyNavigationResultAsync<LoginViewModel>(vm.Command);
            MockContextProvider.Verify(x => x.ClearLoggedInUser());
        }
    }
}