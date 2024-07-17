using System.Reactive.Linq;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Render.Kernel;
using Render.Resources.Localization;

namespace Render.Pages.Configurator.SectionAssignment.SectionView
{
    public class SectionViewTeamCardViewModel : ViewModelBase
    {
        public string Name { get; set; }
        [Reactive]
        public int Count { get; set; }
        [Reactive]
        public string CountString { get; set;  }
        public TeamTranslatorUser Team { get; }
        public ImageSource UserIconSource { get; set; }

        public SectionViewTeamCardViewModel(IViewModelContextProvider viewModelContextProvider, TeamTranslatorUser team) 
            : base("SectionViewTeamCard", viewModelContextProvider)
        {
            Name = team.User.FullName;
            if (team.User.UserIcon.Length > 0)
            {
                var stream = new MemoryStream(team.User.UserIcon);
                UserIconSource = ImageSource.FromStream(() => stream);
            }
            Count = team.Team.SectionAssignmentCount;
            CountString = string.Format(AppResources.Assigned, Count);
            Team = team;
            Disposables.Add(team.WhenAnyValue(x => x.Team.SectionAssignmentCount)
                .Subscribe(s => Count = s));
            Disposables.Add(this.WhenAnyValue(x => x.Count)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(c => CountString = string.Format(AppResources.Assigned, c)));
        }

    }
}