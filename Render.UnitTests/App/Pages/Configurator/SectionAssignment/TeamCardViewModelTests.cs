using Render.Models.Sections;
using Render.Pages.Configurator.SectionAssignment.Cards.Section;
using Render.Pages.Configurator.SectionAssignment.Cards.Team;
using Render.UnitTests.App.Kernel;

namespace Render.UnitTests.App.Pages.Configurator.SectionAssignment
{
    public class TeamCardViewModelTests : ViewModelTestBase
    {
        [Fact]
        public void AddSection_Succeeds()
        {
            //Arrange
            var section = Section.UnitTestEmptySection();
            var sectionVm = new SectionCardViewModel(MockContextProvider.Object)
            {
                SectionId = section.Id,
            };
            var teamVm = new TeamCardViewModel(MockContextProvider.Object)
            {
                TeamId = Guid.Empty,
            };

            //Act
            teamVm.AssignSection(sectionVm);

            //Assert
            teamVm.AssignedSections.Count.Should().Be(1);
        }

        [Fact]
        public void RemoveAssignment_Succeeds()
        {
            //Arrange
            var section = Section.UnitTestEmptySection();
            var sectionVm = new SectionCardViewModel(MockContextProvider.Object)
            {
                SectionId = section.Id,
            };
            var teamVm = new TeamCardViewModel(MockContextProvider.Object)
            {
                TeamId = Guid.Empty,
            };

            //Act
            teamVm.AssignSection(sectionVm);
            teamVm.RemoveSectionAssignement(sectionVm);

            //Assert
            teamVm.AssignedSections.Count.Should().Be(0);
        }
    }
}