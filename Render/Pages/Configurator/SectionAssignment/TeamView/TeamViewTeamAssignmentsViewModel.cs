using System.Collections.ObjectModel;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Render.Kernel;
using Render.Resources.Localization;

namespace Render.Pages.Configurator.SectionAssignment.TeamView
{
    public class TeamViewTeamAssignmentsViewModel : ViewModelBase
    {
        [Reactive]
        public TeamTranslatorUser TeamTranslatorUser { get; private set; }
        
        [Reactive]
        public string UserName { get; private set; }
        
        [Reactive]
        public string AssignmentCount { get; private set; }

        private Func<TeamSectionAssignment, Task> _updateWorkflowCallback;
        private Func<TeamSectionAssignment, Task> _reorderSectionsCallback;
        
        private SourceCache<TeamViewSectionAssignmentCardViewModel, Guid> _cacheAssignmentSourceList = 
            new (x => x.Assignment.Section?.Id ?? Guid.Empty);
        private ReadOnlyObservableCollection<TeamViewSectionAssignmentCardViewModel> _cacheSectionAssignments;
        public ReadOnlyObservableCollection<TeamViewSectionAssignmentCardViewModel> CacheSectionAssignments => _cacheSectionAssignments;
        
        private SourceList<TeamViewSectionAssignmentCardViewModel> _assignmentSourceList =
            new SourceList<TeamViewSectionAssignmentCardViewModel>();
        private ReadOnlyObservableCollection<TeamViewSectionAssignmentCardViewModel> _sectionAssignments;
        public ReadOnlyObservableCollection<TeamViewSectionAssignmentCardViewModel> SectionAssignments => _sectionAssignments;

        private readonly List<TeamSectionAssignment> _teamSectionAssignments;
        public TeamViewTeamAssignmentsViewModel(IViewModelContextProvider viewModelContextProvider,
            TeamTranslatorUser team, List<TeamSectionAssignment> teamSectionAssignments,
            Func<TeamSectionAssignment, Task> updateWorkflowCallback, Func<TeamSectionAssignment, Task> reorderSectionsCallback) 
            : base("TeamViewTeamAssignments", viewModelContextProvider)
        {
            _updateWorkflowCallback = updateWorkflowCallback;
            _reorderSectionsCallback = reorderSectionsCallback;
            _teamSectionAssignments = teamSectionAssignments;
            TeamTranslatorUser = team;
            
            var changeList = _assignmentSourceList.Connect().Publish();
            Disposables.Add(changeList
                .Bind(out _sectionAssignments)
                .Subscribe());
            
            Disposables.Add(changeList.Connect());

            Disposables.Add(this.WhenAnyValue(x => x.TeamTranslatorUser)
                .Subscribe(u =>
                {
                    UserName = u.User.FullName;
                    AssignmentCount = string.Format(AppResources.Assigned, u.Team.SectionAssignmentCount);
                }));
            Disposables.Add(this.WhenAnyValue(x => x.TeamTranslatorUser.Team.SectionAssignmentCount)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(i =>
                {
                    AssignmentCount = string.Format(AppResources.Assigned, i);
                }));
            
            var sectionAssignmentChangeList = _cacheAssignmentSourceList.Connect().Publish();
            Disposables.Add(sectionAssignmentChangeList
                .AutoRefresh(s => s.Assignment.Priority, TimeSpan.FromMilliseconds(50))
                .Sort(SortExpressionComparer<TeamViewSectionAssignmentCardViewModel>
                    .Ascending(x => x.Assignment.Priority))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Bind(out _cacheSectionAssignments)
                .Subscribe());
            Disposables.Add(changeList
                .AutoRefresh(s => s.Assignment.Priority, TimeSpan.FromMilliseconds(50))
                .Sort(SortExpressionComparer<TeamViewSectionAssignmentCardViewModel>
                    .Ascending(x => x.Assignment.Priority))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Bind(out _sectionAssignments)
                .Subscribe());
            Disposables.Add(sectionAssignmentChangeList.Connect());
        }
        
        public void UpdateSelectedTeam(TeamViewTeamCardViewModel viewModel, List<TeamViewTeamCardViewModel>teams)
        {
            TeamTranslatorUser = viewModel.Team;

            _cacheAssignmentSourceList.Edit(cacheAssignmentSourceList => {
                foreach (var assignment in viewModel.TeamAssignments.ToList())
                {
                    var vm = new TeamViewSectionAssignmentCardViewModel(ViewModelContextProvider, assignment.Team,
                        _teamSectionAssignments,
                        _updateWorkflowCallback, _reorderSectionsCallback, assignment);
                    cacheAssignmentSourceList.AddOrUpdate(vm);
                }

                var invalidAssignments = _cacheAssignmentSourceList.Items
                    .Where(x => x.Assignment.Team != viewModel.Team && x.Section != null)
                    .Select(x => x.Assignment.Section?.Id ?? Guid.Empty)
                    .ToList();
                cacheAssignmentSourceList.RemoveKeys(invalidAssignments);
            });
            
            PopulateAssignmentList(teams);
        }
        
        private void PopulateAssignmentList(List<TeamViewTeamCardViewModel>teams)
        {
            _assignmentSourceList.Edit(assignmentSourceList => {
                assignmentSourceList.Clear();
                var assignedSections = teams.SelectMany(x => x.TeamAssignments.Select(y => y.Section)).ToList();
                foreach (var teamSectionAssignment in _teamSectionAssignments)
                {
                    var assignmentTeam = _cacheAssignmentSourceList.Items.SingleOrDefault(x => x.Assignment == teamSectionAssignment);
                    if (assignmentTeam != null)
                    {
                        var vm = new TeamViewSectionAssignmentCardViewModel(ViewModelContextProvider, assignmentTeam.Assignment.Team,
                            _teamSectionAssignments,
                            _updateWorkflowCallback, _reorderSectionsCallback, assignmentTeam.Assignment);
                        assignmentSourceList.Add(vm);
                    }
                    else
                    {
                        if (assignedSections.Any(x => x == teamSectionAssignment.Section))
                        {
                            continue;
                        }
                        var blank = new TeamViewSectionAssignmentCardViewModel(ViewModelContextProvider, TeamTranslatorUser,
                            _teamSectionAssignments,
                            _updateWorkflowCallback, _reorderSectionsCallback);
                        assignmentSourceList.Add(blank);
                    }
                }
            });
        }

        public override void Dispose()
        {
            _updateWorkflowCallback = null;
            _reorderSectionsCallback = null;

            _cacheAssignmentSourceList?.Dispose();
            _cacheAssignmentSourceList = null;

            _assignmentSourceList?.Dispose();
            _assignmentSourceList = null;
            _sectionAssignments = null;

            base.Dispose();
        }
    }
}