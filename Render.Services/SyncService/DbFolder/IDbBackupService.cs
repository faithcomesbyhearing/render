using Render.Repositories.Kernel;

namespace Render.Services.SyncService.DbFolder;

public interface IDbBackupService
{
    bool DatabaseExists(string databasePath);
    bool BucketExists(string databasePath, Buckets bucket);
    Task<string> BackupDatabaseAsync(string databasePath, CancellationToken cancellationToken = default);
    Task<string> ReverseBackupDatabaseAsync(string databasePath, CancellationToken cancellationToken = default);
    Task RemoveDatabaseBackupAsync();
    Task RemoveAllDatabaseBackupsAsync();
    string GetOrCreateBackupDirectoryPath();
    Task<List<Guid>> GetLocalProjectIdsAsync(string databasePath);
}