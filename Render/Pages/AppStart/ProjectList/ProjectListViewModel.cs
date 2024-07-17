using System.Collections.ObjectModel;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Render.Kernel;
using Render.Models.LocalOnlyData;
using Render.Models.Project;
using Render.Models.Users;
using Render.Pages.AppStart.Login;
using Render.Pages.AppStart.ProjectSelect;
using Render.Pages.AppStart.SplashScreen;
using Render.Repositories.Kernel;
using Render.Repositories.LocalDataRepositories;
using Render.Repositories.UserRepositories;
using Render.Resources.Localization;
using Render.Services.UserServices;
using Render.TempFromVessel.Project;

namespace Render.Pages.AppStart.ProjectList
{
    public class ProjectListViewModel : PageViewModelBase
    {
        private readonly IUser _loggedInUser;
        private readonly IDataPersistence<Project> _projectRepository;
        private readonly IDataPersistence<RenderProject> _renderProjectRepository;
        private readonly IMachineLoginStateRepository _loginStateRepository;
        private readonly IUserRepository _userRepository;
        private readonly IUserMembershipService _userMembershipService;
        private readonly ILocalProjectsRepository _localProjectsRepository;

        private readonly SourceCache<ProjectSelectCardViewModel, Guid> _projectListSourceCache = new
            SourceCache<ProjectSelectCardViewModel, Guid>(x => x.Project.Id);

        private readonly ReadOnlyObservableCollection<ProjectSelectCardViewModel> _projectList;

        public ReadOnlyObservableCollection<ProjectSelectCardViewModel> ProjectList => _projectList;

        [Reactive] public bool IsScreenSelected { get; set; }

        [Reactive] public bool? HasProjects { get; private set; }

        [Reactive] public bool OffloadMode { get; set; }
        
        [Reactive] public bool OnOffloaded { get; set; }

        public static async Task<ProjectListViewModel> CreateAsync(IViewModelContextProvider contextProvider)
        {
            var viewModel = new ProjectListViewModel(contextProvider);
            await viewModel.GetAllProjectsForUser();
            return viewModel;
        }

        /// <summary>
        /// This constructor is private. Always use the static CreateAsync() method to get this viewmodel, so that the
        /// async initialization of the viewmodel can occur before you navigate.
        /// </summary>
        private ProjectListViewModel(IViewModelContextProvider viewModelContextProvider)
            : base("ProjectList", viewModelContextProvider, AppResources.SelectAProject)
        {
            _projectRepository = viewModelContextProvider.GetPersistence<Project>();
            _renderProjectRepository = viewModelContextProvider.GetPersistence<RenderProject>();
            _userMembershipService = viewModelContextProvider.GetUserMembershipService();
            _localProjectsRepository = viewModelContextProvider.GetLocalProjectsRepository();
            _loggedInUser = viewModelContextProvider.GetLoggedInUser();
            _loginStateRepository = viewModelContextProvider.GetMachineLoginStateRepository();
            _userRepository = viewModelContextProvider.GetUserRepository();

            Disposables.Add(_projectListSourceCache.Connect()
                .Sort(SortExpressionComparer<ProjectSelectCardViewModel>.Ascending(x => x.Project.Name))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Bind(out _projectList)
                .Subscribe());
            Disposables.Add(_projectListSourceCache.Connect()
                .WhenPropertyChanged(x => x.DownloadState)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Select(x => x.Sender)
                .Subscribe(async vm =>
                {
                    if (vm.DownloadState != DownloadState.NotStarted) return;
                    
                    OnOffloaded = true;
                    
                    await Task.Run(async () => //This is needed to reset work for user and update the title bar and action menu
                    {
                        if (await IsOffloadedSelectedProject(vm.Project.Id))
                        {
                            var grandCentralStation = ViewModelContextProvider.GetGrandCentralStation();
                            grandCentralStation.ResetWorkForUser();
                            var projectSelect = await ProjectSelectViewModel.CreateAsync(viewModelContextProvider);
                            await NavigateToAndReset(projectSelect);
                        }
                    });

                    _projectListSourceCache.Remove(vm);

                    if (_projectListSourceCache.Count != 0) return;
                    await Task.Run(async () =>
                    {
                        await CleanDataAndLogout();
                    });
                            
                    HasProjects = vm.OffloadMode ? (bool?)null : false;
                }));

            Disposables.Add(this.WhenAnyValue(x => x.OffloadMode)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(isOffload =>
                {
                    SetProjectListStatus(isOffload);

                    foreach (ProjectSelectCardViewModel card in ProjectList)
                    {
                        card.OffloadMode = isOffload;
                    }
                }));

            Disposables.Add(this.WhenAnyValue(x => x.IsScreenSelected)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(b =>
                {
                    if (!b) return;
                    OffloadMode = false;
                    OnOffloaded = false;
                    Task.Run(async () => await GetAllProjectsForUser());
                }));
        }

