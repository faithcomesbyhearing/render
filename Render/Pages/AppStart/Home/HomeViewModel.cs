using System.Reactive;
using DynamicData;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Render.Kernel;
using Render.Pages.AppStart.Home.NavigationPanels;
using Render.Pages.AppStart.ProjectSelect;
using Render.Resources;
using Render.Resources.Localization;
using Render.Services.EntityChangeListenerServices;
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
        
        private readonly Guid _projectId;
        public IEntityChangeListenerService EntityChangeListenerService { get; }

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
            var enableSectionAssignment = allSections.Count != 0 && teams.Any(team => team.TranslatorId != Guid.Empty);

            var grandCentralStation = contextProvider.GetGrandCentralStation();
            var loggedInUserId = contextProvider.GetLoggedInUser()?.Id ?? Guid.Empty;
            var projectIdChanged = grandCentralStation.CurrentProjectId != projectId;
            await grandCentralStation.FindWorkForUser(projectId, loggedInUserId);
            
            var projectRepository = contextProvider.GetPersistence<Project>();
            var project = await projectRepository.GetAsync(projectId);

            await AddUserChangeListener(contextProvider, projectIdChanged, project, projectId);
            
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

        private static async Task AddUserChangeListener(IViewModelContextProvider contextProvider, bool projectIdChanged,
            Project project, Guid projectId)
        {
            if (projectIdChanged)
            {
                contextProvider.UserChangeListenerService?.Dispose();
                var users = await contextProvider.GetUserRepository().GetUsersForProjectAsync(project);
                contextProvider.UserChangeListenerService =
                    contextProvider.GetUserChangeListenerService(users?.Select(p => p.Id).ToList());
                var deletedUserCleanService = contextProvider.GetDeletedUserCleanService(projectId);
                contextProvider.UserChangeListenerService?.InitializeListener(deletedUserCleanService.Clean);
            }
        }

        private HomeViewModel(Guid projectId, IViewModelContextProvider viewModelContextProvider,
            bool enableSectionAssignment, string pageName) :
            base("Home", viewModelContextProvider, AppResources.ProjectHome, secondPageName: pageName)
        {
            EntityChangeListenerService = ViewModelContextProvider.GetDocumentSubscriptionManagerService();
            EntityChangeListenerService.InitializeListener(ReloadOnWorkflowStatusChange);
            
            var user = viewModelContextProvider.GetLoggedInUser();
            if (user?.HasClaim(
                    RenderRolesAndClaims.ProjectUserClaimType,
                    projectId.ToString(),
                    RoleName.Configure.GetRoleId()) is true)
            {
                IsUserAdmin = true;
            }

            EnableSectionAssignment = enableSectionAssignment;
            TitleBarViewModel.PageGlyph =
                ((FontImageSource)ResourceExtensions.GetResourceValue("HomeIcon") ?? new FontImageSource()).Glyph;
            _projectId = projectId;
            
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
            //it will hit this code with no user logged in, so we cut it short
            await ViewModelContextProvider.GetGrandCentralStation().FindWorkForUser(_projectId, loggedInUser.Id);
            await LoadSectionStateData();
        }

        private async Task ReloadOnWorkflowStatusChange(Guid id)
        {
            try
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
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

        public override void Dispose()
        {
            AdministrationNavigationPane?.Dispose();
            OnSelectAdminViewCommand?.Dispose();
            OnSelectWorkflowViewCommand?.Dispose();
            NavigateToProjectSelectPageCommand?.Dispose();
            WorkflowNavigationPane?.Dispose();
            EntityChangeListenerService?.Dispose();
            
            base.Dispose();
        }
    }
}