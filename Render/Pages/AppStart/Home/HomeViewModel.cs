using System.Reactive;
using System.Reactive.Linq;
using DynamicData;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Render.Extensions;
using Render.Kernel;
using Render.Models.Sections;
using Render.Pages.AppStart.Home.NavigationPanels;
using Render.Pages.AppStart.ProjectSelect;
using Render.Repositories.LocalDataRepositories;
using Render.Resources;
using Render.Resources.Localization;
using Render.Services;
using Render.Services.SyncService;
using Render.TempFromVessel.Project;
using Render.TempFromVessel.User;

namespace Render.Pages.AppStart.Home
{
    public class HomeViewModel : PageViewModelBase
    {
        //public fields, starting with reactive objects
        [Reactive] public bool ShowAdminPanel { get; set; }
        [Reactive] public bool ShowNavigationPanelOptions { get; set; }
        [Reactive] public bool IsUserAdmin { get; set; }

        public readonly ReactiveCommand<Unit, Unit> OnSelectWorkflowViewCommand;
        public readonly ReactiveCommand<Unit, Unit> OnSelectAdminViewCommand;
        public readonly ReactiveCommand<Unit, IRoutableViewModel> NavigateToProjectSelectPageCommand;

        //private fields
        private bool _isInitialized;
        private bool _syncCompleted;
        private readonly Guid _projectId;
        private readonly IGrandCentralStation _grandCentralStation;
        private readonly IRenderChangeMonitoringService _renderChangeMonitoringService;
        private readonly ISyncService _syncService;

        [Reactive]
        public INavigationPane WorkflowNavigationPane { get; set; }
        public INavigationPane AdministrationNavigationPane { get; set; }
        private bool EnableSectionAssignment { get; set; }
        
        public static async Task<HomeViewModel> CreateAsync(Guid projectId, IViewModelContextProvider contextProvider)
        {
            var workflowRepository = contextProvider.GetWorkflowRepository();
            var workflow = await workflowRepository.GetWorkflowForProjectIdAsync(projectId);
            var sectionRepository = contextProvider.GetSectionRepository();
            var allSections = await sectionRepository.GetSectionsForProjectAsync(projectId);
            var teams = workflow.GetTeams();
            var enableSectionAssignment = teams.All(team => team.TranslatorId != Guid.Empty && allSections.Count != 0);

            var grandCentralStation = contextProvider.GetGrandCentralStation();
            var loggedInUserId = contextProvider.GetLoggedInUser()?.Id ?? Guid.Empty;
            await grandCentralStation.FindWorkForUser(projectId, loggedInUserId);
            
            var projectRepository = contextProvider.GetPersistence<Project>();
            var project = await projectRepository.GetAsync(projectId);

            var viewModel = new HomeViewModel(projectId, contextProvider, enableSectionAssignment,
                project?.Name ?? "Home");
            await viewModel.LoadSectionStateData();
            await viewModel.SessionStateService
                .LoadUserProjectSessionAsync(loggedInUserId, projectId);
            var machineStateRepo = contextProvider.GetMachineLoginStateRepository();
            var machineState = await machineStateRepo.GetMachineLoginState();
            machineState.SetProjectLogin(projectId);
            await machineStateRepo.SaveMachineLoginState(machineState);
            return viewModel;
        }

        private HomeViewModel(Guid projectId, IViewModelContextProvider viewModelContextProvider,
            bool enableSectionAssignment, string pageName) :
            base("Home", viewModelContextProvider, AppResources.ProjectHome, secondPageName: pageName)
        {
            var pageLoadSync = true;
            DisposeOnNavigationCleared = true;
            TitleBarViewModel.DisposeOnNavigationCleared = true;
            _syncService = ViewModelContextProvider.GetSyncService();
            var user = viewModelContextProvider.GetLoggedInUser();
            if (user?.HasClaim(
                    RenderRolesAndClaims.ProjectUserClaimType,
                    projectId.ToString(),
                    RenderRolesAndClaims.GetRoleByName(RoleName.Configure).Id) is true)
            {
                IsUserAdmin = true;
            }

            EnableSectionAssignment = enableSectionAssignment;
            TitleBarViewModel.PageGlyph =
                ((FontImageSource)ResourceExtensions.GetResourceValue("HomeIcon") ?? new FontImageSource()).Glyph;
            _projectId = projectId;
            _grandCentralStation = viewModelContextProvider.GetGrandCentralStation();
            _renderChangeMonitoringService = viewModelContextProvider.GetRenderChangeMonitoringService();

            if (_grandCentralStation.FullWorkflowStatusList != null)
            {
                foreach (var workflowStatus in _grandCentralStation.FullWorkflowStatusList)
                {
                    _renderChangeMonitoringService.MonitorDocumentByField<WorkflowStatus>("WorkflowStatusId",
                        workflowStatus.Id.ToString(), ReloadOnWorkflowStatusChange);
                }
            }

            NavigateToProjectSelectPageCommand = ReactiveCommand.CreateFromTask(NavigateToProjectSelectPage);
            Disposables.Add(NavigateToProjectSelectPageCommand.IsExecuting
                .Subscribe(isExecuting => { IsLoading = isExecuting; }));
            OnSelectWorkflowViewCommand = ReactiveCommand.Create(OnSelectWorkflowView);
            OnSelectAdminViewCommand = ReactiveCommand.Create(OnSelectAdminView);

            Disposables.Add(TitleBarViewModel.NavigationItems.Observable
                .MergeMany(item => item.IsExecuting)
                .Subscribe(isExecuting => IsLoading = isExecuting));
            Disposables.Add(TitleBarViewModel.NavigationItems.Observable
                .MergeMany(item => item.IsExecuting)
                .Subscribe(isExecuting => IsLoading = isExecuting));
            Disposables.Add(this.WhenAnyValue(v => v._syncService.CurrentSyncStatus)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ =>
                {
                    _syncCompleted = _syncService.CurrentSyncStatus == CurrentSyncStatus.Finished;
                    if (_syncCompleted && !pageLoadSync)
                    {
                        ReloadOnWorkflowStatusChange();
                    }
                    pageLoadSync = false;
                }));
        }

