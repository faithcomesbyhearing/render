using Render.Repositories.Kernel;
using Render.TempFromVessel.Kernel;

namespace Render.Services.SyncService;

public class LocalReplicationService : ILocalReplicationService
{
    private bool Initialized { get; set; }

    private readonly IAppSettings _appSettings;
    private readonly ILocalReplicator _renderReplicatorWrapper;
    private readonly ILocalReplicator _renderRenderProjectsReplicatorWrapper;
    private readonly ILocalReplicator _audioReplicatorWrapper;
    
    public LocalReplicationService(
        IAppSettings appSettings,
        ILocalReplicator renderReplicatorWrapper,
        ILocalReplicator renderProjectsReplicatorWrapper,
        ILocalReplicator audioReplicatorWrapper)
    {
        _appSettings = appSettings;
        _renderReplicatorWrapper = renderReplicatorWrapper;
        _renderRenderProjectsReplicatorWrapper = renderProjectsReplicatorWrapper;
        _audioReplicatorWrapper = audioReplicatorWrapper;
        
        ConfigureBucketsReplicators();
    }
    
    public void BeginActiveLocalReplicators(Device device, Guid projectId)
    {
        _renderReplicatorWrapper.AddReplicator(device, projectId, new List<string> { projectId.ToString() });
        _renderRenderProjectsReplicatorWrapper.AddReplicator(device, projectId,
            new List<string> { projectId.ToString() });
        _audioReplicatorWrapper.AddReplicator(device, projectId, new List<string> { projectId.ToString() });
    }

    public void BeginPassiveLocalListeners()
    {
        _renderReplicatorWrapper.StartPassiveListener();
        _renderRenderProjectsReplicatorWrapper.StartPassiveListener();
        _audioReplicatorWrapper.StartPassiveListener();
    }

    public int GetCouchbasePassiveConnectionCount()
    {
        var listOfPassiveConnections = new List<int>
        {
            _renderReplicatorWrapper.PassiveConnections,
            _renderRenderProjectsReplicatorWrapper.PassiveConnections,
            _audioReplicatorWrapper.PassiveConnections
        };
        return listOfPassiveConnections.Min();
    }

    public void CancelActiveLocalReplications(Device connectedDevice)
    {
        if (connectedDevice is null) return;

        _renderReplicatorWrapper?.CancelActiveReplications(connectedDevice);
        _renderRenderProjectsReplicatorWrapper?.CancelActiveReplications(connectedDevice);
        _audioReplicatorWrapper?.CancelActiveReplications(connectedDevice);
    }

    public void CancelPassiveLocalListeners()
    {
        _renderReplicatorWrapper?.CancelPassiveListener();
        _renderRenderProjectsReplicatorWrapper?.CancelPassiveListener();
        _audioReplicatorWrapper?.CancelPassiveListener();
    }

    public void SubscribeOnSyncUpdateAction(Action setCurrentReplicationStatus)
    {
        if (Initialized is false)
        {
            _renderReplicatorWrapper.SyncStatusUpdate += setCurrentReplicationStatus;
            _renderRenderProjectsReplicatorWrapper.SyncStatusUpdate += setCurrentReplicationStatus;
            _audioReplicatorWrapper.SyncStatusUpdate += setCurrentReplicationStatus;
            
            Initialized = true;
        }
    }

    public void UnsubscribeOnSyncUpdateAction(Action setCurrentReplicationStatus)
    {
        if (Initialized)
        {
            _renderReplicatorWrapper.SyncStatusUpdate -= setCurrentReplicationStatus;
            _renderRenderProjectsReplicatorWrapper.SyncStatusUpdate -= setCurrentReplicationStatus;
            _audioReplicatorWrapper.SyncStatusUpdate -= setCurrentReplicationStatus;
            
            Initialized = false;
        }
    }

    public void DisposeReplicators()
    {
        _renderReplicatorWrapper.Dispose();
        _audioReplicatorWrapper.Dispose();
        _renderRenderProjectsReplicatorWrapper.Dispose();
    }

    private void ConfigureBucketsReplicators()
    {
        _renderReplicatorWrapper.Configure(
            Buckets.render.ToString(),
            _appSettings.CouchbaseStartingPort,
            _appSettings.CouchbasePeerUsername,
            _appSettings.CouchbasePeerPassword);
        
        _renderRenderProjectsReplicatorWrapper.Configure(
            Buckets.render.ToString(),
            _appSettings.CouchbaseStartingPort + 1,
            _appSettings.CouchbasePeerUsername,
            _appSettings.CouchbasePeerPassword);
        
        _audioReplicatorWrapper.Configure(
            Buckets.renderaudio.ToString(),
            _appSettings.CouchbaseStartingPort + 3,
            _appSettings.CouchbasePeerUsername,
            _appSettings.CouchbasePeerPassword);
    }
}