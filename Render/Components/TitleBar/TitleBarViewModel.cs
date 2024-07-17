using System.Reactive;
using System.Reactive.Linq;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Render.Components.SectionTitlePlayer;
using Render.Kernel;
using Render.Kernel.WrappersAndExtensions;
using Render.Models.Audio;
using Render.Pages.AppStart.ProjectSelect;
using Render.Resources;
using Render.Resources.Localization;
using Render.Services;
using Render.Pages.Settings.ManageUsers;
using Render.Services.AudioPlugins.AudioRecorder.Interfaces;

namespace Render.Components.TitleBar
{
    public enum TitleBarElements
    {
        RenderLogo,
        SettingsButton,
        BackButton,
        PageTitle,
        SectionPlayer
    }

    public class TitleBarViewModel : ViewModelBase, ITitleBarViewModel
    {
        private IMenuPopupService _menuPopupService;

        private IGrandCentralStation GrandCentralStation { get; }
        public ReactiveCommand<Unit, Unit> ShowMenuCommand { get; private set; }

        // maintain a list of menu item which will navigate away from the current page
        // it is used to display loading screen
        public DynamicDataWrapper<ReactiveCommand<Unit, IRoutableViewModel>> NavigationItems { get; } =
            new DynamicDataWrapper<ReactiveCommand<Unit, IRoutableViewModel>>();

        public string PageTitle { get; }
        public string SecondaryPageTitle { get; }

        public bool ShowSinglePageTitle { get; }
        public bool ShowDualPageTitle { get; }
        public bool ShowPageTitleIcon { get; }
        public bool ShowLogo { get; private set; }
        public bool ShowSettings { get; private set; }
        public bool ShowSectionPlayer { get; private set; }
        
        [Reactive]
        public bool IsEnabled { get; set; }

        [Reactive]
        public string PageGlyph { get; set; }

        [Reactive]
        public bool ShowBackButton { get; private set; }

        [Reactive]
        public TitleBarMenuViewModel TitleBarMenuViewModel { get; set; }

        [Reactive]
        public ISectionTitlePlayerViewModel SectionTitlePlayerViewModel { get; private set; }

        public ReactiveCommand<Unit, IRoutableViewModel> NavigateHomeCommand { get; private set; }
        public ReactiveCommand<Unit, IRoutableViewModel> NavigateToUserSettingsCommand { get; private set; }