        public async Task GetAllProjectsForUser(LocalProject downloadedProject = null)
        {
            bool HideProject (DownloadState state)
            {
                return state == DownloadState.NotStarted 
                       || state == DownloadState.Canceling
                       || state == DownloadState.FinishedPartially;
            }
            
            var localProjectsData = await _localProjectsRepository.GetLocalProjectsForMachine();
            var listOfProjects = localProjectsData.GetProjects();
            
            // Set matching local project state to be finished.
            if (downloadedProject != null)
            {
                var matchingProject = listOfProjects.First(x => x.ProjectId == downloadedProject.ProjectId);
                matchingProject.State = downloadedProject.State;
            }

            if (_projectListSourceCache.Count > 0)
            {
                foreach (var localProject in listOfProjects)
                {
                    if (HideProject(localProject.State))
                    {
                        _projectListSourceCache.Remove(localProject.ProjectId);
                    }
                }
            }

            foreach (var localProject in listOfProjects)
            {
                if (HideProject(localProject.State)) continue;
                
                var project = await _projectRepository.GetAsync(localProject.ProjectId);
                if (project == null && localProject.State != DownloadState.Downloading)
                {
                    return;
                }

                var renderProject = await _renderProjectRepository.QueryOnFieldAsync("ProjectId", localProject.ProjectId.ToString());
                if (renderProject == null && localProject.State != DownloadState.Downloading) continue;
                if (!project.IsBetaTester) continue;

                if (_loggedInUser.UserType == UserType.Render)
                {
                    if (((RenderUser)_loggedInUser).ProjectId == localProject.ProjectId)
                    {
                        _projectListSourceCache.AddOrUpdate(new ProjectSelectCardViewModel(project, renderProject,
                            ViewModelContextProvider, localProject.State));
                    }
                }
                else if (_userMembershipService.HasExplicitPermissionForProject(_loggedInUser, localProject.ProjectId))
                {
                    _projectListSourceCache.AddOrUpdate(new ProjectSelectCardViewModel(project, renderProject,
                        ViewModelContextProvider, localProject.State));
                }
                
            }


            foreach (var item in _projectListSourceCache.Items)
            {
                item.OffloadMode = OffloadMode;
            }

            Disposables.Add(_projectListSourceCache.Items
                .AsObservableChangeSet()
                .MergeMany(item => item.NavigateToProjectCommand.IsExecuting)
                .Subscribe(isExecuting =>
                {
                    IsLoading = isExecuting;
                }));
            HasProjects = _projectListSourceCache.Count > 0;

        }

        private void SetProjectListStatus(bool offload)
        {
            switch (offload)
            {
                case true when HasProjects is false:
                    HasProjects = null;
                    break;
                case false when HasProjects is false:
                    HasProjects = false;
                    break;
                case false when HasProjects is null:
                    HasProjects = false;
                    break;
            }
        }

        private async Task<bool> IsOffloadedSelectedProject(Guid projectId)
        {
            var userMachineSettingsRepository =
                ViewModelContextProvider.GetUserMachineSettingsRepository();
            var machineStateSettingsRepository = await 
                userMachineSettingsRepository.GetUserMachineSettingsForUserAsync(ViewModelContextProvider
                    .GetLoggedInUser().Id);
            return machineStateSettingsRepository.GetLastSelectedProjectId() == projectId;
        }
        
         private async Task CleanDataAndLogout()
        {
            var localProjectsData = await _localProjectsRepository.GetLocalProjectsForMachine();

            var hasProjectOnDevice = localProjectsData.GetProjects().Any(x => x.State == DownloadState.Finished);
            
            if (ViewModelContextProvider.GetLoggedInUser().UserType == UserType.Render && hasProjectOnDevice)
            {
                LogInfo("LogOut Render user after Offload project", new Dictionary<string, string>
                {
                    { "Username", ViewModelContextProvider.GetLoggedInUser().Username }
                });

                ViewModelContextProvider.ClearLoggedInUser();
                var loginViewModel = await LoginViewModel.CreateAsync(ViewModelContextProvider);
                await HostScreen.Router.NavigateAndReset.Execute(loginViewModel);
                
                return;
            }

            if (!hasProjectOnDevice)
            {
                var machineState = await _loginStateRepository.GetMachineLoginState();
                var top4Ids = machineState.GetTopFourUserIds().ToList();
                var users = await _userRepository.GetAllUsersAsync();
                foreach (var userId in top4Ids)
                {
                    var user = users.FirstOrDefault(x => x.Id == userId);
                    if (user != null)
                    {
                        machineState.RemoveUser(userId);
                    }
                }
                machineState.SetProjectLogin(Guid.Empty);
                await _loginStateRepository.SaveMachineLoginState(machineState);
                
                localProjectsData.RemoveNotStartedProjectsFromMachine();
                await _localProjectsRepository.SaveUpdates(localProjectsData);

                LogInfo("LogOut user after Offload last project on device", new Dictionary<string, string>
                {
                    { "Username", ViewModelContextProvider.GetLoggedInUser().Username }
                });
                
                ViewModelContextProvider.GetLocalSyncService().StopLocalSync();
                ViewModelContextProvider.ClearLoggedInUser();
                var splashCScreen = new SplashScreenViewModel(ViewModelContextProvider);
                await HostScreen.Router.NavigateAndReset.Execute(splashCScreen);   
            }
        }
    }
}