namespace Render.Services.SyncService
{
    public enum LocalSyncStatus
    {
        NotSet,
        Active,
        Passive
    }

    public interface ILocalSyncService
    {
        CurrentSyncStatus CurrentSyncStatus { get; }
        int ConnectionCount { get; }
        LocalSyncStatus LocalSyncStatus { get; }
        bool BeginActiveLocalReplication(Device device, Guid projectId);
        void StopLocalSync();
        void BeginPassiveLocalReplication();
        void StartLocalSync(string username, Guid projectId);
    }
}