using FluentAssertions;
using Render.Components.PasswordGrid;
using Render.UnitTests.App.Kernel;

namespace Render.UnitTests.App.Pages.Configurator
{
    public class PasswordGridViewModelTests : ViewModelTestBase
    {
        [Fact]
        public void AddHex_ToBlankPassword_Succeeds()
        {
            //Arrange
            var vm = new PasswordGridViewModel(MockContextProvider.Object, "");
            vm.CreatePassword.Should().BeTrue();

            //Act
            vm.AddToPassword("00");

            //Assert
            vm.Password.Should().Be("00");
        }

        [Fact]
        public void AddHex_AlreadyInPassword_DoesNotAddToPassword()
        {
            //Arrange
            var vm = new PasswordGridViewModel(MockContextProvider.Object, "");

            //Act
            vm.AddToPassword("0001");
            vm.AddToPassword("00");

            //Assert
            vm.Password.Should().Be("00");
        }

        [Fact]
        public void AddHex_ToExistingPassword_AddsToPassword()
        {
            //Arrange
            var vm = new PasswordGridViewModel(MockContextProvider.Object, "");

            //Act
            vm.AddToPassword("0001");
            vm.AddToPassword("02");

            //Assert
            vm.Password.Should().Be("000102");
        }

        [Fact]
        public void AddHex_WhenNotInCreate_DoesNotAddToPassword()
        {
            //Arrange
            var vm = new PasswordGridViewModel(MockContextProvider.Object, "0001");

            //Act
            vm.AddToPassword("02");

            //Assert
            vm.Password.Should().Be("0001");
        }

        [Fact]
        public void ResetPassword_ResetsPassword()
        {
            //Arrange
            var vm = new PasswordGridViewModel(MockContextProvider.Object, "0001");

            //Act
            vm.ResetPassword();

            //Assert
            vm.Password.Should().Be("");
            vm.CreatePassword.Should().BeTrue();
        }

        [Fact]
        public void AddHex_NotInAValidSpot_DoesNotAddToPassword()
        {
            //Arrange
            var vm = new PasswordGridViewModel(MockContextProvider.Object, "");

            //Act
            vm.AddToPassword("00");
            vm.AddToPassword("0A");

            //Assert
            vm.Password.Should().Be("0A");
        }
    }
}