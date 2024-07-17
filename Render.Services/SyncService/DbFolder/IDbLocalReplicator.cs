
namespace Render.Services.SyncService.DbFolder;

public interface IDbLocalReplicator : IDisposable
{
    void StartImport(string sourceDatabase);
    void StopImport();
    LocalReplicationResult CurrentImportStatus { get; }
}