using Couchbase.Lite.Sync;
using Render.Repositories.Kernel;

namespace Render.Services.SyncService.DbFolder;

public interface IBucketReplicator : IDisposable
{
    public void ConfigureDatabases(Buckets databaseName, string localDatabasePath, string sourceDatabase);
    public void StartReplication(ReplicatorType replicatorType, bool freshDownload, List<string> filterIds = null);
    int TotalToSync { get; }
    int TotalSynced { get; }
    bool Completed { get; }
    public void CancelReplication();
    
}