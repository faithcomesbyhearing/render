namespace Render.Services.SyncService;

public interface ILocalReplicationService
{
    public void BeginActiveLocalReplicators(Device device, Guid projectId);
    public void BeginPassiveLocalListeners();
    public void CancelActiveLocalReplications(Device connectedDevice);
    public void CancelPassiveLocalListeners();
    public int GetCouchbasePassiveConnectionCount();
    void SubscribeOnSyncUpdateAction(Action setCurrentReplicationStatus);
    void UnsubscribeOnSyncUpdateAction(Action setCurrentReplicationStatus);
    void DisposeReplicators();
}