        private async Task LoadSectionStateData()
        {
            try
            {
                if(IsUserAdmin)
                {
                    AdministrationNavigationPane =
                        new AdministrationNavigationPaneViewModel(ViewModelContextProvider, _projectId, EnableSectionAssignment);
                    
                    Disposables.Add(AdministrationNavigationPane.NavigationIcons
                        .Observable
                        .MergeMany(item => item.NavigateToPageCommand.IsExecuting)
                        .Subscribe(isExecuting => IsLoading = isExecuting));
				}

                ShowNavigationPanelOptions = AdministrationNavigationPane != null;

                WorkflowNavigationPane = await WorkflowNavigationPaneViewModel.CreateAsync(ViewModelContextProvider);
                var pane = (WorkflowNavigationPaneViewModel)WorkflowNavigationPane;
				if (!ShowAdminPanel)
                {
                    ShowAdminPanel = pane?.HasWorkAssigned == false && ShowNavigationPanelOptions;
                }

                Disposables.Add(WorkflowNavigationPane.NavigationIcons
                    .Observable
                    .MergeMany(item => item.NavigateToPageCommand.IsExecuting)
                    .Subscribe(isExecuting => IsLoading = isExecuting));
            }
            catch (Exception e)
            {
                LogError(e);
                throw;
            }

        }

        private async Task ReloadIcons()
        {
            //Need this to prevent calling LoadSectionStateData() twice when HomeScreen is loaded
            if (!_isInitialized && !_syncCompleted)
            {
                _isInitialized = true;
                return;
            }

            var loggedInUser = ViewModelContextProvider.GetLoggedInUser();
            if (loggedInUser == null)
            {
                return;
            }

            var workflowRepository = ViewModelContextProvider.GetWorkflowRepository();
            var workflow = await workflowRepository.GetWorkflowForProjectIdAsync(_projectId);
            var sectionRepository = ViewModelContextProvider.GetSectionRepository();
            var allSections = await sectionRepository.GetSectionsForProjectAsync(_projectId);
            var teams = workflow.GetTeams();
            EnableSectionAssignment = teams.All(team => team.TranslatorId != Guid.Empty && allSections.Count != 0);

            //If we are syncing when logging out, once the sync completes
            //it will hit this code with no user logged in so we cut it short
            await _grandCentralStation.FindWorkForUser(_projectId, loggedInUser.Id);
            await LoadSectionStateData();
            _syncCompleted = false;
        }

        private void ReloadOnWorkflowStatusChange()
        {
            try
            {
                MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await ReloadIcons();
					var pane = (WorkflowNavigationPaneViewModel)WorkflowNavigationPane;
					ShowAdminPanel = ShowNavigationPanelOptions && pane?.HasWorkAssigned is false;
                });
            }
            catch (Exception e)
            {
                LogError(e);
                throw;
            }
        }

        public void ResetDocumentListeners()
        {            
            foreach (var workflowStatus in _grandCentralStation.FullWorkflowStatusList)
            {
                _renderChangeMonitoringService.StopMonitoringDocumentByField<WorkflowStatus>("WorkflowStatusId", workflowStatus.Id.ToString());
                _renderChangeMonitoringService.MonitorDocumentByField<WorkflowStatus>("WorkflowStatusId", workflowStatus.Id.ToString(), ReloadOnWorkflowStatusChange);
            }
        }


        private void OnSelectWorkflowView()
        {
            ShowAdminPanel = false;
        }

        private void OnSelectAdminView()
        {
            ShowAdminPanel = true;
        }

        private async Task<IRoutableViewModel> NavigateToProjectSelectPage()
        {
            try
            {
                var projectSelectViewModel = await Task.Run(async () => await ProjectSelectViewModel.CreateAsync(ViewModelContextProvider));
                return await NavigateTo(projectSelectViewModel);
            }
            catch (Exception e)
            {
                LogError(e);
                throw;
            }
        }
        public void RemoveWorkflowMonitors()
        {
            if (_grandCentralStation.FullWorkflowStatusList == null)
            {
                return;
            }
            
            foreach (var workflowStatus in _grandCentralStation.FullWorkflowStatusList)
            {
                _renderChangeMonitoringService.StopMonitoringDocumentByField<WorkflowStatus>("WorkflowStatusId", workflowStatus.Id.ToString());
            }
        }

        public static void ClearNavigationStack()
        {
            Application.Current?.MainPage?.Navigation?.NavigationStack?
                .Where(page => page is IDisposable)
                .Cast<IDisposable>()
                .ForEach(disposable => disposable.Dispose());
        }

        public override void Dispose()
        {
            RemoveWorkflowMonitors();
            _renderChangeMonitoringService?.Dispose();
            AdministrationNavigationPane?.Dispose();
            OnSelectAdminViewCommand?.Dispose();
            OnSelectWorkflowViewCommand?.Dispose();
            NavigateToProjectSelectPageCommand?.Dispose();
            WorkflowNavigationPane?.Dispose();

            base.Dispose();
        }
    }
}