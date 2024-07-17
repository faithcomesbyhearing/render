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
    private Device _device;
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

    public void Initialize()
    {
        _handshakeService.DeviceAvailable += HandshakeServiceOnDeviceAvailable;
        _handshakeService.BeginListener();
    }

    public void StartProjectDownload(Guid projectId)
    {
        _projectId = projectId;
        AddProjectState = AddProjectState.Loading;

        if (_device == null)
        {
            _handshakeService.DeviceAvailable -= HandshakeServiceOnDeviceAvailable;
            _handshakeService.DeviceAvailable += StartSyncOnDeviceLocated;
            _handshakeService.Timeout -= OnHandshakeTimeout;
            _handshakeService.Timeout += OnHandshakeTimeout;
            _handshakeService.BeginListener(true);
            return;
        }

        StartSyncOnDeviceLocated(_device);
    }

    public void StopProjectDownload()
    {
        _handshakeService.Timeout -= OnHandshakeTimeout;
        _handshakeService.CloseUDPListener();
        
        if (_localProjectDownloader != null)
        {
            _localProjectDownloader.DownloadFinished -= CheckIfLocalGuidProjectDownloadState;
            _localProjectDownloader.Dispose();
        }
    }

    private void StartSyncOnDeviceLocated(Device device)
    {
        //Need to ignore any other devices that are found later in the middle of sync
        _handshakeService.DeviceAvailable -= StartSyncOnDeviceLocated;
        _device = device;
        if (_localProjectDownloader != null)
        {
            _localProjectDownloader.DownloadFinished -= CheckIfLocalGuidProjectDownloadState;
        }

        _localProjectDownloader = _viewModelContextProvider.GetLocalProjectDownloader(_projectId);
        _localProjectDownloader.BeginActiveLocalReplicationOfProject(device);
        _localProjectDownloader.DownloadFinished += CheckIfLocalGuidProjectDownloadState;
    }

    private async void CheckIfLocalGuidProjectDownloadState(LocalReplicationResult result)
    {
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
                var downloadResult = await _audioLossRetryDownloadService.RetryDownloadIfAudioLoss(() =>
                {
                    StartProjectDownload(_projectId);
                    return Task.CompletedTask;
                }, _projectId, 3);
                
                if (downloadResult.AudioIsBroken && downloadResult.AutomaticRetryCompleted is false)
                {
                    break;   
                }
                
                AddProjectState = AddProjectState.ProjectAddedSuccessfully;
                _ = _localProjectsRepository.SaveLocalProject(_projectId, true);
                _handshakeService.Timeout -= OnHandshakeTimeout;
                break;
            default:
                AddProjectState = AddProjectState.None;
                break;
        }
    }

    private void HandshakeServiceOnDeviceAvailable(Device device)
    {
        _device = device;
    }

    private void OnHandshakeTimeout()
    {
        AddProjectState = AddProjectState.ErrorConnectingToLocalMachine;
    }

    public void Dispose()
    {
        _handshakeService.CloseUDPListener();
        _handshakeService.Timeout -= OnHandshakeTimeout;
        _handshakeService.DeviceAvailable -= HandshakeServiceOnDeviceAvailable;
        _handshakeService.DeviceAvailable -= StartSyncOnDeviceLocated;

        if (_localProjectDownloader != null)
        {
            _localProjectDownloader.DownloadFinished -= CheckIfLocalGuidProjectDownloadState;
            _localProjectDownloader.Dispose();
        }
    }
}