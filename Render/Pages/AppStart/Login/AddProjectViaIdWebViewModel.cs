using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Render.Kernel;
using Render.Kernel.WrappersAndExtensions;
using Render.Models.LocalOnlyData;
using Render.Repositories.Kernel;
using Render.Repositories.LocalDataRepositories;
using Render.TempFromVessel.Project;
using Render.WebAuthentication;

namespace Render.Pages.AppStart.Login;

public class AddProjectViaIdWebViewModel : ReactiveObject, IDisposable
{
    private Guid _projectId;
    private readonly IViewModelContextProvider _viewModelContextProvider;
    private readonly IDataPersistence<Project> _projectsRepository;
    private readonly ILocalProjectsRepository _localProjectsRepository;
    private IProjectDownloadService _projectDownloadService;
    private AuthenticationApiWrapper.RenderProjectDownloadObject _projectDownloadCredentials;
    private readonly IAudioLossRetryDownloadService _audioLossRetryDownloadService;

    private List<IDisposable> Disposables { get; } = new();

    [Reactive] public AddProjectState AddProjectState { get; private set; }
    [Reactive] public bool AllowProjectOffload { get; private set; } = true;

    public AddProjectViaIdWebViewModel(IViewModelContextProvider viewModelContextProvider, IAudioLossRetryDownloadService audioLossRetryDownloadService)
    {
        _viewModelContextProvider = viewModelContextProvider;
        _projectsRepository = viewModelContextProvider.GetPersistence<Project>();
        _localProjectsRepository = viewModelContextProvider.GetLocalProjectsRepository();
        _audioLossRetryDownloadService = audioLossRetryDownloadService;
    }

    public async Task StartProjectDownloadAsync(Guid projectId)
    {
        _projectId = projectId;
        AddProjectState = AddProjectState.Loading;

        var authenticationApiWrapper = _viewModelContextProvider.GetAuthenticationApiWrapper();
        var result = await authenticationApiWrapper.AuthenticateForProjectDownloadViaIdAsync(_projectId.ToString());
        if (result == null || string.IsNullOrEmpty(result.Username))
        {
            AddProjectState = AddProjectState.ErrorAddingProject;
            return;
        }

        _projectDownloadCredentials = result;
        _projectDownloadService = _viewModelContextProvider.GetProjectDownloaderService();


        var localProject = await GetLocalProjectOnMachine();
        if (localProject?.State == DownloadState.FinishedPartially)
        {
            _projectDownloadService.ResumeDownload(_projectId,
                _projectDownloadCredentials.GlobalUserIds,
                _projectDownloadCredentials.Username,
                _projectDownloadCredentials.Password);
        }
        else
        {
            _projectDownloadService.StartDownload(_projectId,
                _projectDownloadCredentials.GlobalUserIds,
                _projectDownloadCredentials.Username,
                _projectDownloadCredentials.Password);
        }

        SubscribeToProjectDownloadFinished();
    }

    public void StopProjectDownload()
    {
        _projectDownloadService?.StopDownload(_projectId);
    }

    private void SubscribeToProjectDownloadFinished()
    {
        Disposables.Add(_projectDownloadService.WhenDownloadFinished(_projectId)
            .Subscribe(async downloadedSuccessfully =>
            {
                if (downloadedSuccessfully)
                {
                    var project = await _projectsRepository.GetAsync(_projectId);
                    var isSuccess = project is { HasDeployedScopes: true };

                    if (isSuccess)
                    {
                        var downloadResult = await _audioLossRetryDownloadService.RetryDownloadIfAudioLoss(() => StartProjectDownloadAsync(_projectId), _projectId, 1);
                        if (downloadResult.AudioIsBroken && downloadResult.AutomaticRetryCompleted is false)
                        {
                            return;   
                        }
                    }

                    AddProjectState = isSuccess
                        ? AddProjectState.ProjectAddedSuccessfully
                        : AddProjectState.ErrorAddingProject;
                }
                else
                {
                    AddProjectState = AddProjectState.ErrorAddingProject;
                }


                await _localProjectsRepository.SaveLocalProject(_projectId, AddProjectState == AddProjectState.ProjectAddedSuccessfully);
                AllowProjectOffload = false;
            }));
    }

    private async Task<LocalProject> GetLocalProjectOnMachine()
    {
        var localProject = await _localProjectsRepository.GetLocalProjectsForMachine();
        return localProject.GetProject(_projectId);
    }

    public void Dispose()
    {
        Disposables?.DisposeCollection();
        Disposables?.Clear();
    }
}