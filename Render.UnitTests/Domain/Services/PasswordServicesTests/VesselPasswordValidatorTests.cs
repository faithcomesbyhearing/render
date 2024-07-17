using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Render.Models.Users;
using Render.Services.PasswordServices;
using Render.UnitTests.App.Kernel;

namespace Render.UnitTests.Domain.Services.PasswordServicesTests
{
    public class VesselPasswordValidatorTests : TestBase
    {
        private User User { get; }

        public VesselPasswordValidatorTests()
        {
            User = new User("Test User", "testuser");
            var hasher = new PasswordHasher<User>();
            User.HashedPassword = hasher.HashPassword(User, "password");
        }

        [Fact]
        public void ValidateCorrectPassword_ReturnsSuccess()
        {
            //Arrange
            var validator = new VesselPasswordValidator();

            //Act
            var result = validator.ValidatePassword(User, "password");
            //Assert
            result.Should().Be(PasswordVerificationResult.Success);
        }

        [Fact]
        public void ValidateIncorrectPassword_ReturnsFailure()
        {
            //Arrange
            var validator = new VesselPasswordValidator();

            //Act
            var result = validator.ValidatePassword(User, "wrong");

            //Assert
            result.Should().Be(PasswordVerificationResult.Failed);
        }
    }
}