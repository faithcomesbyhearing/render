using Render.Models.Sections;
using Render.Pages.Configurator.SectionAssignment.Cards.Section;
using Render.Pages.Configurator.SectionAssignment.Cards.Team;
using Render.UnitTests.App.Kernel;

namespace Render.UnitTests.App.Pages.Configurator.SectionAssignment
{
    public class SectionCardViewModelTests : ViewModelTestBase
    {
        [Fact]
        public void ViewModel_WithNoAssignment_ShowAddATeam()
        {
            //Arrange
            var section = Section.UnitTestEmptySection();
            var vm = new SectionCardViewModel(MockContextProvider.Object)
            {
                SectionId = section.Id,
            };

            //Assert
            vm.ShowTeam.Should().BeFalse();
        }

        [Fact]
        public void AddAssignment_ShowsTeam()
        {
            //Arrange
            var section = Section.UnitTestEmptySection();
            var vm = new SectionCardViewModel(MockContextProvider.Object)
            {
                SectionId = section.Id,
            };

            //Act
            vm.AssignTeam(new TeamCardViewModel(MockContextProvider.Object)
            {
                TeamId = Guid.Empty,
            });

            //Assert
            vm.ShowTeam.Should().BeTrue();
        }

        [Fact]
        public void RemoveAssignment_ShowsAddATeam()
        {
            //Arrange
            var section = Section.UnitTestEmptySection();

            //Act
            var vm = new SectionCardViewModel(MockContextProvider.Object)
            {
                SectionId = section.Id,
            };
            vm.AssignTeam(new TeamCardViewModel(MockContextProvider.Object)
            {
                TeamId = Guid.Empty,
            });
            vm.RemoveTeam();

            //Assert
            vm.ShowTeam.Should().BeFalse();
        }
    }
}