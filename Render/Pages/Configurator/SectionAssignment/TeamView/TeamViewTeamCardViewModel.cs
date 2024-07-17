using System.Reactive;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Render.Kernel;
using Render.Resources.Localization;

namespace Render.Pages.Configurator.SectionAssignment.TeamView
{
    public class TeamViewTeamCardViewModel : ViewModelBase
    {
        public readonly TeamTranslatorUser Team;
        [Reactive] public bool Selected { get; private set; }
        [Reactive] public int Count { get; private set; }
        [Reactive] public string CountString { get; private set; }
        public List<TeamSectionAssignment> TeamAssignments { get; }

        public readonly ReactiveCommand<Unit, Unit> SelectCommand;
        public string TeamName { get; }

        public TeamViewTeamCardViewModel(IViewModelContextProvider viewModelContextProvider,
            TeamTranslatorUser team, List<TeamSectionAssignment> teamAssignments)
        :base("TeamViewTeamCard", viewModelContextProvider)
        {
            Team = team;
            TeamName = string.Format(AppResources.TeamTitle, team.Team.TeamNumber);
            TeamAssignments = teamAssignments;
            CountString = Count.ToString();
            SelectCommand = ReactiveCommand.Create(Select);
            Count = team.Team.SectionAssignmentCount;
            Team = team;
            Disposables.Add(team.WhenAnyValue(x => x.Team.SectionAssignmentCount)
                .Subscribe(s =>
                {
                    Count = s;
                    CountString = s.ToString();
                }));
        }
        
        public void Select()
        {
            Selected = true;
        }

        public void Deselect()
        {
            Selected = false;
        }
    }
}