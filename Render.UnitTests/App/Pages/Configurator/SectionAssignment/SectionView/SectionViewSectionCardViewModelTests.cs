using FluentAssertions;
using Render.Kernel;
using Render.Models.Sections;
using Render.Models.Users;
using Render.Models.Workflow;
using Render.Pages.Configurator.SectionAssignment;
using Render.Pages.Configurator.SectionAssignment.SectionView;
using Render.UnitTests.App.Kernel;

namespace Render.UnitTests.App.Pages.Configurator.SectionAssignment.SectionView
{
    public class SectionViewSectionCardViewModelTests : ViewModelTestBase
    {
        private readonly Func<TeamSectionAssignment, Task> _updateWorkflowCallback;
        private readonly Func<TeamSectionAssignment, Task> _reorderSectionsCallback;

        public SectionViewSectionCardViewModelTests()
        {
            _updateWorkflowCallback = _ => Task.CompletedTask;
            _reorderSectionsCallback = _ => Task.CompletedTask;
        }

        [Fact]
        public void ViewModel_WithNoAssignment_ShowAddATeam()
        {
            //Arrange
            var section = Section.UnitTestEmptySection();

            //Act
            var vm = new SectionViewSectionCardViewModel(MockContextProvider.Object, new TeamSectionAssignment
                (section, null, 1), null, _updateWorkflowCallback, _reorderSectionsCallback);

            //Assert
            vm.ShowTeam.Should().BeFalse();
        }

        [Fact]
        public void ViewModel_WithAssignment_ShowsTeam()
        {

            //Arrange
            var section = Section.UnitTestEmptySection();
            var team = new Team(1);
            var user = new RenderUser("User", Guid.Empty);
            var teamTranslatorUser = new TeamTranslatorUser(team, user);

            //Act
            var vm = new SectionViewSectionCardViewModel(MockContextProvider.Object, new TeamSectionAssignment
                (section, teamTranslatorUser, 1), teamTranslatorUser, _updateWorkflowCallback, _reorderSectionsCallback);

            //Assert
            vm.ShowTeam.Should().BeTrue();
        }

        [Fact]
        public void AddAssignment_ShowsTeam()
        {
            //Arrange
            var section = Section.UnitTestEmptySection();
            var team = new Team(1);
            var user = new RenderUser("User", Guid.Empty);
            var teamTranslatorUser = new TeamTranslatorUser(team, user);

            //Act
            var vm = new SectionViewSectionCardViewModel(MockContextProvider.Object, new TeamSectionAssignment
                (section, null, 1), null, _updateWorkflowCallback, _reorderSectionsCallback);
            var sectionViewTeamCard = new NewSectionViewTeamCardViewModel(MockContextProvider.Object, teamTranslatorUser);
            vm.AssignTeamToSectionCommand.Execute(sectionViewTeamCard).Subscribe(Stubs.ActionNop, Stubs.ExceptionNop);

            //Assert
            vm.ShowTeam.Should().BeTrue();
        }

        [Fact]
        public void RemoveAssignment_ShowsAddATeam()
        {
            //Arrange
            var section = Section.UnitTestEmptySection();
            var team = new Team(1);
            var user = new RenderUser("User", Guid.Empty);
            var teamTranslatorUser = new TeamTranslatorUser(team, user);

            //Act
            var vm = new SectionViewSectionCardViewModel(MockContextProvider.Object, new TeamSectionAssignment
                (section, teamTranslatorUser, 1), teamTranslatorUser, _updateWorkflowCallback, _reorderSectionsCallback);
            vm.RemoveTeamCommand.Execute().Subscribe(Stubs.ActionNop, Stubs.ExceptionNop);
            //Assert
            vm.ShowTeam.Should().BeFalse();
        }

        [Fact]
        public void AssignmentChanges_TeamAdded_UpdatesViewModel()
        {
            //Arrange
            var section = Section.UnitTestEmptySection();
            var team = new Team(1);
            var user = new RenderUser("User", Guid.Empty);
            var teamTranslatorUser = new TeamTranslatorUser(team, user);
            var teamSectionAssignment = new TeamSectionAssignment
                (section, null, 1);
            //Act
            var vm = new SectionViewSectionCardViewModel(MockContextProvider.Object, teamSectionAssignment, null,
                _updateWorkflowCallback, _reorderSectionsCallback);
            teamSectionAssignment.Team = teamTranslatorUser;

            //Assert
            vm.ShowTeam.Should().BeTrue();
        }

        [Fact]
        public void AssignmentChanges_TeamRemoved_UpdatesViewModel()
        {
            //Arrange
            var section = Section.UnitTestEmptySection();
            var team = new Team(1);
            var user = new RenderUser("User", Guid.Empty);
            var teamTranslatorUser = new TeamTranslatorUser(team, user);
            var sectionAssignment = new TeamSectionAssignment
                (section, teamTranslatorUser, 1);
            //Act
            var vm = new SectionViewSectionCardViewModel(MockContextProvider.Object, sectionAssignment, teamTranslatorUser,
                _updateWorkflowCallback, _reorderSectionsCallback);
            sectionAssignment.Team = null;

            //Assert
            vm.ShowTeam.Should().BeFalse();
        }
    }
}