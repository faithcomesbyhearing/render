namespace Render.Services.SyncService
{
    public enum CurrentSyncStatus
    {
        NotStarted,
        Looking,
        ActiveReplication,
        ErrorEncountered,
        Finished
    }
}