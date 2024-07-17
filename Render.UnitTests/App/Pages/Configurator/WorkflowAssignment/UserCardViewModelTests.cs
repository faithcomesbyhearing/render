using FluentAssertions;
using Render.Models.Users;
using Render.UnitTests.App.Kernel;
using Render.Pages.Configurator.WorkflowAssignment.Cards;

namespace Render.UnitTests.App.Pages.Configurator.WorkflowAssignment
{
    public class UserCardViewModelTests : ViewModelTestBase
    {
        [Fact]
        public void ViewModel_CreatesProperly()
        {
            //Arrange
            var user = new RenderUser("Full Name", Guid.NewGuid());

            //Act
            var vm = new UserCardViewModel(user, MockContextProvider.Object);

            //Assert
            vm.Name.Should().Be(user.Username);
            vm.FullName.Should().Be(user.FullName);
            vm.User.Should().Be(user);
        }
    }
}