using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Render.Kernel;
using Render.Kernel.WrappersAndExtensions;
using Render.Repositories.LocalDataRepositories;
using Render.Services.SyncService;
using Device = Render.Services.SyncService.Device;

namespace Render.Pages.AppStart.Login;

public class AddProjectViaIdLocalViewModel : ReactiveObject, IDisposable
{
    private Guid _projectId;
    private readonly IViewModelContextProvider _viewModelContextProvider;
    private readonly IHandshakeService _handshakeService;
    private readonly ILocalProjectsRepository _localProjectsRepository;
    private ILocalProjectDownloader _localProjectDownloader;
    private readonly IAudioLossRetryDownloadService _audioLossRetryDownloadService;

    [Reactive] public AddProjectState AddProjectState { get; private set; }

    public AddProjectViaIdLocalViewModel(IViewModelContextProvider viewModelContextProvider, IAudioLossRetryDownloadService audioLossRetryDownloadService)
    {
        _viewModelContextProvider = viewModelContextProvider;
        _handshakeService = viewModelContextProvider.GetHandshakeService();
        _localProjectsRepository = viewModelContextProvider.GetLocalProjectsRepository();
        _audioLossRetryDownloadService = audioLossRetryDownloadService;
    }

    public async Task StartProjectDownload(Guid projectId)
    {
        AddProjectState = AddProjectState.Loading;

        _handshakeService.ConnectionTimeOut += OnHandshakeTimeout;
        var broadcastMessage = await _handshakeService.TryToFindBroadcastForSync(projectId, includeTimeout: true);
        if (broadcastMessage != null)
        {
            var hubToConnect = await 
                _handshakeService.StartToConnectToServer(broadcastMessage, ConnectionTask.DownloadProject);
            StartSyncOnDeviceLocated(hubToConnect, projectId);
        }
    }

    public void StopProjectDownload()
    {
        _handshakeService.ConnectionTimeOut -= OnHandshakeTimeout;
        _handshakeService.DisconnectFromServer();
        _handshakeService.UnsubscribeOnServerDisconnected(OnHandshakeTimeout);
        
        if (_localProjectDownloader != null)
        {
            StopReplicationProcess();
            _localProjectDownloader.DownloadFinished -= CheckIfLocalGuidProjectDownloadState;
            _localProjectDownloader.Dispose();
        }
    }

    private void StartSyncOnDeviceLocated(Device device, Guid projectId)
    {
        _projectId = projectId;
        _handshakeService.ConnectionTimeOut -= OnHandshakeTimeout;
        //Need to ignore any other devices that are found later in the middle of sync
        if (_localProjectDownloader != null)
        {
            _handshakeService.UnsubscribeOnServerDisconnected(OnHandshakeTimeout);
            _localProjectDownloader.DownloadFinished -= CheckIfLocalGuidProjectDownloadState;
        }
        
        _handshakeService.SubscribeOnServerDisconnected(OnHandshakeTimeout);
        _localProjectDownloader = _viewModelContextProvider.GetLocalProjectDownloader(_projectId);
        _localProjectDownloader.BeginActiveLocalReplicationOfProject(device);
        _localProjectDownloader.DownloadFinished += CheckIfLocalGuidProjectDownloadState;
    }

    private async void CheckIfLocalGuidProjectDownloadState(LocalReplicationResult result)
    {
        _handshakeService.ConnectionTimeOut -= OnHandshakeTimeout;
        _handshakeService.DisconnectFromServer();
        _handshakeService.UnsubscribeOnServerDisconnected(OnHandshakeTimeout);
        
        switch (result)
        {
            case LocalReplicationResult.ProjectNotFound:
                AddProjectState = AddProjectState.ErrorAddingProject;
                break;
            case LocalReplicationResult.NotYetComplete:
                AddProjectState = AddProjectState.Loading;
                break;
            case LocalReplicationResult.Failed:
                AddProjectState = AddProjectState.ErrorConnectingToLocalMachine;
                break;
            case LocalReplicationResult.Succeeded:
                var downloadResult = await _audioLossRetryDownloadService.RetryDownloadIfAudioLoss(() => StartProjectDownload(_projectId), _projectId, 3);

                if (downloadResult.AudioIsBroken && downloadResult.AutomaticRetryCompleted is false)
                {
                    break;
                }

                AddProjectState = AddProjectState.ProjectAddedSuccessfully;
                _ = _localProjectsRepository.SaveLocalProject(_projectId, true);
                break;
            default:
                AddProjectState = AddProjectState.None;
                break;
        }
    }
    
    private void OnHandshakeTimeout()
    {
        AddProjectState = AddProjectState.ErrorConnectingToLocalMachine;
        StopProjectDownload();
    }

    private void StopReplicationProcess()
    {
        var connectedHub = _handshakeService.GetConnectedServer();
        _localProjectDownloader.CancelActiveLocalReplicationOfProject(connectedHub);
    }

    public void Dispose()
    {
        _handshakeService.ConnectionTimeOut -= OnHandshakeTimeout;

        if (_localProjectDownloader != null)
        {
            _localProjectDownloader.DownloadFinished -= CheckIfLocalGuidProjectDownloadState;
            _localProjectDownloader.Dispose();
        }
    }
}