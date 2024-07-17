using System.Reactive;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Render.Kernel;

namespace Render.Pages.Configurator.SectionAssignment.TeamView
{
    public class TeamViewSectionCardViewModel : ViewModelBase
    {
        public readonly TeamSectionAssignment Section;
        [Reactive] public bool IsSelected { get; private set; }
        [Reactive] public bool ShowCardOnModal { get; set; } = true;
        public ReactiveCommand<Unit,Unit> ToggleSelectCommand { get; }
        public TeamViewSectionCardViewModel(IViewModelContextProvider viewModelContextProvider,
            TeamSectionAssignment section)
            : base("TeamViewSectionCard", viewModelContextProvider)
        {
            Section = section;
            ToggleSelectCommand = ReactiveCommand.Create(ToggleIsSelected);
        }
        
        private void ToggleIsSelected()
        {
            IsSelected = !IsSelected;
        }
    }
}