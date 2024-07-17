using System.Collections.ObjectModel;
using System.Reactive.Linq;
using CommunityToolkit.Maui.Core.Extensions;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;
using Render.Kernel;
using Render.Kernel.WrappersAndExtensions;

namespace Render.Pages.Configurator.SectionAssignment.SectionView
{
    public class SectionAssignmentSectionViewViewModel : ViewModelBase
    {
        public DynamicDataWrapper<NewSectionViewTeamCardViewModel> TeamCards =
            new DynamicDataWrapper<NewSectionViewTeamCardViewModel>();

        private SourceCache<SectionViewSectionCardViewModel, Guid> _sectionCardSource = new
            SourceCache<SectionViewSectionCardViewModel, Guid>(x => x.Assignment.Section.Id);

        private ReadOnlyObservableCollection<SectionViewSectionCardViewModel> _sectionCards;
        public ReadOnlyObservableCollection<SectionViewSectionCardViewModel> SectionCards => _sectionCards;

        public SectionAssignmentSectionViewViewModel(IViewModelContextProvider viewModelContextProvider,
            List<TeamTranslatorUser> allTeams,
            List<TeamSectionAssignment> allAssignments,
            Func<TeamSectionAssignment, Task> updateWorkflowCallback, 
            Func<TeamSectionAssignment, Task> reorderSectionsCallback)
            : base("SectionAssignmentSectionView", viewModelContextProvider)
        {
            Disposables.Add(
                _sectionCardSource.Connect()
                    .AutoRefresh(s => s.Assignment.Priority)
                    .Batch(TimeSpan.FromMilliseconds(50))
                    .Sort(SortExpressionComparer<SectionViewSectionCardViewModel>
                        .Ascending(x => x.Assignment.Priority))
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Bind(out _sectionCards)
                    .Subscribe());

            foreach (var team in allTeams)
            {
                TeamCards.Add(new NewSectionViewTeamCardViewModel(viewModelContextProvider, team));
            }

            foreach (var assignment in allAssignments)
            {
                var team = allTeams.FirstOrDefault(x => x.Team.SectionAssignments
                    .Any(s => s.SectionId == assignment.Section.Id));
                _sectionCardSource.AddOrUpdate(
                    new SectionViewSectionCardViewModel(viewModelContextProvider, assignment, team,
                            updateWorkflowCallback, reorderSectionsCallback));
            }
        }

        public override void Dispose()
        {
            TeamCards?.Dispose();
            TeamCards = null;

            _sectionCardSource.Dispose();
            _sectionCardSource = null;

            _sectionCards = null;

            base.Dispose();
        }
    }
}