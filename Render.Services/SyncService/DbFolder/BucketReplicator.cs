using Couchbase.Lite;
using Couchbase.Lite.Sync;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Render.Interfaces;
using Render.Repositories.Extensions;
using Render.Repositories.Kernel;

namespace Render.Services.SyncService.DbFolder;

public class BucketReplicator(IRenderLogger renderLogger) : ReactiveObject, IBucketReplicator
{
    private Database _localDatabase;
    private Database _sourceDatabase;

    private Replicator _replicator;
    private ListenerToken _listenerToken;
    private ReplicatorStatus CurrentStatus { get; set; }
    private ReplicatorStatus LastActivity { get; set; }

    public int TotalToSync { get; set; }
    public int TotalSynced { get; set; }

    [Reactive] public bool Completed { get; private set; }

    public void ConfigureDatabases(Buckets databaseName, string localDatabasePath, string sourceDatabase)
    {
        var localDatabaseConfiguration = localDatabasePath.IsNullOrEmpty()
            ? null
            : new DatabaseConfiguration()
            {
                Directory = localDatabasePath,
            };

        _localDatabase = new Database(databaseName.ToString(), localDatabaseConfiguration);

        var sourceDatabaseConfiguration = sourceDatabase.IsNullOrEmpty()
            ? null 
            : new DatabaseConfiguration() { Directory = sourceDatabase };
        
        _sourceDatabase = new Database(databaseName.ToString(), sourceDatabaseConfiguration);
    }

    public void StartReplication(ReplicatorType replicatorType, bool freshDownload, List<string> filterIds = null)
    {
        var replicatorConfig = new ReplicatorConfiguration(new DatabaseEndpoint(_sourceDatabase))
        {
            ReplicatorType = replicatorType,
            Continuous = false
        };
        var collectionConfiguration = new CollectionConfiguration()
        {
            ConflictResolver = CustomConflictResolver.Instance,
        };
        if (filterIds is { Count: > 0 })
        {
            collectionConfiguration.PullFilter = LocalReplicator.GetFilterFunc(filterIds);
            collectionConfiguration.PushFilter = LocalReplicator.GetFilterFunc(filterIds);
        }

        replicatorConfig.AddCollection(_localDatabase.GetDefaultCollection(), collectionConfiguration);
        _replicator = new Replicator(replicatorConfig);
        _listenerToken = _replicator.AddChangeListener(
            (sender, args) =>
            {
                if (sender is not Replicator replicator) return;

                if (args.Status.Error != null)
                {
                    renderLogger.LogError(args.Status.Error);
                }

                CurrentStatus = args.Status;

                if (CurrentStatus.Activity is ReplicatorActivityLevel.Idle or ReplicatorActivityLevel.Stopped &&
                    LastActivity.Activity == ReplicatorActivityLevel.Busy)
                {
                    TotalToSync = (int)args.Status.Progress.Completed;
                    TotalSynced = (int)args.Status.Progress.Total;
                    Completed = true;
                    replicator.Stop();
                    replicator.Dispose();
                    return;
                }

                LastActivity = args.Status;
            });

        _replicator?.Start(freshDownload);
    }
    
    public void CancelReplication()
    {
        _replicator?.Stop();
        _replicator?.RemoveChangeListener(_listenerToken);
    }
    
    public void Dispose()
    {
        _replicator?.Stop();
        _replicator?.RemoveChangeListener(_listenerToken);
        _localDatabase?.Close();
        _sourceDatabase?.Close();
        _replicator?.Dispose();
        
        Completed = false;
    }
}