using System.Reactive;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Render.Kernel;
using Render.Pages.Configurator.SectionAssignment.Cards.Team;

namespace Render.Pages.Configurator.SectionAssignment.Cards.Section
{
    public class SectionCardViewModel : ViewModelBase
    {
        public required Guid SectionId { get; init; }

        public int Priority { get; set; }

        [Reactive]
        public string Title { get; init; }

        [Reactive]
        public int Number { get; init; }

        [Reactive]
        public string ScriptureReference { get; init; }

        [Reactive]
        public bool ShowTeam { get; private set; }

        [Reactive]
        public TeamCardViewModel AssignedTeamViewModel { get; private set; }

        [Reactive]
        public ReactiveCommand<TeamCardViewModel, Unit> AssignTeamCommand { get; private set; }

        [Reactive]
        public ReactiveCommand<Unit, Unit> RemoveTeamCommand { get; private set; }

        public SectionCardViewModel(IViewModelContextProvider viewModelContextProvider)
            : base(nameof(SectionCardViewModel), viewModelContextProvider) 
        {
            AssignTeamCommand = ReactiveCommand.Create<TeamCardViewModel>(AssignTeam);
            RemoveTeamCommand = ReactiveCommand.Create(RemoveTeam);
        }

        public void RemoveTeam()
        {
            AssignedTeamViewModel.RemoveSectionAssignement(this);

            AssignedTeamViewModel = null;
            ShowTeam = false;
        }

        public void AssignTeam(TeamCardViewModel team)
        {
            if (team is null)
            {
                return;
            }

            AssignedTeamViewModel = team;
            ShowTeam = true;

            team.AssignSection(this);
        }

        public override void Dispose()
        {
            AssignedTeamViewModel = null;

            AssignTeamCommand?.Dispose();
            AssignTeamCommand = null;

            RemoveTeamCommand?.Dispose();
            RemoveTeamCommand = null;

            base.Dispose();
        }
    }
}