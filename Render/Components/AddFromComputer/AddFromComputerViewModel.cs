using System.Reactive;
using System.Reactive.Linq;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Render.Components.AddProject;
using Render.Kernel;
using Render.Kernel.WrappersAndExtensions;
using Render.Models.Sections;
using Render.Pages.AppStart.Login;
using Render.Resources;
using Render.Resources.Localization;
using Render.Services.SyncService;
using Render.Services.SyncService.DbFolder;
using Render.TempFromVessel.Project;

namespace Render.Components.AddFromComputer;

public class AddFromComputerViewModel : ViewModelBase, IAddProjectViewModel
{
    private readonly IOffloadService _offloadService;
    private readonly IModalService _modalManager;
    private readonly IAudioIntegrityService _audioIntegrityService;

    [Reactive] public AddProjectState AddProjectState { get; set; } = AddProjectState.None;

    public ReactiveCommand<Unit, Unit> CancelOnDownloadingCommand { get; private set; }

    public ReactiveCommand<Unit, Unit> RetryOnErrorCommand { get; private set; }

    public ReactiveCommand<Unit, Unit> CancelOnErrorCommand { get; private set; }

    public ReactiveCommand<Unit, IRoutableViewModel> NavigateOnCompletedCommand { get; private set; }

    public string NavigateOnCompletedButtonText { get; }
    public string DownloadErrorText { get; } = AppResources.ErrorAddingProject;

    [Reactive] private ILocalDatabaseReplicationManager LocalDatabaseReplicationManager { get; set; }

    [Reactive] public bool ShowProgressView { get; private set; }

    private Guid ProjectId { get; set; }

    public AddFromComputerViewModel(
        IViewModelContextProvider viewModelContextProvider,
        string navigateOnCompletedButtonText,
        Func<Task<IRoutableViewModel>> navigateOnCompletedCommand = null, 
        Func<Task> actionOnDownloadCompleted = null)
        : base("AddFromComputer", viewModelContextProvider)
    {
        NavigateOnCompletedButtonText = navigateOnCompletedButtonText;
        _offloadService = ViewModelContextProvider.GetOffloadService();
        _modalManager = viewModelContextProvider.GetModalService();
        _audioIntegrityService = viewModelContextProvider.GetAudioIntegrityService();
        LocalDatabaseReplicationManager = viewModelContextProvider.GetLocalDatabaseReplicationManager();
        CancelOnDownloadingCommand = ReactiveCommand.CreateFromTask(StopImport);
        RetryOnErrorCommand = ReactiveCommand.CreateFromTask(OpenFileAndStartImport);

        NavigateOnCompletedCommand = ReactiveCommand.CreateFromTask(async () =>
        {
            if (navigateOnCompletedCommand is not null)
            {
                return await navigateOnCompletedCommand.Invoke();
            }

            if (actionOnDownloadCompleted is not null)
            {
                await actionOnDownloadCompleted.Invoke();
            }
            ShowProgressView = false;
            return null;
        });

        CancelOnErrorCommand = ReactiveCommand.Create(() =>
        {
            ShowProgressView = false;
            AddProjectState = AddProjectState.None;
        });

        Disposables.Add(this.WhenAnyValue(x => x.LocalDatabaseReplicationManager.Status)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(async status =>
            {
                await CheckImportDownloadStateAsync(status);
            }));
    }

    public async Task OpenFileAndStartImport()
    {
        var projectForImportPath = await ViewModelContextProvider?.GetDownloadService()?.ChooseFolderAsync()!;
        if (projectForImportPath is null) return;

        if (projectForImportPath.Name.Contains("Project"))
        {
            var projectNameArray = projectForImportPath.Name.Split('_');

            if (Guid.TryParse(projectNameArray[1], out var parsedGuid)
                && LocalDatabaseReplicationManager.EnsureAllBucketsForProjectReplicationExists(
                    projectForImportPath.Path))
            {
                LocalDatabaseReplicationManager.Reset();
                OnReplicationStart();

                var hasAudioLoss = await _audioIntegrityService.DoesRemoteBucketContainAudioLoss(projectForImportPath.Path);
                if (hasAudioLoss is null) 
                {
                    await OnReplicationFailedAsync();
                    return;
                }

                if(LocalDatabaseReplicationManager.Status is ReplicationStatus.Cancelled)
                {
                    LocalDatabaseReplicationManager.Reset();
                    return;
                }

                if (hasAudioLoss.Value)
                {
                    await _modalManager.ShowInfoModal(Icon.OffloadItem,
                        AppResources.CannotLoadAudio,
                        AppResources.SyncAndTryAgain);
                }

                ProjectId = parsedGuid;
                await LocalDatabaseReplicationManager.StartImportAsync(projectForImportPath.Path, freshDownload: true);
                
                return;
            }
        }

        await ViewModelContextProvider?.GetModalService()?
            .ShowInfoModal(Icon.PopUpWarning, AppResources.NoProjectsToImport, AppResources.ChooseAnotherStorage)!;
    }

    private async Task StopImport()
    {
        LocalDatabaseReplicationManager.StopReplication();

        await _offloadService.OffloadProject(ProjectId);
    }

    private async Task AddProjectsToMachine()
    {
        var project = await ViewModelContextProvider.GetPersistence<Project>().GetAsync(ProjectId);
        var localProjectRepository = ViewModelContextProvider.GetLocalProjectsRepository();
        var localProjects = await localProjectRepository.GetLocalProjectsForMachine();

        var workflowStatusList = await ViewModelContextProvider.GetPersistence<WorkflowStatus>().QueryOnFieldAsync(
            "ProjectId",
            project.Id.ToString(), 0);

        if (workflowStatusList.Count == 0)
        {
            AddProjectState = AddProjectState.ErrorAddingProject;
            return;
        }

        localProjects.AddDownloadedProject(project.ProjectId);
        await localProjectRepository.SaveLocalProjectsForMachine(localProjects);
    }

    private async Task CheckImportDownloadStateAsync(ReplicationStatus replicationStatus)
    {
        switch (replicationStatus)
        {
            case ReplicationStatus.Completed:
                if (await _audioIntegrityService.DoesProjectContainAudioLoss(ProjectId))
                {
                    await _modalManager.ShowInfoModal(Icon.OffloadItem,
                        AppResources.CannotLoadAudio,
                        AppResources.SyncAndTryAgain);
                }

                await AddProjectsToMachine();
                AddProjectState = AddProjectState.ProjectAddedSuccessfully;
                break;
            
            case ReplicationStatus.Error:
                await _offloadService.OffloadProject(ProjectId);
                await OnReplicationFailedAsync();
                break;         
            case ReplicationStatus.Cancelled:
                ShowProgressView = false;
                break;
            case ReplicationStatus.Replication:
                OnReplicationStart();
                break;
            case ReplicationStatus.NotStarted:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(replicationStatus), replicationStatus, null);
        }
    }

    private void OnReplicationStart()
    {
        ShowProgressView = true;
        AddProjectState = AddProjectState.Loading;
    }

    private async Task OnReplicationFailedAsync()
    {
        AddProjectState = AddProjectState.ErrorAddingProject;

        await _modalManager.ShowInfoModal(
            Icon.PopUpWarning,
            AppResources.SyncPathIsNotAvailable,
            AppResources.ChooseAnotherStorage);
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