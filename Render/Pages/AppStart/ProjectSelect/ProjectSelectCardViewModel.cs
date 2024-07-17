using System.Reactive;
using System.Reactive.Linq;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Render.Components.Modal;
using Render.Kernel;
using Render.Kernel.WrappersAndExtensions;
using Render.Models.LocalOnlyData;
using Render.Models.Project;
using Render.Pages.AppStart.Home;
using Render.Repositories.Kernel;
using Render.Repositories.LocalDataRepositories;
using Render.Resources;
using Render.Resources.Localization;
using Render.Services.SyncService;
using Render.TempFromVessel.Project;

namespace Render.Pages.AppStart.ProjectSelect
{
    public class ProjectSelectCardViewModel : ViewModelBase
    {
        private readonly IDataPersistence<Project> _projectRepository;
        private readonly ILocalProjectsRepository _localProjectsRepository;
        private readonly IUserMachineSettingsRepository _userMachineSettingsRepository;

        private readonly IModalService _modalService;
        private readonly IOffloadService _offloadService;
        private readonly ISyncService _syncService;
        private readonly ILocalSyncService _localSyncService;

        public string ProjectTitle { get; }

        [Reactive] public Project Project { get; private set; }
        [Reactive] public DownloadState DownloadState { get; private set; }
        [Reactive] public bool OffloadMode { get; set; }
        [Reactive] public bool CanBeOpened { get; private set; }

        public ReactiveCommand<Unit, IRoutableViewModel> NavigateToProjectCommand { get; }
        public ReactiveCommand<Unit, Unit> OpenOffloadWarningModalCommand { get; }


        public ProjectSelectCardViewModel(Project project, RenderProject renderProject,
            IViewModelContextProvider viewModelContextProvider, DownloadState projectDownloadState)
            : base("ProjectDownloadCard", viewModelContextProvider)
        {
            _projectRepository = viewModelContextProvider.GetPersistence<Project>();
            _localProjectsRepository = ViewModelContextProvider.GetLocalProjectsRepository();
            _userMachineSettingsRepository = viewModelContextProvider.GetUserMachineSettingsRepository();

            _modalService = viewModelContextProvider.GetModalService();
            _offloadService = viewModelContextProvider.GetOffloadService();
            _syncService = viewModelContextProvider.GetSyncService();
            _localSyncService = viewModelContextProvider.GetLocalSyncService();

            ProjectTitle = (renderProject == null || renderProject.RenderProjectLanguageId == default)
                ? $"{project.Name}"
                : $"{project.Name} - {renderProject.GetLanguageName()}";

            Project = project;
            DownloadState = projectDownloadState;

            Disposables.Add(this.WhenAnyValue(x => x.OffloadMode)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(offloadMode =>
                {
                    CanBeOpened = !offloadMode && !Project.IsDeleted && DownloadState == DownloadState.Finished;
                }));

            NavigateToProjectCommand = ReactiveCommand.CreateFromTask(NavigateToProject, this.WhenAnyValue(viewModel => viewModel.CanBeOpened));
            OpenOffloadWarningModalCommand = ReactiveCommand.CreateFromTask(OpenOffloadWarningModal);
        }

        private async Task<IRoutableViewModel> NavigateToProject()
        {
            if (!CanBeOpened) return null;

            //This is a stop gap - if we hit an exception when navigating home, just stay here instead of crashing
            try
            {
                await GetAndUpdateUserMachineSettings();
                await StartSync();
                var homeViewModel = await HomeViewModel.CreateAsync(Project.Id, ViewModelContextProvider);
                return await NavigateTo(homeViewModel);
            }
            catch (Exception e)
            {
                LogError(e);
                return null;
            }
        }

        private async Task GetAndUpdateUserMachineSettings()
        {
            var userMachineSettings = await _userMachineSettingsRepository.GetUserMachineSettingsForUserAsync(ViewModelContextProvider.GetLoggedInUser().Id);
            if (userMachineSettings.GetAndSetLastSelectedProject(Project.Id))
            {
                await _userMachineSettingsRepository.UpdateUserMachineSettingsAsync(userMachineSettings);
            }
        }

        private async Task OpenOffloadWarningModal()
        {
            var localProjects = await _localProjectsRepository.GetLocalProjectsForMachine();
            var lastSync = localProjects.GetProjects().SingleOrDefault(project => project.ProjectId == Project.Id)?.LastSyncDate ?? DateTime.MinValue;

            var projectSize = await _offloadService.GetOffloadProjectSize(Project.Id);
            var projectNamePlaceholder = !string.IsNullOrEmpty(projectSize)
                ? $"{Project.Name} ({projectSize})"
                : Project.Name;

            var modalViewModel = new ModalViewModel(
                ViewModelContextProvider,
                _modalService,
                Icon.OffloadItem,
                string.Format(AppResources.OffloadProject, Project.Name),
                string.Format(AppResources.OffloadWarning, projectNamePlaceholder, lastSync),
                new ModalButtonViewModel(AppResources.Cancel),
                new ModalButtonViewModel(AppResources.Confirm))
            {
                AfterConfirmCommand = ReactiveCommand.CreateFromTask(Offload)
            };

            await _modalService.ConfirmationModal(modalViewModel);
        }

        private async Task Offload()
        {
            DownloadState = DownloadState.Offloading;

            var localProject = await _localProjectsRepository.GetLocalProjectsForMachine();
            localProject.Offload(Project.Id);
            await _localProjectsRepository.SaveLocalProjectsForMachine(localProject);
            await _offloadService.OffloadProject(Project.ProjectId);

            DownloadState = DownloadState.NotStarted;
            OffloadMode = false;
        }

        private async Task StartSync()
        {
#if DEMO
            return;
#endif
            
            var projectIds = new List<Guid>();
            var globalUserIds = new List<Guid>();
            var localProjects = await _localProjectsRepository.GetLocalProjectsForMachine();

            foreach (var project in localProjects.GetDownloadedProjects())
            {
                projectIds.Add(project.ProjectId);

                var originProject = await _projectRepository.GetAsync(project.ProjectId);

                if (originProject != null)
                {
                    globalUserIds.AddRange(originProject.GlobalUserIds);
                }
            }
            
            var loggedInUser = ViewModelContextProvider.GetLoggedInUser();
            var selectedProject = await _projectRepository.GetAsync(Project.Id);
            if (!await ViewModelContextProvider.GetSyncGatewayApiWrapper().IsConnected())
            {
                if (selectedProject != null && loggedInUser is not null)
                {
                    _localSyncService.StopLocalSync();
                    _localSyncService.StartLocalSync(loggedInUser.Username, selectedProject.ProjectId);
                }
            }
            else
            {
                _syncService.StartAllSync(projectIds, globalUserIds, loggedInUser.Id.ToString(), loggedInUser.SyncGatewayLogin);    
            }
            
            // Update Project Statistics
            var statisticsPersistence = ViewModelContextProvider.GetPersistence<RenderProjectStatistics>();

            foreach (var projectId in projectIds)
            {
                var projectStatistics = (await statisticsPersistence.QueryOnFieldAsync("ProjectId", projectId.ToString(), 1, false)).FirstOrDefault();
                if (projectStatistics != null)
                {
                    projectStatistics.SetRenderProjectLastSyncDate(DateTimeOffset.Now);
                    await statisticsPersistence.UpsertAsync(projectStatistics.Id, projectStatistics);
                }
            }
        }
    }
}