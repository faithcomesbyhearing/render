using Render.Services.SyncService;

namespace Render.Kernel;

public interface ISyncStateService
{
    LastSynchronizationState GetLastSyncState();

    void SetLastSyncState(LastSynchronizationState synchronizationState);
}

public class SyncStateService : ISyncStateService
{
    private LastSynchronizationState _lastSynchronizationState;

    public LastSynchronizationState GetLastSyncState()
    {
        return _lastSynchronizationState;
    }

    public void SetLastSyncState(LastSynchronizationState synchronizationState)
    {
        _lastSynchronizationState = synchronizationState;
    }
}