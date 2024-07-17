using System.Reactive.Linq;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Render.Interfaces;
using Render.Repositories.Kernel;

namespace Render.Services.SyncService.DbFolder;

public class DbLocalReplicator : ReactiveObject, IDbLocalReplicator
{
    private readonly IBucketReplicator _renderProjectReplicator;
    private readonly IBucketReplicator _renderAudioReplicator;
    private readonly IBucketReplicator _renderLocalOnlyReplicator;

    private List<IBucketReplicator> Replicators { get; } = [];
    private readonly List<IDisposable> _disposables = [];

    [Reactive] public LocalReplicationResult CurrentImportStatus { get; private set; } = LocalReplicationResult.NotStarted;
    
    private readonly string _localDatabasePath;
    private string _sourceDatabasePath;
    public DbLocalReplicator(IRenderLogger logger, string localDatabasePath)
    {
        _localDatabasePath = localDatabasePath;
        _renderProjectReplicator = new BucketReplicator(logger);
        _renderAudioReplicator = new BucketReplicator(logger);
        _renderLocalOnlyReplicator = new BucketReplicator(logger);
        
        _disposables.Add(this
            .WhenAnyValue(x => x._renderProjectReplicator.Completed, x => x._renderAudioReplicator.Completed,
                x => x._renderLocalOnlyReplicator.Completed)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Skip(1)
            .Subscribe(x => CheckForImportCompleted()));
    }

    private void CheckForImportCompleted()
    {
        if (Replicators.Any(x => !x.Completed))
        {
            CurrentImportStatus = LocalReplicationResult.NotYetComplete;
            return;
        }
        CurrentImportStatus = Replicators.All(x => x.TotalToSync == x.TotalSynced) ? LocalReplicationResult.Succeeded : LocalReplicationResult.Failed;
        Replicators.Clear();
    }

    public void StartImport(string sourceDatabase)
    {
        _sourceDatabasePath = sourceDatabase;
        CurrentImportStatus = LocalReplicationResult.NotYetComplete;
        StartReplicationProcessForBucket(_renderProjectReplicator, Buckets.render);
        StartReplicationProcessForBucket(_renderAudioReplicator, Buckets.renderaudio);
        StartReplicationProcessForBucket(_renderLocalOnlyReplicator, Buckets.localonlydata);
    }

    public void StopImport()
    {
        Replicators.ForEach(x =>
        {
            x.CancelReplication();
            x.Dispose();
        });
        Replicators.Clear();
    }
    
    private void StartReplicationProcessForBucket(IBucketReplicator bucketReplicator, Buckets bucket)
    {
        Replicators.Add(bucketReplicator);
        bucketReplicator.ConfigureDatabases(bucket, _localDatabasePath, _sourceDatabasePath);
        bucketReplicator.StartReplication();
    }
    
    public void Dispose()
    {
        _disposables.ForEach(x => x.Dispose());
        _disposables.Clear();
        
        Replicators.ForEach(x =>
        {
            x.Dispose();
        });
        Replicators.Clear();
    }
    
}