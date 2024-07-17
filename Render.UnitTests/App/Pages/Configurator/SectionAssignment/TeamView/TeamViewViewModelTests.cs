using FluentAssertions;
using Render.Models.Sections;
using Render.Models.Users;
using Render.Models.Workflow;
using Render.Pages.Configurator.SectionAssignment;
using Render.Pages.Configurator.SectionAssignment.TeamView;
using Render.UnitTests.App.Kernel;

namespace Render.UnitTests.App.Pages.Configurator.SectionAssignment.TeamView
{
    public class TeamViewViewModelTests : ViewModelTestBase
    {
        private readonly List<TeamTranslatorUser> _allTeams;
        private readonly List<TeamSectionAssignment> _allAssignments;
        private readonly TeamTranslatorUser _teamTranslatorUser1;
        private readonly Func<TeamSectionAssignment, Task> _updateWorkflowCallback;
        private readonly Func<TeamSectionAssignment, Task> _reorderSectionsCallback;

        public TeamViewViewModelTests()
        {
            _allTeams = new List<TeamTranslatorUser>();
            _allAssignments = new List<TeamSectionAssignment>();
            var user1 = new RenderUser("User1", Guid.Empty);
            var team1 = new Team(1);
            _teamTranslatorUser1 = new TeamTranslatorUser(team1, user1);
            var user2 = new RenderUser("User1", Guid.Empty);
            var team2 = new Team(2);
            var teamTranslatorUser2 = new TeamTranslatorUser(team2, user2);
            _allTeams.Add(_teamTranslatorUser1);
            _allTeams.Add(teamTranslatorUser2);
            var section1 = Section.UnitTestEmptySection();
            var section2 = Section.UnitTestEmptySection();
            var assignment1 = new TeamSectionAssignment(section1, null, 1);
            var assignment2 = new TeamSectionAssignment(section2, null, 2);
            _allAssignments.Add(assignment1);
            _allAssignments.Add(assignment2);

            _updateWorkflowCallback = _ => Task.CompletedTask;
            _reorderSectionsCallback = _ => Task.CompletedTask;
        }

        [Fact]
        public async void Creation_WithTwoTeams_Succeeds()
        {
            //Arrange
            
            //Act
            var vm = new SectionAssignmentTeamViewViewModel(MockContextProvider.Object, _allTeams, _allAssignments, _updateWorkflowCallback, _reorderSectionsCallback);
            await Task.Delay(50);

            //Assert
            vm.Teams.Count.Should().Be(2);
            vm.SectionCards.Count.Should().Be(2);
        }

        /// [Fact]
        /// <summary>
        /// TODO: Refactor test to avoid async execution. 
        /// Async execution breaks AzureDevOps builds from time to time.
        /// </summary>
        public async void UpdateSectionAssignment_AddTeam_RemovesFromSectionList()
        {
            //Arrange

            //Act
            var vm = new SectionAssignmentTeamViewViewModel(MockContextProvider.Object, _allTeams, _allAssignments, _updateWorkflowCallback, _reorderSectionsCallback);
            vm.SectionCards.First().Section.Team = _teamTranslatorUser1;
            await Task.Delay(200);
            //Assert
            vm.SectionCards.Count.Should().Be(1);
        }


        [Fact]
        public async void UpdateSectionAssignment_RemoveTeam_AddsSectionToList()
        {
            //Arrange
            _allAssignments.First().Team = _teamTranslatorUser1;
            //Act
            var vm = new SectionAssignmentTeamViewViewModel(MockContextProvider.Object, _allTeams, _allAssignments, _updateWorkflowCallback, _reorderSectionsCallback);
            _allAssignments.First().Team = null;
            await Task.Delay(200);

            //Assert
            vm.SectionCards.Count.Should().Be(2);
        }

        [Fact]
        public async void UpdateSectionAssignment_AddTeam_AddsSectionToTeam()
        {
            //Arrange

            //Act
            var vm = new SectionAssignmentTeamViewViewModel(MockContextProvider.Object, _allTeams, _allAssignments, _updateWorkflowCallback, _reorderSectionsCallback);
            vm.SectionCards.First().Section.Team = _teamTranslatorUser1;
            await Task.Delay(50);

            //Assert
            vm.Teams.First().TeamAssignments.Count.Should().Be(1);
        }

        [Fact]
        public void UpdateSectionAssignment_RemoveTeam_RemovesTeam()
        {
            //Arrange
            _allAssignments.First().Team = _teamTranslatorUser1;
            //Act
            var vm = new SectionAssignmentTeamViewViewModel(MockContextProvider.Object, _allTeams, _allAssignments, _updateWorkflowCallback, _reorderSectionsCallback);
            _allAssignments.First().Team = null;

            //Assert
            vm.Teams.First().TeamAssignments.Count.Should().Be(0);
        }
    }
}