using FluentAssertions;
using Render.Models.Users;
using Render.Models.Workflow;
using Render.Pages.Configurator.SectionAssignment;
using Render.Pages.Configurator.SectionAssignment.TeamView;
using Render.Resources.Localization;
using Render.UnitTests.App.Kernel;
using Section = Render.Models.Sections.Section;

namespace Render.UnitTests.App.Pages.Configurator.SectionAssignment.TeamView
{
    public class TeamViewTeamAssignmentsViewModelTests : ViewModelTestBase
    {
        private readonly Func<TeamSectionAssignment, Task> _updateWorkflowCallback;
        private readonly Func<TeamSectionAssignment, Task> _reorderSectionsCallback;

        public TeamViewTeamAssignmentsViewModelTests()
        {
            _updateWorkflowCallback = _ => Task.CompletedTask;
            _reorderSectionsCallback = _ => Task.CompletedTask;
        }

        [Fact]
        public void Creation_TeamWithAssignments_Succeeds()
        {
            //Arrange
            var user = new RenderUser("User", Guid.Empty);
            var team = new Team(1);
            team.AddSectionAssignment(Guid.Empty, 1);
            var teamTranslatorUser = new TeamTranslatorUser(team, user);

            //Act
            var vm = new TeamViewTeamAssignmentsViewModel(MockContextProvider.Object, teamTranslatorUser, null
                , _updateWorkflowCallback, _reorderSectionsCallback);

            // Assert
            vm.AssignmentCount.Should().Be(string.Format(AppResources.Assigned, 1));
        }

        [Fact]
        public async void UpdateSelectedTeam_Succeeds()
        {
            //Arrange
            var user1 = new RenderUser("User", Guid.Empty);
            var team1 = new Team(1);
            team1.AddSectionAssignment(Guid.Empty, 1);
            var teamTranslatorUser = new TeamTranslatorUser(team1, user1);
            var section = Section.UnitTestEmptySection();
            var user2 = new RenderUser("User2", Guid.Empty);
            var team2 = new Team(2);
            team2.AddSectionAssignment(Guid.NewGuid(), 2);
            var teamTranslatorUser2 = new TeamTranslatorUser(team2, user2);
            var teamAssignments = new List<TeamSectionAssignment>
            {
                new (section, teamTranslatorUser2, 2),
                new (Section.UnitTestEmptySection(), teamTranslatorUser, 1)
            };
            var teams = new TeamViewTeamCardViewModel(MockContextProvider.Object, teamTranslatorUser, teamAssignments);

            //Act
            var vm = new TeamViewTeamAssignmentsViewModel(MockContextProvider.Object, teamTranslatorUser, teamAssignments,
                _updateWorkflowCallback, _reorderSectionsCallback);
            vm.UpdateSelectedTeam(new TeamViewTeamCardViewModel(MockContextProvider.Object, teamTranslatorUser2, teamAssignments), new List<TeamViewTeamCardViewModel>() { teams });
            await Task.Delay(50);

            // Assert
            vm.AssignmentCount.Should().Be(string.Format(AppResources.Assigned, 1));
            vm.UserName.Should().Be("User2");
            vm.CacheSectionAssignments.First().Section.Should().Be(section);
            vm.CacheSectionAssignments.First().Assignment.Team.Team.Id.Should().Be(team2.Id);
            vm.SectionAssignments.Count.Should().Be(1);
            vm.CacheSectionAssignments.Count.Should().Be(1);
        }
    }
}