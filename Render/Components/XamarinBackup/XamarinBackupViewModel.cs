using System.Reactive;
using System.Reactive.Linq;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Render.Components.AddProject;
using Render.Kernel;
using Render.Kernel.WrappersAndExtensions;
using Render.Pages.AppStart.Login;
using Render.Resources;
using Render.Resources.Localization;
using Render.Services.SyncService;
using Render.Services.SyncService.DbFolder;

namespace Render.Components.XamarinBackup;

public class XamarinBackupViewModel : ViewModelBase, IAddProjectViewModel
{
    private readonly IOffloadService _offloadService;
    private readonly IModalService _modalManager;
    private readonly IDbBackupService _backupService;
    private readonly IAudioIntegrityService _audioIntegrityService;

    public List<Guid> ProjectIds { get; private set; }

    [Reactive] private ILocalDatabaseReplicationManager LocalDatabaseReplicationManager { get; set; }
    [Reactive] public bool ShowProgressView { get; private set; }
    [Reactive] public AddProjectState AddProjectState { get; set; } = AddProjectState.None;

    public ReactiveCommand<Unit, Unit> CancelOnDownloadingCommand { get; private set; }
    public ReactiveCommand<Unit, Unit> RetryOnErrorCommand { get; private set; }
    public ReactiveCommand<Unit, Unit> CancelOnErrorCommand { get; private set; }
    public ReactiveCommand<Unit, IRoutableViewModel> NavigateOnCompletedCommand { get; }
    public string NavigateOnCompletedButtonText { get; }

    public string DownloadErrorText { get; } = AppResources.ErrorAddingProject;

    public XamarinBackupViewModel(
        IViewModelContextProvider viewModelContextProvider,
        string navigateOnCompletedButtonText,
        Func<Task<IRoutableViewModel>> navigateOnCompletedCommand)
        : base("XamarinBackupViewModel", viewModelContextProvider)
    {
        NavigateOnCompletedButtonText = navigateOnCompletedButtonText;
        _offloadService = ViewModelContextProvider.GetOffloadService();
        _modalManager = viewModelContextProvider.GetModalService();
        _audioIntegrityService = viewModelContextProvider.GetAudioIntegrityService();
        _backupService = viewModelContextProvider.GetDbBackupService();

        LocalDatabaseReplicationManager = viewModelContextProvider.GetLocalDatabaseReplicationManager();
        
        CancelOnDownloadingCommand = ReactiveCommand.CreateFromTask(StopBackup);
        RetryOnErrorCommand = ReactiveCommand.CreateFromTask(StartBackup);

        NavigateOnCompletedCommand = ReactiveCommand.CreateFromTask(async () =>
        {
            return await navigateOnCompletedCommand.Invoke();
        });

        CancelOnErrorCommand = ReactiveCommand.CreateFromTask(async () =>
        {
            ShowProgressView = false;
            AddProjectState = AddProjectState.None;
        });

        Disposables.Add(this.WhenAnyValue(x => x.LocalDatabaseReplicationManager.Status)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(async status => { await CheckImportDownloadStateAsync(status); }));
    }

    public async Task StartBackup()
    {
        var xamarinDatabasePath = GetXamarinDatabasePath();

        if (_backupService.DatabaseExists(xamarinDatabasePath))
        {
            LocalDatabaseReplicationManager.Reset();
            OnReplicationStart();

            var backupDatabasePath = await _backupService.BackupDatabaseAsync(xamarinDatabasePath);
            var hasAudioLoss = await _audioIntegrityService.DoesRemoteBucketContainAudioLoss(backupDatabasePath);

            if(hasAudioLoss is null)
            {
                OnReplicationFailed();

                await _modalManager.ShowInfoModal(
                        Icon.PopUpWarning,
                        AppResources.SyncPathIsNotAvailable,
                        AppResources.ChooseAnotherStorage);

                return;
            }

            if (LocalDatabaseReplicationManager.Status is ReplicationStatus.Cancelled)
            {
                LocalDatabaseReplicationManager.Reset();
                await _backupService.RemoveDatabaseBackupAsync();

                return;
            }

            ProjectIds = await _backupService.GetLocalProjectIdsAsync(backupDatabasePath);
            await _backupService.RemoveDatabaseBackupAsync();

            if (hasAudioLoss.Value)
            {
                await _modalManager.ShowInfoModal(Icon.OffloadItem,
                    AppResources.CannotLoadAudio,
                    AppResources.SyncAndTryAgain);
            }

            await LocalDatabaseReplicationManager.StartImportAsync(xamarinDatabasePath, true, true);
        }
        else
        {
            await _modalManager.ShowInfoModal(Icon.PopUpWarning, AppResources.NoProjectsToImport, AppResources.NoDataFromRender)!;
        }
    }

    private async Task StopBackup()
    {
        LocalDatabaseReplicationManager.StopReplication();
        await _offloadService.OffloadProjectsData(ProjectIds);
    }

    private async Task CheckImportDownloadStateAsync(ReplicationStatus replicationStatus)
    {
        switch (replicationStatus)
        {
            case ReplicationStatus.Completed:
                
                if (await _audioIntegrityService.DoesLocalDatabaseContainAudioLoss())
                {
                    await _modalManager.ShowInfoModal(Icon.OffloadItem,
                        AppResources.CannotLoadAudio,
                        AppResources.SyncAndTryAgain);
                }

                await AddProjectsAfterBackup();

                AddProjectState = AddProjectState.ProjectAddedSuccessfully;
                break;

            case ReplicationStatus.Error:
                OnReplicationFailed();
                await _offloadService.OffloadProjectsData(ProjectIds);
                break;
            case ReplicationStatus.Replication:
                OnReplicationStart();
                break;
            case ReplicationStatus.Cancelled:
                ShowProgressView = false;
                break;
            case ReplicationStatus.NotStarted:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(replicationStatus), replicationStatus, null);
        }
    }

    private async Task AddProjectsAfterBackup()
    {
        var localProjectsRepository = ViewModelContextProvider.GetLocalProjectsRepository();

        var localProjects = await localProjectsRepository.GetLocalProjectsForMachine();

        //for the case if a backup is made after at least one project has already been downloaded.
        foreach (var projectId in ProjectIds.Where(projectId => !localProjects.GetDownloadedProjects().Select(x => x.ProjectId).Contains(projectId)))
        {
            localProjects.AddDownloadedProject(projectId);
        }

        await localProjectsRepository.SaveUpdates(localProjects);
    }

    private string GetXamarinDatabasePath()
    {
        var appDirectory = ViewModelContextProvider.GetAppDirectory();
        var appSettings = ViewModelContextProvider.GetAppSettings();

        return $@"{Directory.GetParent(appDirectory.AppData)?.Parent}\{appSettings.RenderXamarinAppDirName}\LocalState";
    }

    private void OnReplicationStart()
    {
        ShowProgressView = true;
        AddProjectState = AddProjectState.Loading;
    }

    private void OnReplicationFailed()
    {
        AddProjectState = AddProjectState.ErrorAddingProject;
    }

    public override void Dispose()
    {
        LocalDatabaseReplicationManager?.Dispose();
        LocalDatabaseReplicationManager = null;
        RetryOnErrorCommand = null;
        CancelOnDownloadingCommand = null;
        CancelOnErrorCommand = null;
        base.Dispose();
    }
}