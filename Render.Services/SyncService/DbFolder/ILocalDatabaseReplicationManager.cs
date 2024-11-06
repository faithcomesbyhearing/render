using Render.TempFromVessel.Project;

namespace Render.Services.SyncService.DbFolder;

public interface ILocalDatabaseReplicationManager :IDisposable
{
    Task StartImportAsync(string sourceDatabase, bool freshDownload, bool includeLocal = false);
    Task StartExportAsync(string sourceDatabase, Project project);
    Task StartSyncDataAsync(string sourceDatabase, Guid projectId);
    void StopReplication();
    void Reset();
    public ReplicationStatus Status { get; }
    bool EnsureProjectDirectoryForReplicationExists(string projectPath);
    bool EnsureAllBucketsForProjectReplicationExists(string projectFolderPath);
    bool ReplicationWillStartInAnotherProjectFolder(string projectFolderPath);
}