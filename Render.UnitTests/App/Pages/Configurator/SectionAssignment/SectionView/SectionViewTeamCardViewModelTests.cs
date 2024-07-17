using FluentAssertions;
using Render.Models.Users;
using Render.Models.Workflow;
using Render.Pages.Configurator.SectionAssignment;
using Render.Pages.Configurator.SectionAssignment.SectionView;
using Render.UnitTests.App.Kernel;

namespace Render.UnitTests.App.Pages.Configurator.SectionAssignment.SectionView
{
    public class SectionViewTeamCardViewModelTests : ViewModelTestBase
    {
        [Fact]
        public void UserCount_SetsProperly()
        {
            //Arrange
            var user = new RenderUser("User", Guid.Empty);
            var team = new Team(1);
            team.UpdateTranslator(user.Id);
            team.AddSectionAssignment(Guid.Empty, 1);
            var teamTranslatorUser = new TeamTranslatorUser(team, user);

            //Act
            var vm = new SectionViewTeamCardViewModel(MockContextProvider.Object, teamTranslatorUser);

            //Assert
            vm.Count.Should().Be(1);
        }

        [Fact]
        public void UserCount_ReactsToChanges()
        {

            //Arrange
            var user = new RenderUser("User", Guid.Empty);
            var team = new Team(1);
            team.UpdateTranslator(user.Id);
            team.AddSectionAssignment(Guid.Empty, 1);
            var teamTranslatorUser = new TeamTranslatorUser(team, user);

            //Act
            var vm = new SectionViewTeamCardViewModel(MockContextProvider.Object, teamTranslatorUser);
            team.AddSectionAssignment(Guid.NewGuid(), 2);

            //Assert
            vm.Count.Should().Be(2);
        }
    }
}