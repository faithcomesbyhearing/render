using System.Collections.ObjectModel;
using ReactiveUI;
using Render.Pages.AppStart.Home;

namespace Render.Kernel.WrappersAndExtensions
{
    public static class RoutingExtensions
    {
        public static IRoutableViewModel GetPreviousScreen(this ObservableCollection<IRoutableViewModel> navigationStack)
            => navigationStack.Count > 1 ? navigationStack[navigationStack.Count - 2] : null;

        public static bool IsPreviousScreenHome(this ObservableCollection<IRoutableViewModel> navigationStack)
            => navigationStack.GetPreviousScreen()?.UrlPathSegment == "Home";

        public static HomeViewModel GetHomeScreen(this ObservableCollection<IRoutableViewModel> navigationStack) =>
            (HomeViewModel)navigationStack.LastOrDefault(x => x.UrlPathSegment == "Home");
    }
}