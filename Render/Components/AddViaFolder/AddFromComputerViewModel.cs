using System.Reactive;
using System.Reactive.Linq;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Render.Components.AddProject;
using Render.Kernel;
using Render.Pages.AppStart.Login;
using Render.Repositories.Kernel;
using Render.Resources.Localization;
using Render.Services.SyncService;
using Render.Services.SyncService.DbFolder;

namespace Render.Components.AddViaFolder;

public class AddFromComputerViewModel : ViewModelBase, IAddProjectViewModel
{
    private readonly string _renderProjectBucketName = $"{Buckets.render.ToString()}.cblite2";
    private readonly string _renderAudioBucketName = $"{Buckets.renderaudio.ToString()}.cblite2";
    private readonly string _renderLocalOnlyBucketName = $"{Buckets.localonlydata.ToString()}.cblite2";
    
    private readonly IOffloadService _offloadService;

    [Reactive] public AddProjectState AddProjectState { get; set; } = AddProjectState.None;

    public ReactiveCommand<Unit, Unit> CancelOnDownloadingCommand { get; private set; }

    public ReactiveCommand<Unit, Unit> RetryOnErrorCommand { get; private set; }

    public ReactiveCommand<Unit, Unit> CancelOnErrorCommand { get; private set; }

    public ReactiveCommand<Unit, IRoutableViewModel> NavigateOnCompletedCommand { get; private set; }

    public string NavigateOnCompletedButtonText { get; }
    public string DownloadErrorText { get; } = AppResources.ErrorAddingProject;

    [Reactive] private IDbLocalReplicator DbLocalReplicator { get; set; }

    [Reactive] public bool ShowAddFromComputer { get; private set; }

    public AddFromComputerViewModel(IViewModelContextProvider viewModelContextProvider, string navigateOnCompletedButtonText,
        Func<Task<IRoutableViewModel>> navigateOnCompletedCommand = null)
        : base("AddViaFolder", viewModelContextProvider)
    {
        NavigateOnCompletedButtonText = navigateOnCompletedButtonText;
        _offloadService = ViewModelContextProvider.GetOffloadService();
        DbLocalReplicator = viewModelContextProvider.GetFolderProjectsDownloader();
        CancelOnDownloadingCommand = ReactiveCommand.CreateFromTask(StopImport);
        RetryOnErrorCommand = ReactiveCommand.CreateFromTask(OpenFileAndStartImport);

        NavigateOnCompletedCommand = ReactiveCommand.CreateFromTask(async () =>
        {
            if (navigateOnCompletedCommand is not null) return await NavigateTo(navigateOnCompletedCommand);
            ResetReplicator();
            return null;
        });

        CancelOnErrorCommand = ReactiveCommand.Create(() => { AddProjectState = AddProjectState.None; });

        Disposables.Add(this.WhenAnyValue(x => x.DbLocalReplicator.CurrentImportStatus)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(async state => await CheckImportDownloadState(state)));
    }

    public async Task OpenFileAndStartImport()
    {
        var dbFolder = await ViewModelContextProvider?.GetDownloadService()?.ChooseFileAsync()!;
        if (dbFolder is null) return;

        var dirNames = Directory.GetDirectories(dbFolder.Path).Select(Path.GetFileName).ToList();

        if (dirNames.Contains(_renderProjectBucketName)
            && dirNames.Contains(_renderAudioBucketName)
            && dirNames.Contains(_renderLocalOnlyBucketName))
        {
            DbLocalReplicator.StartImport(dbFolder.Path);
            ShowAddFromComputer = true;
        }
    }

    private async Task StopImport()
    {
        DbLocalReplicator.StopImport();
        await _offloadService.OffloadIncompleteProjects();
        ShowAddFromComputer = false;
    }

    private async Task CheckImportDownloadState(LocalReplicationResult result)
    {
        switch (result)
        {
            case LocalReplicationResult.Failed:
                AddProjectState = AddProjectState.ErrorAddingProject;
                await _offloadService.OffloadIncompleteProjects();
                break;
            case LocalReplicationResult.NotYetComplete:
                AddProjectState = AddProjectState.Loading;
                break;
            case LocalReplicationResult.Succeeded:
                AddProjectState = AddProjectState.ProjectAddedSuccessfully;
                break;
            case LocalReplicationResult.ProjectNotFound:
            case LocalReplicationResult.NotStarted:
            default:
                AddProjectState = AddProjectState.None;
                break;
        }
    }
    
    private async Task<IRoutableViewModel> NavigateTo(Func<Task<IRoutableViewModel>> navigateOnCompletedCommand)
    {
        Dispose();
        return await navigateOnCompletedCommand?.Invoke()!;
    }

    private void ResetReplicator()
    {
        ShowAddFromComputer = false;
        DbLocalReplicator?.Dispose();
        DbLocalReplicator = ViewModelContextProvider.GetFolderProjectsDownloader();
    }

    public override void Dispose()
    {
        DbLocalReplicator.Dispose();
        DbLocalReplicator = null;
        RetryOnErrorCommand = null;
        CancelOnDownloadingCommand = null;
        CancelOnErrorCommand = null;
        base.Dispose();
    }
}