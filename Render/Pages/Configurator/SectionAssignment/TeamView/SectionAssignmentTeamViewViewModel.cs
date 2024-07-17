using System.Collections.ObjectModel;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;
using Render.Kernel;

namespace Render.Pages.Configurator.SectionAssignment.TeamView
{
    public class SectionAssignmentTeamViewViewModel : ViewModelBase
    {
        private SourceList<TeamViewTeamCardViewModel> _teamSourceList =
            new SourceList<TeamViewTeamCardViewModel>();

        private ReadOnlyObservableCollection<TeamViewTeamCardViewModel> _teams;
        public ReadOnlyObservableCollection<TeamViewTeamCardViewModel> Teams => _teams;

        public TeamViewTeamAssignmentsViewModel TeamAssignmentsViewModel { get; private set; }

        public ReadOnlyObservableCollection<TeamViewSectionCardViewModel> SectionCards => _sectionAssignments;

        private SourceCache<TeamSectionAssignment, Guid> _sectionCardSourceCache = new SourceCache<TeamSectionAssignment, Guid>(x => x.Section.Id);
        private ReadOnlyObservableCollection<TeamViewSectionCardViewModel> _sectionAssignments;

        public SectionAssignmentTeamViewViewModel(IViewModelContextProvider viewModelContextProvider,
            List<TeamTranslatorUser> allTeams,
            List<TeamSectionAssignment> allAssignments, 
            Func<TeamSectionAssignment, Task> updateWorkflowCallback, 
            Func<TeamSectionAssignment, Task> reorderSectionsCallback,
            Guid? teamId = null)
            : base("SectionAssignmentTeamView", viewModelContextProvider)
        {
            TeamAssignmentsViewModel = new TeamViewTeamAssignmentsViewModel(viewModelContextProvider, allTeams.First(), allAssignments, updateWorkflowCallback, reorderSectionsCallback);
            var teamChangeList = _teamSourceList.Connect().Publish();
            Disposables.Add(teamChangeList
                .Bind(out _teams)
                .Subscribe());
            Disposables.Add(teamChangeList
                .WhenPropertyChanged(x => x.Selected)
                .Subscribe(vm =>
                {
                    if (!vm.Value) return;
                    
                    foreach (var team in _teams)
                    {
                        if (team.Team.Team.Id != vm.Sender.Team.Team.Id)
                        {
                            team.Deselect();
                        }
                    }
                    TeamAssignmentsViewModel?.UpdateSelectedTeam(vm.Sender, _teams.ToList());
                }));
            Disposables.Add(teamChangeList.Connect());
            
           
            
            var changeList = _sectionCardSourceCache.Connect().Publish();
            Disposables.Add(changeList
                .AutoRefresh(TimeSpan.FromMilliseconds(50))
                .Filter(x => x.Team == null)
                .Transform(x => new TeamViewSectionCardViewModel(viewModelContextProvider, x))
                .Sort(SortExpressionComparer<TeamViewSectionCardViewModel>
                    .Ascending(c => c.Section.Priority))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Bind(out _sectionAssignments)
                .Subscribe());
            Disposables.Add(changeList.WhenPropertyChanged(x => x.Team, false)
                .Subscribe(t =>
                {
                    if (t.Sender?.Team?.Team == null)
                    {
                        var team  = Teams.First(x => x.TeamAssignments.Any(s => s.Section.Id == t.Sender.Section.Id));
                        var assignment = team.TeamAssignments.First(x => x.Section.Id == t.Sender.Section.Id);
                        team.TeamAssignments.Remove(assignment);
                        if(TeamAssignmentsViewModel.TeamTranslatorUser.User.Id == team.Team.User.Id)
                            TeamAssignmentsViewModel?.UpdateSelectedTeam(team, _teams.ToList());
                    }
                    else
                    {
                        var team = Teams.FirstOrDefault(x => x.Team.Team.Id == t.Sender.Team.Team.Id);
                        if (team == null) return;
                        team.TeamAssignments.Add(t.Sender);
                        if(TeamAssignmentsViewModel.TeamTranslatorUser.User.Id == team.Team.User.Id)
                            TeamAssignmentsViewModel?.UpdateSelectedTeam(team, _teams.ToList());
                    }
                }));
            
            //This needs to be called after _teams source list is populated so that list of assigned sections in team assignment view
            //is updated properly on load.
            foreach (var team in allTeams)
            {
                var vm = new TeamViewTeamCardViewModel(viewModelContextProvider, team,
                    allAssignments.FindAll(x => x.Team == team));
                if ((_teamSourceList.Count == 0 && teamId == null) || teamId == team?.Team?.Id)
                {
                    vm.Select();
                    TeamAssignmentsViewModel?.UpdateSelectedTeam(vm, _teams.ToList());
                }
            
                _teamSourceList.Add(vm);
            }
            
            Disposables.Add(changeList.Connect());
            _sectionCardSourceCache.AddOrUpdate(allAssignments);
        }

        public override void Dispose()
        {
            TeamAssignmentsViewModel?.Dispose();
            TeamAssignmentsViewModel = null;

            _teamSourceList?.Dispose();
            _teamSourceList = null;

            _sectionCardSourceCache?.Dispose();
            _sectionCardSourceCache = null;

            _sectionAssignments = null;
            _teams = null;

            base.Dispose();
        }
    }
}