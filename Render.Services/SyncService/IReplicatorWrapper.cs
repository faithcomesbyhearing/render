using Couchbase.Lite.Sync;

namespace Render.Services.SyncService
{
    public interface IReplicatorWrapper
    {
        bool Completed { get; }

        bool Error { get; }

        int TotalToSync { get; }

        int TotalSynced { get; }

        ReplicatorStatus CurrentStatus { get; }

        void Configure(string databaseName,
            string connectionString,
            int maxSyncAttempts,
            string syncGatewayUsername,
            string syncGatewayPassword,
            List<string> projectIds = null);


        void StartSync();

        void Stop();

        void StartDownload(List<string> channels, bool freshDownload = false);

        void Dispose();
    }
}