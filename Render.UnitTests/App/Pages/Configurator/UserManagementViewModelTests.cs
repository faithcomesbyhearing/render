using FluentAssertions;
using Moq;
using Render.Models.Users;
using Render.Pages.Configurator.UserManagement;
using Render.Repositories.Kernel;
using Render.Repositories.UserRepositories;
using Render.Resources.Localization;
using Render.Services.UserServices;
using Render.TempFromVessel.Project;
using Render.TempFromVessel.User;
using Render.UnitTests.App.Kernel;


namespace Render.UnitTests.App.Pages.Configurator
{
    public class UserManagementViewModelTests : ViewModelTestBase
    {
        private readonly Mock<IUserRepository> _mockUserRepository;
        private readonly Guid _projectId;
        private readonly User _user;
        private readonly User _user2;
        private readonly RenderUser _renderUser;
        private readonly RenderUser _renderUser2;
        private readonly Project _project;

        public UserManagementViewModelTests()
        {
            _project = new Project("Render Project", "ref", "iso");
            _projectId = _project.Id;
            _user = new User("User 1", "user1");
            _user2 = new User("User 2", "user2");
            _renderUser = new RenderUser("Render User 1", _projectId);
            _renderUser2 = new RenderUser("Render User 2", _projectId);
            _mockUserRepository = new Mock<IUserRepository>();
            _mockUserRepository.Setup(x => x.GetUsersForProjectAsync(_project)).ReturnsAsync(new List<IUser>
            {
                _user,
                _user2,
                _renderUser,
                _renderUser2
            });
            var mockProjectRepository = new Mock<IDataPersistence<Project>>();
            mockProjectRepository.Setup(x => x.GetAsync(_projectId)).ReturnsAsync(_project);
            MockContextProvider.Setup(x => x.GetUserRepository()).Returns(_mockUserRepository.Object);
            MockContextProvider.Setup(x => x.GetPersistence<Project>()).Returns(mockProjectRepository.Object);
        }

        [Fact]
        public async Task ViewModelCreation_CreatesProperUserCards()
        {
            //Arrange
            var mockUserMembershipService = new Mock<IUserMembershipService>();
            MockContextProvider.Setup(x => x.GetUserMembershipService())
                .Returns(mockUserMembershipService.Object);
            _mockUserRepository.Setup(x => x.GetUsersForProjectAsync(_project))
                .ReturnsAsync(new List<IUser> { _renderUser, _renderUser2, _user, _user2 });

            //Act
            var vm = await UserManagementPageViewModel.CreateAsync(MockContextProvider.Object, _projectId);
            //Assert
            //These counts are 4 because there is the fake data in the viewmodel to fix the page's view.
            vm.RenderUserTileViewModels.Items.Count.Should().Be(2);
            vm.RenderUserTileViewModels.Items.Select(x => x.FullName).Should().NotContain(_user.FullName);
            vm.RenderUserTileViewModels.Items.Select(x => x.FullName).Should().NotContain(_user2.FullName);
            vm.RenderUserTileViewModels.Items.Select(x => x.FullName).Should().Contain(_renderUser.FullName);
            vm.RenderUserTileViewModels.Items.Select(x => x.FullName).Should().Contain(_renderUser2.FullName);
            vm.VesselUserTileViewModels.Items.Count.Should().Be(2);
            vm.VesselUserTileViewModels.Items.Select(x => x.FullName).Should().Contain(_user.FullName);
            vm.VesselUserTileViewModels.Items.Select(x => x.FullName).Should().Contain(_user2.FullName);
            vm.VesselUserTileViewModels.Items.Select(x => x.FullName).Should().NotContain(_renderUser.FullName);
            vm.VesselUserTileViewModels.Items.Select(x => x.FullName).Should().NotContain(_renderUser2.FullName);
        }

        [Fact]
        public void UserTileViewModel_WithMultipleUserRoles_ConcatenatesCorrectly()
        {
            //Arrange
            var user = new RenderUser("Full Name", _projectId);
            user.Claims.Add(new VesselClaim(RenderRolesAndClaims.ProjectUserClaimType, _projectId.ToString(), RoleName.Configure.GetRoleId()));
            user.Claims.Add(new VesselClaim(RenderRolesAndClaims.ProjectUserClaimType, _projectId.ToString(), RoleName.Consultant.GetRoleId()));

            var expected = $"{AppResources.Configure}, {AppResources.Consultant}";
            //Act
            var vm = new UserTileViewModel(MockContextProvider.Object, user, _project);

            //Assert
            vm.Roles.Should().Be(expected);
        }
    }
}