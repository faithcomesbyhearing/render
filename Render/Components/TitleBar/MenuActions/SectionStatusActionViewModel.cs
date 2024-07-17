using System.Reactive.Linq;
using ReactiveUI;
using Render.Kernel;
using Render.Resources;
using Render.Resources.Localization;
using Render.Resources.Styles;
using SectionStatusPageViewModel = Render.Pages.Settings.SectionStatus.SectionStatusPageViewModel;

namespace Render.Components.TitleBar.MenuActions
{
    public class SectionStatusActionViewModel : MenuActionViewModel
    {
        public SectionStatusActionViewModel(IViewModelContextProvider viewModelContextProvider, string pageName)
            : base("HomeAction", viewModelContextProvider, pageName)
        {
            var color = ((ColorReference)ResourceExtensions.GetResourceValue("Option")) ?? new ColorReference();
            var imageSource = IconExtensions.BuildFontImageSource(Icon.Sections, color.Color, 40);
            var title = AppResources.SectionStatus;
            var command = ReactiveCommand.CreateFromTask(NavigateToAsync);
            SetSources(imageSource, command, title);
        }

        private async Task<IRoutableViewModel> NavigateToAsync()
        {
            CloseMenu();

            var projectId = ViewModelContextProvider.GetGrandCentralStation().CurrentProjectId;
            var loggedInUserId = ViewModelContextProvider.GetLoggedInUser().Id;

            await ViewModelContextProvider.GetGrandCentralStation().FindWorkForUser(projectId, loggedInUserId);

            var sectionStatusViewModel = await Task.Run(async () =>
                await SectionStatusPageViewModel.CreateAsync(ViewModelContextProvider, projectId));
            
            return await HostScreen.Router.Navigate.Execute(sectionStatusViewModel);
        }
    }
}