using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Render.Models.Users;
using Render.Services.PasswordServices;
using Render.UnitTests.App.Kernel;

namespace Render.UnitTests.Domain.Services.PasswordServicesTests
{
    public class RenderTextPasswordValidatorTests : TestBase
    {
        private readonly RenderUser _user;
        
        public RenderTextPasswordValidatorTests()
        {
            _user = new RenderUser("RenderUser", Guid.Empty)
            {
                HashedPassword = "password"
            };
        }

        [Fact]
        public void ValidateCorrectPassword_ReturnsSuccess()
        {
            //Arrange
            var validator = new RenderTextPasswordValidator();

            //Act
            var result = validator.ValidatePassword(_user, "password");

            //Assert
            result.Should().Be(PasswordVerificationResult.Success);
        }

        [Fact]
        public void ValidateIncorrectPassword_ReturnsFailure()
        {
            //Arrange
            var validator = new RenderTextPasswordValidator();

            //Act
            var result = validator.ValidatePassword(_user, "wrong");

            //Assert
            result.Should().Be(PasswordVerificationResult.Failed);
        }
    }
}