using System.Reactive;
using System.Reactive.Linq;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Render.Kernel;

namespace Render.Pages.AppStart.Home.NavigationIcons;

public class NavigationIconViewModel : SectionNavigationViewModel
{
    [Reactive] public ReactiveCommand<Unit, IRoutableViewModel> NavigateToPageCommand { get; set; }
    public string IconImageGlyph { get; protected init; }
    public string IconName { get; protected init; }
    [Reactive] public ActionState ActionState { get; set; }
    [Reactive] public string Title { get; set; }

    public int StageNumber { get; protected init; }

    protected readonly IObservable<bool> CanExecute;

    public bool IsFirstIcon { get; set; }

    protected NavigationIconViewModel(IViewModelContextProvider viewModelContextProvider,
        string title)
        : base("NavigationIconViewModel", viewModelContextProvider)
    {
        Title = title;
        CanExecute = this.WhenAnyValue(x => x.ActionState).Select(x => x != ActionState.Inactive);
    }
}