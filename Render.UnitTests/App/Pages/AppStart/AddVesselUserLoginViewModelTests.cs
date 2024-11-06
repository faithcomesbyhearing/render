using Render.WebAuthentication;
using Moq;
using Render.Models.Users;
using Render.Pages.AppStart.Login;
using Render.Repositories.UserRepositories;
using Render.UnitTests.App.Kernel;
using Render.Resources.Localization;

namespace Render.UnitTests.App.Pages.AppStart
{
    public class AddVesselUserLoginViewModelTests : ViewModelTestBase
    {
        private readonly User _user;
        private readonly Mock<IUserRepository> _userRepository;
        private readonly Mock<IAuthenticationApiWrapper> _mockAuthenticationApiWrapper;

        public AddVesselUserLoginViewModelTests()
        {
            _user = new User("Test User", "testuser")
            {
                HashedPassword = "password"
            };

            _userRepository = new Mock<IUserRepository>();
            _mockAuthenticationApiWrapper = new Mock<IAuthenticationApiWrapper>();

            _userRepository.Setup(x => x.GetAllUsersAsync()).ReturnsAsync(new List<IUser> { _user });
            _userRepository.Setup(x => x.GetUserAsync(_user.Username)).ReturnsAsync((User)null);

            _mockAuthenticationApiWrapper.Setup(x => x.AuthenticateUserAsync(_user.Username, It.IsAny<string>()))
                .ReturnsAsync(new AuthenticationApiWrapper.AuthenticationResult(true, Guid.NewGuid().ToString()));
            _mockAuthenticationApiWrapper.Setup(x => x.AuthenticateRenderUserForSyncAsync(It.IsAny<string>(), It.IsAny<string>(), Guid.Empty))
                .ReturnsAsync(new AuthenticationApiWrapper.SyncGatewayUser(_user.Id, "login"));
            
            MockContextProvider.Setup(x => x.GetUserRepository()).Returns(_userRepository.Object);
            MockContextProvider.Setup(x => x.GetAuthenticationApiWrapper()).Returns(_mockAuthenticationApiWrapper.Object);
        }

        [Fact]
        public void UserNotOnMachine_CallsToApi()
        {
			//Arrange
			var syncGatewayApiWrapper = new Mock<ISyncGatewayApiWrapper>();
			syncGatewayApiWrapper.Setup(x => x.IsConnected()).ReturnsAsync(false);
			MockContextProvider.Setup(x => x.GetSyncGatewayApiWrapper()).Returns(syncGatewayApiWrapper.Object);
			var vm = new AddVesselUserLoginViewModel(MockContextProvider.Object);

			//Act
			vm.UsernameViewModel.Value = _user.Username;
            vm.PasswordViewModel.Value = _user.HashedPassword;
            vm.LoginCommand.Execute().Subscribe();

            //Assert
            _mockAuthenticationApiWrapper.Verify(x => x.AuthenticateUserAsync(_user.Username,
                _user.HashedPassword));
            _mockAuthenticationApiWrapper.Verify(x => x.AuthenticateRenderUserForSyncAsync(
                _user.Username, _user.HashedPassword, Guid.Empty));
        }

        [Fact]
        public void UserNotOnMachine_ApiReturnsFalse_SetsValidation()
        {
            //Arrange
            var vm = new AddVesselUserLoginViewModel(MockContextProvider.Object);
            _mockAuthenticationApiWrapper.Setup(x => x.AuthenticateUserAsync(_user.Username, "wrong"))
                .ReturnsAsync(new AuthenticationApiWrapper.AuthenticationResult(false, ""));
            //Act
            vm.UsernameViewModel.Value = _user.Username;
            vm.PasswordViewModel.Value = "wrong";
            vm.LoginCommand.Execute().Subscribe();

            //Assert
            vm.PasswordViewModel.CheckValidation().Should().BeFalse();
            vm.PasswordViewModel.ValidationMessage.Should().Be(AppResources.IncorrectPassword);
        }

        [Fact]
        public void UserOnMachine_Succeeds()
        {
            //Arrange
            _userRepository.Setup(x => x.GetUserAsync(_user.Username)).ReturnsAsync(_user);
            var vm = new AddVesselUserLoginViewModel(MockContextProvider.Object);

            //Act
            vm.UsernameViewModel.Value = _user.Username;
            vm.PasswordViewModel.Value = "wrong";
            vm.LoginCommand.Execute().Subscribe();

            //Assert
            _mockAuthenticationApiWrapper.Verify(
                x => x.AuthenticateUserAsync(It.IsAny<string>(), It.IsAny<string>()),
                Times.Never);
        }
    }
}