        public TitleBarViewModel(List<TitleBarElements> elementsToActivate,
            TitleBarMenuViewModel titleBarMenuViewModel,
            IViewModelContextProvider viewModelContextProvider,
            string pageTitle,
            Audio sectionTitleAudio,
            int sectionNumber,
            string secondaryPageTitle = "",
            string passageNumber = "") :
            base("TitleBar",
            viewModelContextProvider)
        {
            _menuPopupService = viewModelContextProvider.GetMenuPopupService();
            
            IsEnabled = true;
            PageTitle = pageTitle == AppResources.SegmentBackTranslateSelect ? AppResources.SegmentBackTranslate : pageTitle;
            SecondaryPageTitle = secondaryPageTitle;
            GrandCentralStation = ViewModelContextProvider.GetGrandCentralStation();
            var canExecuteNavigateHome = this.WhenAnyValue(x => x.PageTitle,
                    x => x.GrandCentralStation.CurrentProjectId)
                .Select(args => !string.Equals(args.Item1, "Home", StringComparison.InvariantCultureIgnoreCase)
                                && args.Item2 != Guid.Empty);
            NavigateHomeCommand = ReactiveCommand.CreateFromTask(FinishCurrentStackAndNavigateHome, canExecuteNavigateHome);
            NavigateToUserSettingsCommand = ReactiveCommand.CreateFromTask(NavigateToUserSettingsPage);
            NavigationItems.Add(NavigateHomeCommand);
            NavigationItems.Add(NavigateToUserSettingsCommand);
            NavigateBackCommand = ReactiveCommand.CreateFromTask(NavigateBackOrToProjectSelectAsync);
            TitleBarMenuViewModel = titleBarMenuViewModel;
            ShowMenuCommand = ReactiveCommand.Create(() => _menuPopupService.Show(TitleBarMenuViewModel));

            foreach (var element in elementsToActivate)
            {
                switch (element)
                {
                    case TitleBarElements.RenderLogo:
                        ShowLogo = true;
                        break;
                    case TitleBarElements.SettingsButton:
                        ShowSettings = true;
                        break;
                    case TitleBarElements.BackButton:
                        ShowBackButton = true;
                        break;
                    case TitleBarElements.PageTitle:
                        ShowSinglePageTitle = string.IsNullOrEmpty(SecondaryPageTitle);
                        ShowDualPageTitle = !string.IsNullOrEmpty(SecondaryPageTitle);
                        break;
                    case TitleBarElements.SectionPlayer:
                        ShowSectionPlayer = true;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            ShowPageTitleIcon = ShowSinglePageTitle || ShowDualPageTitle;

            SectionTitlePlayerViewModel = sectionTitleAudio == null
                ? new SectionTitlePlayerViewModel(viewModelContextProvider, sectionNumber, passageNumber)
                : new SectionTitlePlayerViewModel(sectionTitleAudio, viewModelContextProvider, sectionNumber, passageNumber);

            PageGlyph = ((FontImageSource)ResourceExtensions.GetResourceValue("PlaceholderWhite"))?.Glyph;

            AddRecordingSubscription(pageTitle);
        }

        /// <summary>
        /// This code is awful workaround for the BUG 26272.
        /// We have issues with page disposing, therefore remove all subscriptions when we are on the Home page.
        /// Will be fixed in the scope of the PBI 26566.
        /// </summary>
        private void AddRecordingSubscription(string pageTitle)
        {
            if (pageTitle == AppResources.ProjectHome)
            {
                IAudioRecorder.IsRecordingChanged = null;
            }

            IAudioRecorder.IsRecordingChanged += RecordingChanged;
        }

        private void RecordingChanged(bool isRecording)
        {
            IsEnabled = !isRecording;
        }

        public void ToggleShowBackButton()
        {
            ShowBackButton = !ShowBackButton;
        }

        public void SetNavigationCondition(Func<Task<bool>> allowNavigationExecution)
        {
            NavigateHomeCommand = NavigateHomeCommand?.AddCondition(allowNavigationExecution);
            NavigateBackCommand = NavigateBackCommand?.AddCondition(allowNavigationExecution);

            TitleBarMenuViewModel?.SetMenuActionsNavigationCondition(allowNavigationExecution);
        }

        /// <summary>
        /// For the title bar, we need to do some dependent navigating back. If we're on home, go to the project select page.
        /// Otherwise, go back to the previous page.
        /// </summary>
        private async Task<IRoutableViewModel> NavigateBackOrToProjectSelectAsync()
        {
            try
            {
                if (HostScreen.Router.NavigationStack.Last().UrlPathSegment != "Home") return await NavigateBack();
                var projectSelectViewModel = await ProjectSelectViewModel.CreateAsync(ViewModelContextProvider);
                return await NavigateToAndReset(projectSelectViewModel);

            }
            catch (Exception e)
            {
                LogError(e);
                throw;
            }
        }

        private async Task<IRoutableViewModel> NavigateToUserSettingsPage()
        {
            var projectId = ViewModelContextProvider.GetGrandCentralStation().CurrentProjectId;
            var userSettings = await UserSettingsViewModel.CreateAsync(ViewModelContextProvider, projectId, false);
            return await NavigateTo(userSettings);
        }

        public override void Dispose()
        {
            IAudioRecorder.IsRecordingChanged -= RecordingChanged;
            
            SectionTitlePlayerViewModel?.Dispose();
            SectionTitlePlayerViewModel = null;

            ShowMenuCommand?.Dispose();
            ShowMenuCommand = null;

            NavigateHomeCommand?.Dispose();
            NavigateHomeCommand = null;

            NavigateToUserSettingsCommand?.Dispose();
            NavigateToUserSettingsCommand = null;

            TitleBarMenuViewModel?.Dispose();
            TitleBarMenuViewModel = null;

            base.Dispose();
        }
    }
}