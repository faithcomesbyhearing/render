namespace Render.Services.SyncService
{
    public interface IWebSyncService
    {
        string ConnectionString { get; }

        int MaxSyncAttempts { get; }

        string SyncGatewayUsername { get; }

        string SyncGatewayPassword { get; }

        CurrentSyncStatus CurrentSyncStatus { get; }

        void StartWebSync(
            List<Guid> projectIds,
            List<Guid> globalUserIds,
            string syncGatewayUsername,
            string syncGatewayPassword,
            bool startTimer = true);

        void StartWebSync();

        IOneShotReplicator GetAdminDownloader(
            string syncGatewayUsername,
            string syncGatewayPassword,
            string databasePath);

        void StopWebSync();
    }
}