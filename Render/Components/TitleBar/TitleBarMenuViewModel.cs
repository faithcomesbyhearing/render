using System.Collections.ObjectModel;
using System.Reactive;
using DynamicData;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Render.Components.TitleBar.MenuActions;
using Render.Kernel;
using Render.Kernel.WrappersAndExtensions;
using Render.Models.Sections;
using Render.Models.Workflow;
using Render.Models.Workflow.Stage;
using Render.Services.SyncService;

namespace Render.Components.TitleBar
{
    public class TitleBarMenuViewModel : ViewModelBase
    {
        private IMenuPopupService _menuPopupService;

        public string Username { get; }

        private ObservableCollection<IMenuActionViewModel> MenuItems = new ();

        public DynamicDataWrapper<IMenuActionViewModel> Actions { get; private set; } = new ();

        // maintain a list of menu item which will navigate away from the current page
        // it is used to display loading screen
        public DynamicDataWrapper<IMenuActionViewModel> NavigationItems { get; } = new ();

        public IMenuActionViewModel HomeViewModel { get; }
        public IMenuActionViewModel SectionStatusActionViewModel { get; }
        public IMenuActionViewModel LogOutViewModel { get; }
        public IMenuActionViewModel ProjectListMenuViewModel { get; }
        public SyncMenuActionViewModel SyncViewModel { get; }
        public SyncFromUsbActionViewModel SyncFromUsbViewModel { get; }
        public IMenuActionViewModel DividePassageViewModel { get; }

        public bool ShowUser { get; }
        public bool ShowActionItems { get; }
        public bool ShowProjectHome { get; }
        public bool ShowSectionStatus { get; }
        public bool ShowProjectList { get; }
        public bool ShowDividePassage { get; }
        private bool _closeOnSyncComplete;
        private string _pageName;

        [Reactive] public bool ReloadOnModalClose { get; set; }

        public ReactiveCommand<Unit, Unit> CloseCommand { get; }

        public TitleBarMenuViewModel(List<IMenuActionViewModel> menuActions,
            IViewModelContextProvider viewModelContextProvider, string urlPath, string pageName,
            Section section = null, PassageNumber passageNumber = null, Stage stage = null, Step step = null, bool isSegmentSelect = false) :
            base("TitleBarMenu", viewModelContextProvider)
        {
            _pageName = pageName;
            _menuPopupService = viewModelContextProvider.GetMenuPopupService();
            CloseCommand = ReactiveCommand.Create(_menuPopupService.Close);
            
            Actions.AddRange(menuActions);
            NavigationItems.AddRange(menuActions);
            MenuItems.AddRange(menuActions);

            ShowActionItems = menuActions.Count > 0;
            ShowProjectList = urlPath != "ProjectDownload" && urlPath != "ProjectSelect";

            if (ShowProjectList)
            {
                ProjectListMenuViewModel = new ProjectListMenuActionViewModel(viewModelContextProvider, pageName);
                NavigationItems.Add(ProjectListMenuViewModel);
                MenuItems.Add(ProjectListMenuViewModel);
            }

            // we will not divide a divided passage and user can only divide passage in draft step
            ShowDividePassage = step?.RenderStepType == RenderStepTypes.Draft && passageNumber?.DivisionNumber == 0 &&
                                urlPath == "Drafting";

            if (ShowDividePassage)
            {
                DividePassageViewModel = new DividePassageActionViewModel(ViewModelContextProvider, pageName, section, passageNumber, step);
                NavigationItems.Add(DividePassageViewModel);
            }

            var projectId = GetProjectId(); 
            ShowUser = viewModelContextProvider.GetLoggedInUser() != null;
            Username = ShowUser ? viewModelContextProvider.GetLoggedInUser().FullName : "";

            if (projectId != Guid.Empty && ShowProjectList && ShowUser)
            {
                ShowProjectHome = urlPath != "Home";
                ShowSectionStatus = urlPath != "SectionStatusPage";
            }
            
            if (ShowProjectHome)
            {
                HomeViewModel = new HomeActionViewModel(viewModelContextProvider, pageName);
                NavigationItems.Add(HomeViewModel);
            }

            if (ShowSectionStatus)
            {
                SectionStatusActionViewModel = new SectionStatusActionViewModel(viewModelContextProvider, pageName);
                NavigationItems.Add(SectionStatusActionViewModel);
            }

            SyncViewModel = new SyncMenuActionViewModel(viewModelContextProvider);

            SyncFromUsbViewModel = new SyncFromUsbActionViewModel(viewModelContextProvider);
            
            LogOutViewModel = new LogOutActionViewModel(viewModelContextProvider, pageName);
            NavigationItems.Add(LogOutViewModel);
            MenuItems.Add(LogOutViewModel);

            Disposables.Add(this.WhenAnyValue(x => x.SyncViewModel.CurrentSyncStatus)
                .Subscribe(CheckIfComplete));

            Disposables.Add(this.WhenAnyValue(x => x.LogOutViewModel.IsActionExecuting)
                .Subscribe(value => IsLoading = value));
        }

		private void CheckIfComplete(CurrentSyncStatus syncStatus)
		{
			// Set to close modal when sync finishes
			if (syncStatus == CurrentSyncStatus.ActiveReplication)
			{
				_closeOnSyncComplete = true;
			}

			// If sync has been started, and is now complete close the modal
			if (syncStatus == CurrentSyncStatus.Finished && _closeOnSyncComplete && SyncViewModel.IsManualSync)
			{
				CloseModal();
			}

			if (SyncViewModel?.SyncManager.IsWebSync == true)
			{
				LogOutViewModel.CanActionExecute = syncStatus != CurrentSyncStatus.ActiveReplication;
			}
		}

		private void CloseModal()
        {
            _menuPopupService.Close();
            // If the menu is closing because sync is completed, and we are on the home page, reload the page. 
            if (SyncViewModel.IsManualSync && _pageName == "Project Home")
            {
                SyncViewModel.IsManualSync = false;
                ReloadOnModalClose = true;
            }
            SyncViewModel.IsManualSync = false;
        }

        public void SetMenuActionsNavigationCondition(Func<Task<bool>> condition)
        {
            HomeViewModel?.SetCommandCondition(condition);
            SectionStatusActionViewModel?.SetCommandCondition(condition);
            LogOutViewModel?.SetCommandCondition(condition);
            ProjectListMenuViewModel?.SetCommandCondition(condition);

            foreach (var action in Actions.SourceItems)
            {
                action?.SetCommandCondition(condition);
            }
        }

        public override void Dispose()
        {
            HomeViewModel?.Dispose();
            SectionStatusActionViewModel?.Dispose();
            LogOutViewModel?.Dispose();
            SyncFromUsbViewModel?.Dispose();
            ProjectListMenuViewModel?.Dispose();
            SyncViewModel?.Dispose();
            DividePassageViewModel?.Dispose();
            Actions?.Dispose();
            base.Dispose();
        }
    }
}