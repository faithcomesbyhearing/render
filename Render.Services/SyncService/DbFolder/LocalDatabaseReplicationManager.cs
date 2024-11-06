using System.Collections.Concurrent;
using Couchbase.Lite.Sync;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Render.Interfaces;
using Render.Repositories.Extensions;
using Render.Repositories.Kernel;
using Render.TempFromVessel.Project;

namespace Render.Services.SyncService.DbFolder;

public enum ReplicationStatus
{
    NotStarted,
    Error,
    Cancelled,
    Replication,
    Completed
}

public class LocalDatabaseReplicationManager : ReactiveObject, ILocalDatabaseReplicationManager
{
    private readonly IRenderLogger _logger;
    private readonly IBucketReplicator _renderProjectReplicator;
    private readonly IBucketReplicator _renderAudioReplicator;
    private readonly IBucketReplicator _renderLocalOnlyReplicator;

    private readonly IDbBackupService _backupService;
    private readonly string _localDatabasePath;

    private string _reverseDatabasePath;
    private ConcurrentBag<IBucketReplicator> Replicators { get; } = [];
    private List<IDisposable> _disposables = [];

    private CancellationTokenSource _cts;

    [Reactive]
    public ReplicationStatus Status { get; private set; }

    public LocalDatabaseReplicationManager(IDbBackupService backupService, IRenderLogger logger, string localDatabasePath)
    {
        _localDatabasePath = localDatabasePath;
        _logger = logger;
        _renderLocalOnlyReplicator = new BucketReplicator(logger);
        _renderProjectReplicator = new BucketReplicator(logger);
        _renderAudioReplicator = new BucketReplicator(logger);

        _backupService = backupService;
        _cts = new CancellationTokenSource();

        _disposables.Add(this
            .WhenAnyValue(
                x => x._renderProjectReplicator.Completed,
                x => x._renderAudioReplicator.Completed,
                x => x._renderLocalOnlyReplicator.Completed)
            .Subscribe(async _ => await CheckReplicationCompletedAsync()));
    }

    public async Task StartImportAsync(string sourceDatabase, bool freshDownload, bool includeLocal = false)
    {
        Status = ReplicationStatus.Replication;
        ResetCancelationToken();

        sourceDatabase = await BackupDatabaseAsync(sourceDatabase);
        if (sourceDatabase.IsNullOrEmpty())
        {
            return;
        }

        StartReplicationProcessForBucket(sourceDatabase, _renderProjectReplicator, Buckets.render, ReplicatorType.Pull,
            freshDownload);
        StartReplicationProcessForBucket(sourceDatabase, _renderAudioReplicator, Buckets.renderaudio,
            ReplicatorType.Pull, freshDownload);

        if (includeLocal)
        {
            StartReplicationProcessForBucket(sourceDatabase, _renderLocalOnlyReplicator, Buckets.localonlydata,
                ReplicatorType.Pull, freshDownload);
        }
    }

    public async Task StartExportAsync(string sourceDatabase, Project project)
    {
        Status = ReplicationStatus.Replication;
        ResetCancelationToken();

        _reverseDatabasePath = sourceDatabase;
        sourceDatabase = await BackupDatabaseAsync(sourceDatabase);
        if (sourceDatabase.IsNullOrEmpty()) 
        { 
            return;
        }

        var filters = new List<string>() { project.ProjectId.ToString() };
        filters.AddRange(project.GlobalUserIds.Select(userId => userId.ToString()));

        StartReplicationProcessForBucket(sourceDatabase, _renderProjectReplicator, Buckets.render,
            ReplicatorType.Push, freshDownload: false, filters);
        StartReplicationProcessForBucket(sourceDatabase, _renderAudioReplicator,
            Buckets.renderaudio, ReplicatorType.Push, freshDownload: false,
            [project.ProjectId.ToString()]);
    }

