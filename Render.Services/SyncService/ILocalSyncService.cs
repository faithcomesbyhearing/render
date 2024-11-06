namespace Render.Services.SyncService
{
    public interface ILocalSyncService
    {
        CurrentSyncStatus CurrentSyncStatus { get; }
        void StopLocalSync();
        Task StartLocalSync(string username, Guid projectId);
    }
}