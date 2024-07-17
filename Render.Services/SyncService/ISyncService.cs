namespace Render.Services.SyncService
{
    public interface ISyncService
    {
        string ConnectionString { get; }

        int MaxSyncAttempts { get; }

        string SyncGatewayUsername { get; }

        string SyncGatewayPassword { get; }

        CurrentSyncStatus CurrentSyncStatus { get; }

        void StartAllSync(
            List<Guid> projectIds,
            List<Guid> globalUserIds,
            string syncGatewayUsername,
            string syncGatewayPassword,
            bool startTimer = true);

        void StartAllSync();

        IOneShotReplicator GetAdminDownloader(
            string syncGatewayUsername,
            string syncGatewayPassword,
            string databasePath);

        void StopAllSync();
    }
}