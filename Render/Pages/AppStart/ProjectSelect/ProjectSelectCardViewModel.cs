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
using Render.Services.SyncService.DbFolder;
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
        private readonly ISyncManager _syncManager;
        private readonly ILocalDatabaseReplicationManager _localDatabaseReplicationManager;

        public string ProjectTitle { get; }

        [Reactive] public Project Project { get; private set; }
        [Reactive] public DownloadState DownloadState { get; private set; }
        [Reactive] public bool OffloadMode { get; set; }
        [Reactive] public bool CanBeOpened { get; private set; }
        [Reactive] public bool CanBeExported { get; set; } = true;

        public ReactiveCommand<Unit, IRoutableViewModel> NavigateToProjectCommand { get; }
        public ReactiveCommand<Unit, Unit> OpenOffloadWarningModalCommand { get; }
        public ReactiveCommand<Unit, Unit> ExportCommand { get; }

        public ProjectSelectCardViewModel(Project project, RenderProject renderProject,
            IViewModelContextProvider viewModelContextProvider, DownloadState projectDownloadState)
            : base("ProjectDownloadCard", viewModelContextProvider)
        {
            _projectRepository = viewModelContextProvider.GetPersistence<Project>();
            _localProjectsRepository = viewModelContextProvider.GetLocalProjectsRepository();
            _userMachineSettingsRepository = viewModelContextProvider.GetUserMachineSettingsRepository();
            _localDatabaseReplicationManager = viewModelContextProvider.GetLocalDatabaseReplicationManager();

            _modalService = viewModelContextProvider.GetModalService();
            _offloadService = viewModelContextProvider.GetOffloadService();
            _syncManager = viewModelContextProvider.GetSyncManager();

            ProjectTitle = (renderProject == null || renderProject.RenderProjectLanguageId == default)
                ? $"{project.Name}"
                : $"{project.Name} - {renderProject.GetLanguageName()}";

            Project = project;
            DownloadState = projectDownloadState;

            Disposables.Add(this.WhenAnyValue(x => x.OffloadMode)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(offloadMode => { CanBeOpened = !offloadMode && !Project.IsDeleted && DownloadState == DownloadState.Finished; }));

            NavigateToProjectCommand = ReactiveCommand.CreateFromTask(NavigateToProject,
                this.WhenAnyValue(viewModel => viewModel.CanBeOpened));

            NavigateToProjectCommand = ReactiveCommand.CreateFromTask(NavigateToProject,
                this.WhenAnyValue(viewModel => viewModel.CanBeOpened));
            OpenOffloadWarningModalCommand = ReactiveCommand.CreateFromTask(OpenOffloadWarningModal);
            
            ExportCommand = ReactiveCommand.CreateFromTask(ExportProject, canExecute: this.WhenAnyValue(viewModel => viewModel.CanBeExported));
            
            Disposables.Add(this.WhenAnyValue(x => x._localDatabaseReplicationManager.Status)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(async status => await OnExportCompletedAsync(status)));
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
                return await NavigateToAndReset(homeViewModel);
            }
            catch (Exception e)
            {
                LogError(e);
                return null;
            }
        }

        private async Task GetAndUpdateUserMachineSettings()
        {
            var userMachineSettings =
                await _userMachineSettingsRepository.GetUserMachineSettingsForUserAsync(ViewModelContextProvider
                    .GetLoggedInUser().Id);
            if (userMachineSettings.GetAndSetLastSelectedProject(Project.Id))
            {
                await _userMachineSettingsRepository.UpdateUserMachineSettingsAsync(userMachineSettings);
            }
        }

        private async Task OpenOffloadWarningModal()
        {
            var localProjects = await _localProjectsRepository.GetLocalProjectsForMachine();
            var lastSync =
                localProjects.GetProjects().SingleOrDefault(project => project.ProjectId == Project.Id)?.LastSyncDate ??
                DateTime.MinValue;

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

        private async Task ExportProject()
        {
            var dbFolder = await ViewModelContextProvider?.GetDownloadService()?.ChooseFolderAsync()!;
            if (dbFolder is null) return;

            var directoryPath = @$"{dbFolder.Path}\Project_{Project.ProjectId}";

            if (_localDatabaseReplicationManager.ReplicationWillStartInAnotherProjectFolder(directoryPath))
            {
                await _modalService.ShowInfoModal(Icon.PopUpWarning,
                    AppResources.StorageAlreadyContains,
                    AppResources.PleaseSelectAnotherStorage);
                return;
            }
            
            await _localDatabaseReplicationManager.StartExportAsync(directoryPath, Project);
        }

        private async Task OnExportCompletedAsync(ReplicationStatus status)
        {
            switch (status)
            {
                case ReplicationStatus.Completed:
                    DownloadState = DownloadState.ExportDone;
                    break;
                case ReplicationStatus.Error:
                    DownloadState = DownloadState.FinishedPartially;
                    await _modalService.ShowInfoModal(
                        Icon.PopUpWarning,
                        AppResources.SyncPathIsNotAvailable,
                        AppResources.ChooseAnotherStorage);
                    break;
                case ReplicationStatus.Replication:
                    DownloadState = DownloadState = DownloadState.Exporting;
                    break;
                case ReplicationStatus.NotStarted:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(status), status, null);
            }
        }

        private async Task Offload()
        {
            DownloadState = DownloadState.Offloading;

            var localProject = await _localProjectsRepository.GetLocalProjectsForMachine();
            localProject.Offload(Project.Id);
            await _localProjectsRepository.SaveLocalProjectsForMachine(localProject);
            _syncManager.StopLocalSync();
            await _offloadService.OffloadProject(Project.ProjectId);

            DownloadState = DownloadState.NotStarted;
            OffloadMode = false;
        }

        private async Task StartSync()
        {
#if DEMO
            return;
#endif
            var selectedProject = await _projectRepository.GetAsync(Project.Id);
            var loggedInUser = ViewModelContextProvider.GetLoggedInUser();

            if (selectedProject != null && loggedInUser != null)
            {
                await _syncManager.StartSync(selectedProject.ProjectId, loggedInUser, loggedInUser.SyncGatewayLogin, needToResetLocalSync: true);
            }
        }
    }
}