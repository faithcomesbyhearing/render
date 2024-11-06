using System.Reactive.Linq;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Render.Kernel.WrappersAndExtensions;
using Render.Repositories.Extensions;
using Render.Repositories.Kernel;
using Render.Resources;
using Render.Resources.Localization;
using Render.Sequencer.Core.Utils.Extensions;
using Render.Services.GrandCentralStation;
using Render.Services.SyncService;
using Render.Services.SyncService.DbFolder;
using Render.TempFromVessel.Project;

namespace Render.Kernel.SyncServices;

public class UsbSyncService : ReactiveObject, IUsbSyncService
{
    [Reactive] public CurrentSyncStatus CurrentSyncStatus { get; private set; }

    private readonly ILocalDatabaseReplicationManager _localDatabaseReplicationManager;
    private readonly IDownloadService _downloadService;
    private readonly IUsbSyncFolderStorageService _usbSyncPathFolderStorage;
    private readonly IModalService _modalService;
    private readonly IDataPersistence<Project> _projectRepository;

    private readonly List<IDisposable> _disposables = [];

    public UsbSyncService(
        ILocalDatabaseReplicationManager localDatabaseReplicationManager,
        IDownloadService downloadService,
        IUsbSyncFolderStorageService usbSyncPathFolderStorage,
        IModalService modalService,
        IDataPersistence<Project> projectRepository)
    {
        _localDatabaseReplicationManager = localDatabaseReplicationManager;
        _downloadService = downloadService;
        _usbSyncPathFolderStorage = usbSyncPathFolderStorage;
        _modalService = modalService;
        _projectRepository = projectRepository;
        
        this.WhenAnyValue(x => x._localDatabaseReplicationManager.Status)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(async status => await CheckReplicationStateAsync(status))
            .ToDisposables(_disposables);
    }

    public async Task StartUsbSync(Guid projectId)
    {
        if (CurrentSyncStatus == CurrentSyncStatus.ActiveReplication) 
        { 
            return; 
        }

        if(CurrentSyncStatus is CurrentSyncStatus.ErrorEncountered)
        {
            _usbSyncPathFolderStorage.SetUsbSyncFolder(default, default);
        }

        var storedUsbFolderPath = await _usbSyncPathFolderStorage.GetUsbSyncFolder(projectId);
        
        string projectFolderPath;

        if (storedUsbFolderPath.IsNullOrEmpty())
        {
            var selectedStorageFromDialog = await _downloadService.ChooseFolderAsync();

            if (selectedStorageFromDialog is null)
            {
                await ResetStateAndShowPopup(AppResources.NoDataForSync);
                return;
            }

            var projectName = $"Project_{projectId}";
            var directoryPath = selectedStorageFromDialog.Path.Contains(projectName)
                ? selectedStorageFromDialog.Path
                : @$"{selectedStorageFromDialog.Path}\{projectName}";

            _usbSyncPathFolderStorage.SetUsbSyncFolder(directoryPath, projectName);

            projectFolderPath = await _usbSyncPathFolderStorage.GetUsbSyncFolder(projectId);

            if (projectFolderPath is null)
            {
                return;
            }
        }
        else
        {
            projectFolderPath = storedUsbFolderPath;
        }

        await ResolveDataSyncOrExport(projectFolderPath, projectId);
    }

    public void StopUsbSync()
    {
        _localDatabaseReplicationManager.StopReplication();
    }

    private async Task ResolveDataSyncOrExport(string directoryPath, Guid currentProjectId)
    {
        if (_localDatabaseReplicationManager.EnsureProjectDirectoryForReplicationExists(directoryPath)
            && _localDatabaseReplicationManager.EnsureAllBucketsForProjectReplicationExists(directoryPath))
        {
            await _localDatabaseReplicationManager.StartSyncDataAsync(directoryPath, currentProjectId);
        }
        else
        {
            if (_localDatabaseReplicationManager.ReplicationWillStartInAnotherProjectFolder(directoryPath))
            {
                await ResetStateAndShowPopup(AppResources.StorageAlreadyContains);
                return;
            }

            await _localDatabaseReplicationManager.StartExportAsync(directoryPath, await _projectRepository.GetAsync(currentProjectId));
        }
    }

    private async Task ResetStateAndShowPopup(string title)
    {
        CurrentSyncStatus = CurrentSyncStatus.NotStarted;
        _usbSyncPathFolderStorage.SetUsbSyncFolder(default, default);
        await _modalService.ShowInfoModal(Icon.PopUpWarning, title, AppResources.ChooseAnotherStorage)!;
    }

    private async Task CheckReplicationStateAsync(ReplicationStatus replicationStatus)
    {
        switch (replicationStatus)
        {
            case ReplicationStatus.Completed:
                CurrentSyncStatus = CurrentSyncStatus.Finished;
                break;
            case ReplicationStatus.Error:
                CurrentSyncStatus = CurrentSyncStatus.ErrorEncountered;
                await _modalService.ShowInfoModal(
                    Icon.PopUpWarning, 
                    AppResources.SyncPathIsNotAvailable, 
                    AppResources.ChooseAnotherStorage);
                break;
            case ReplicationStatus.Replication:
                CurrentSyncStatus = CurrentSyncStatus.ActiveReplication;
                break;
            case ReplicationStatus.NotStarted:
                break;
            case ReplicationStatus.Cancelled:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(replicationStatus), replicationStatus, null);
        }
    }

    public void Dispose()
    {
        _localDatabaseReplicationManager?.StopReplication();
        _localDatabaseReplicationManager?.Dispose();
    }
}