using Render.Services.PasswordServices;
using Render.UnitTests.App.Kernel;

namespace Render.UnitTests.Domain.Services.PasswordServicesTests
{
    public class PasswordServiceTests : TestBase
    {
        private readonly IPasswordService _passwordService;

        public PasswordServiceTests()
        {
            _passwordService = new PasswordService();
        }

        [Fact]
        public void GeneratePassword_ShouldReturnPassword()
        {
            //Arrange
            //Act
            var password = _passwordService.GeneratePassword();
            //Assert
            password.Should().NotBeEmpty();
        }
    }
}