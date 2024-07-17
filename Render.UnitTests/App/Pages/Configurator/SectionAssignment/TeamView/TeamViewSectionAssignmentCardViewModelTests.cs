using FluentAssertions;
using Render.Kernel;
using Render.Models.Sections;
using Render.Models.Users;
using Render.Models.Workflow;
using Render.Pages.Configurator.SectionAssignment;
using Render.Pages.Configurator.SectionAssignment.TeamView;
using Render.UnitTests.App.Kernel;

namespace Render.UnitTests.App.Pages.Configurator.SectionAssignment.TeamView
{
    public class TeamViewSectionAssignmentCardViewModelTests : ViewModelTestBase
    {
        private readonly Func<TeamSectionAssignment, Task> _updateWorkflowCallback;
        private readonly Func<TeamSectionAssignment, Task> _reorderSectionsCallback;

        public TeamViewSectionAssignmentCardViewModelTests()
        {
            _updateWorkflowCallback = _ => Task.CompletedTask;
            _reorderSectionsCallback = _ => Task.CompletedTask;
        }

        [Fact]
        public void NoAssignment_DoesNotShowSection()
        {
            //Arrange
            var team = new Team(1);
            var user = new RenderUser("User", Guid.Empty);
            var teamTranslatorUser = new TeamTranslatorUser(team, user);
            //Act
            var vm = new TeamViewSectionAssignmentCardViewModel(MockContextProvider.Object, teamTranslatorUser, null,
                _updateWorkflowCallback, _reorderSectionsCallback);

            //Assert
            vm.ShowSection.Should().BeFalse();
        }

        [Fact]
        public void Assignment_ShowsSection()
        {
            //Arrange
            var section = Section.UnitTestEmptySection();
            var team = new Team(1);
            var user = new RenderUser("User", Guid.Empty);
            var teamTranslatorUser = new TeamTranslatorUser(team, user);
            var sectionAssignment = new TeamSectionAssignment
                (section, null, 1);
            //Act
            var vm = new TeamViewSectionAssignmentCardViewModel(MockContextProvider.Object, teamTranslatorUser, null,
                _updateWorkflowCallback, _reorderSectionsCallback, sectionAssignment);

            //Assert
            vm.ShowSection.Should().BeTrue();
        }

        [Fact]
        public void AddSection_Succeeds()
        {
            //Arrange
            var section = Section.UnitTestEmptySection();
            var team = new Team(1);
            var user = new RenderUser("User", Guid.Empty);
            var teamTranslatorUser = new TeamTranslatorUser(team, user);
            var sectionAssignment = new TeamSectionAssignment
                (section, null, 1);
            //Act
            var vm = new TeamViewSectionAssignmentCardViewModel(MockContextProvider.Object, teamTranslatorUser, null,
                _updateWorkflowCallback, _reorderSectionsCallback);
            vm.AssignSectionCommand.Execute(sectionAssignment).Subscribe(Stubs.ActionNop, Stubs.ExceptionNop);

            //Assert
            vm.ShowSection.Should().BeTrue();
        }

        [Fact]
        public void RemoveAssignment_Succeeds()
        {
            //Arrange
            var section = Section.UnitTestEmptySection();
            var team = new Team(1);
            var user = new RenderUser("User", Guid.Empty);
            var teamTranslatorUser = new TeamTranslatorUser(team, user);
            var sectionAssignment = new TeamSectionAssignment
                (section, null, 1);
            //Act
            var vm = new TeamViewSectionAssignmentCardViewModel(MockContextProvider.Object, teamTranslatorUser, null,
                _updateWorkflowCallback, _reorderSectionsCallback, sectionAssignment);
            vm.RemoveAssignmentCommand.Execute().Subscribe();
            //Assert
            vm.ShowSection.Should().BeFalse();
        }
    }
}