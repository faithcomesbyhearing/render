using System.Reactive;
using ReactiveUI;
using Render.Components.SectionTitlePlayer;
using Render.Kernel;
using Render.Kernel.WrappersAndExtensions;

namespace Render.Components.TitleBar
{
    public interface ITitleBarViewModel : IViewModelBase
    {
        string PageTitle { get; }
        string PageGlyph { get; set; }
        string SecondaryPageTitle { get; }

        bool ShowSinglePageTitle { get; }
        bool ShowDualPageTitle { get; }
        bool ShowPageTitleIcon { get; }
        bool ShowLogo { get; }
        bool ShowSettings { get; }
        bool ShowSectionPlayer { get; }
        bool ShowBackButton { get; }
        bool DisposeOnNavigationCleared { get; set; }
        bool IsEnabled { get; set; }

        DynamicDataWrapper<ReactiveCommand<Unit, IRoutableViewModel>> NavigationItems { get; }
        TitleBarMenuViewModel TitleBarMenuViewModel { get; set;  }

        ISectionTitlePlayerViewModel SectionTitlePlayerViewModel { get; }

        ReactiveCommand<Unit, Unit> ShowMenuCommand { get; }
        ReactiveCommand<Unit, IRoutableViewModel> NavigateBackCommand { get; set; }
        ReactiveCommand<Unit,IRoutableViewModel> NavigateHomeCommand { get; }
        ReactiveCommand<Unit, IRoutableViewModel> NavigateToUserSettingsCommand { get; }

        void ToggleShowBackButton();
        void SetNavigationCondition(Func<Task<bool>> allowNavigationExecution);
    }
}