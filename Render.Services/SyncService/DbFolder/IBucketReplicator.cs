using Render.Repositories.Kernel;

namespace Render.Services.SyncService.DbFolder;

public interface IBucketReplicator : IDisposable
{
    public void ConfigureDatabases(Buckets databaseName, string localDatabasePath, string sourceDatabase);
    public void StartReplication();
    int TotalToSync { get; }
    int TotalSynced { get; }
    bool Completed { get; }
    public void CancelReplication();

}