using System.Reactive.Linq;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Render.Kernel;
using Render.Resources.Localization;

namespace Render.Pages.Configurator.SectionAssignment.SectionView
{
    public class NewSectionViewTeamCardViewModel : ViewModelBase
    {
        public string Name { get; set; }
        [Reactive]
        public int Count { get; set; }
        
        [Reactive]
        public string CountString { get; set;  }
        public TeamTranslatorUser Team { get; }

        public NewSectionViewTeamCardViewModel(IViewModelContextProvider viewModelContextProvider, TeamTranslatorUser team) 
            : base("SectionViewTeamCard", viewModelContextProvider)
        {
            Name = string.Format(AppResources.TeamTitle, team.Team.TeamNumber);
            Count = team.Team.SectionAssignmentCount;
            CountString = Count.ToString();
            Team = team;
            Disposables.Add(team.WhenAnyValue(x => x.Team.SectionAssignmentCount)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(s =>
                {
                    Count = s;
                    CountString = Count.ToString();
                }));
        }

    }
}