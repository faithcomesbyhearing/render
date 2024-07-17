using Render.Models.Users;
using Render.Services.PasswordServices;
using Render.UnitTests.App.Kernel;

namespace Render.UnitTests.Domain.Services.PasswordServicesTests
{
    public class PasswordValidatorFactoryTests : TestBase
    {
        private readonly PasswordValidatorFactory _factory;
        public PasswordValidatorFactoryTests()
        {
            _factory = new PasswordValidatorFactory();
        }

        [Fact]
        public void GetValidator_VesselUser_ReturnsVesselPasswordValidator()
        {
            //Arrange
            var user = new User("Test User", "testuser");

            //Act
            var validator = _factory.GetValidator(user);

            //Assert
            validator.Should().BeAssignableTo(typeof(VesselPasswordValidator));
        }

        [Fact]
        public void GetValidator_RenderUser_Grid_ReturnsRenderGridPasswordValidator()
        {
            //Arrange
            var user = new RenderUser("User", Guid.Empty);

            //Act
            var validator = _factory.GetValidator(user);

            //Assert
            validator.Should().BeAssignableTo(typeof(RenderGridPasswordValidator));
        }

        [Fact]
        public void GetValidator_RenderUser_Text_ReturnsRenderTextPasswordValidator()
        {

            //Arrange
            var user = new RenderUser("User", Guid.Empty)
            {
                IsGridPassword = false
            };

            //Act
            var validator = _factory.GetValidator(user);

            //Assert
            validator.Should().BeAssignableTo(typeof(RenderTextPasswordValidator));
        }
    }
}