    public async Task StartSyncDataAsync(string sourceDatabase, Guid projectId)
    {
        Status = ReplicationStatus.Replication;
        ResetCancelationToken();

        _reverseDatabasePath = sourceDatabase;
        sourceDatabase = await BackupDatabaseAsync(sourceDatabase);

        if (sourceDatabase.IsNullOrEmpty())
        {
            return;
        }

        StartReplicationProcessForBucket(sourceDatabase, _renderProjectReplicator, Buckets.render,
            ReplicatorType.PushAndPull, freshDownload: true,
            [projectId.ToString()]);
        StartReplicationProcessForBucket(sourceDatabase, _renderAudioReplicator, Buckets.renderaudio,
            ReplicatorType.PushAndPull, freshDownload: true,
            [projectId.ToString()]);
    }

    public void StopReplication()
    {
        Status = ReplicationStatus.Cancelled;

        _cts.Cancel();
        ReplicatorMaintenance(true);
    }

    public void Reset()
    {
        ResetCancelationToken();
        ReplicatorMaintenance(true);

        Status = ReplicationStatus.NotStarted;
    }

    public bool EnsureProjectDirectoryForReplicationExists(string projectPath)
    {
        if (Directory.Exists(projectPath))
        {
            return true;
        }

        if (projectPath != null)
        {
            Directory.CreateDirectory(projectPath);
        }

        return false;
    }

    public bool EnsureAllBucketsForProjectReplicationExists(string projectDirectoryPath)
    {
        return _backupService.BucketExists(projectDirectoryPath, Buckets.render) &&
               _backupService.BucketExists(projectDirectoryPath, Buckets.renderaudio);
    }

    public bool ReplicationWillStartInAnotherProjectFolder(string projectDirectoryPath)
    {
        return Directory.GetParent(projectDirectoryPath)!.Name.Contains(nameof(Project));
    }

    private void StartReplicationProcessForBucket(
        string sourceDatabasePath,
        IBucketReplicator bucketReplicator,
        Buckets bucket,
        ReplicatorType typeOfReplication,
        bool freshDownload,
        List<string> filterIds = null)
    {
        if (_cts?.IsCancellationRequested is true)
        {
            return;
        }

        _logger.LogInfo($"Start replication for the '{bucket}' bucket.");
        Replicators.Add(bucketReplicator);
        bucketReplicator.ConfigureDatabases(bucket, _localDatabasePath, sourceDatabasePath);
        bucketReplicator.StartReplication(typeOfReplication, freshDownload, filterIds);
    }

    private async Task<string> BackupDatabaseAsync(string databasePath, bool reverse = false) 
    {
        try
        {
            _logger.LogInfo(reverse is false ? "Start database transferring from the remote drive to the local temporary folder." :
                                               "Start database transferring from the local temporary folder to the remote drive.");

            return reverse ? await _backupService.ReverseBackupDatabaseAsync(databasePath, _cts.Token) : 
                             await _backupService.BackupDatabaseAsync(databasePath, _cts.Token);
        }
        catch(Exception ex) 
        {
            _logger.LogError(ex);

            Status = ReplicationStatus.Error;

            return null;
        }
    }

    private async Task CheckReplicationCompletedAsync()
    {
        if (Replicators.IsEmpty || Replicators.Any(replicator => replicator.Completed is false))
        {
            return;
        }

        await FinishDownloadAndCleanupAsync();
    }

    private async Task FinishDownloadAndCleanupAsync()
    {
        var completed = Replicators.All(replicator => replicator.TotalToSync == replicator.TotalSynced);

        if (completed && _reverseDatabasePath.IsNullOrEmpty() is false)
        {
            await BackupDatabaseAsync(_reverseDatabasePath, true);
        }

        if (Status is not ReplicationStatus.Error) 
        {
            Status = completed ? ReplicationStatus.Completed : ReplicationStatus.Error;
        }

        ReplicatorMaintenance();
        
        _ = _backupService.RemoveDatabaseBackupAsync();
    }

    private void ReplicatorMaintenance(bool cancel = false)
    {
        foreach (var replicator in Replicators)
        {
            if (cancel)
            {
                replicator.CancelReplication();
            }

            replicator.Dispose();
        }

        Replicators.Clear();
    }

    private void ResetCancelationToken()
    {
        _cts?.Dispose();
        _cts = new CancellationTokenSource();
    }

    public void Dispose()
    {
        _disposables.ForEach(x => x.Dispose());
        _disposables.Clear();

        _cts?.Dispose();
        _cts = null;

        ReplicatorMaintenance();
    }
}