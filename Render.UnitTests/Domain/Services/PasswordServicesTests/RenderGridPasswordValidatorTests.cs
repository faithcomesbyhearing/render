using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Render.Models.Users;
using Render.Services.PasswordServices;
using Render.UnitTests.App.Kernel;

namespace Render.UnitTests.Domain.Services.PasswordServicesTests
{
    public class RenderGridPasswordValidatorTests : TestBase
    {
        private RenderUser _user;
        public RenderGridPasswordValidatorTests()
        {
            _user = new RenderUser("RenderUser", Guid.Empty)
            {
                HashedPassword = "00010203"
            };
        }

        [Fact]
        public void ValidateCorrectPassword_ReturnsSuccess()
        {
            //Arrange
            var validator = new RenderGridPasswordValidator();

            //Act
            var result = validator.ValidatePassword(_user, "00010203");

            //Assert
            result.Should().Be(PasswordVerificationResult.Success);
        }

        [Fact]
        public void ValidateIncorrectPassword_ReturnsFailure()
        {
            //Arrange
            var validator = new RenderGridPasswordValidator();

            //Act
            var result = validator.ValidatePassword(_user, "00");

            //Assert
            result.Should().Be(PasswordVerificationResult.Failed);
        }
    }
}