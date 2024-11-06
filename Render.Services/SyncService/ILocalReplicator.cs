using Couchbase.Lite.Sync;

namespace Render.Services.SyncService
{
    public interface ILocalReplicator
    {
        int PassiveConnections { get; }

        LocalReplicationResult Result { get; }

        ReplicatorStatus CurrentStatus { get; }

        event Action SyncStatusUpdate;

        void Configure(string databaseName,
            int portNumber,
            string peerUsername,
            string peerPassword);

        void AddReplicator(
            Device device,
            Guid projectGuid,
            List<string> filterIds = null,
            bool oneShotDownload = false,
            bool freshDownload = false);

        void StartPassiveListener();

        void CancelPassiveListener();

        void CancelActiveReplications(Device deviceToDisconnect);

        void Dispose();
    }
}