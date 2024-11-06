using Couchbase.Lite;
using Couchbase.Lite.Query;
using Render.Interfaces;
using Render.Models.LocalOnlyData;
using Render.TempFromVessel.Kernel;
using Bucket = Render.Repositories.Kernel.Buckets;

namespace Render.Services.SyncService.DbFolder;

public class DbBackupService : IDbBackupService
{
    private const string BucketFormat = ".cblite2";

    private static readonly Dictionary<Bucket, string> _buckets = new()
    {
        { Bucket.render, $"{Bucket.render.ToString()}{BucketFormat}" },
        { Bucket.renderaudio, $"{Bucket.renderaudio.ToString()}{BucketFormat}" },
        { Bucket.localonlydata, $"{Bucket.localonlydata.ToString()}{BucketFormat}" },
    };

    private readonly IAppDirectory _appDirectory;
    private readonly IAppSettings _appSettings;

    private readonly string _tempKey;

    public DbBackupService(IAppDirectory appDirectory, IAppSettings appSettings)
    {
        _appDirectory = appDirectory;
        _appSettings = appSettings;

        _tempKey = Guid.NewGuid().ToString();
    }

    public Task<string> BackupDatabaseAsync(string databasePath, CancellationToken cancellationToken = default)
    {
        return BackupDatabaseAsync(databasePath, false, cancellationToken);
    }

    public Task<string> ReverseBackupDatabaseAsync(string databasePath, CancellationToken cancellationToken = default)
    {
        return BackupDatabaseAsync(databasePath, true, cancellationToken);
    }

    public Task RemoveDatabaseBackupAsync()
    {
        var path = GetOrCreateBackupDirectoryPath();
        return RemoveDatabaseBackupAsync(path);
    }

    public Task RemoveAllDatabaseBackupsAsync()
    {
        var path = _appDirectory.DbBackup;
        return RemoveDatabaseBackupAsync(path);
    }

    public string GetOrCreateBackupDirectoryPath()
    {
        var path = Path.Combine(_appDirectory.DbBackup, _tempKey);

        if (Directory.Exists(path) is false)
        {
            Directory.CreateDirectory(path);
        }

        return path;
    }

    public bool DatabaseExists(string databasePath)
    {
        if (Directory.Exists(databasePath) is false)
        {
            return false;
        }

        foreach (var bucket in _buckets)
        {
            if (BucketExists(databasePath, bucket.Key))
            {
                return true;
            }
        }

        return false;
    }

    public bool BucketExists(string databasePath, Bucket bucket)
    {
        var bucketPath = GetBucketPath(databasePath, bucket);
        return Directory.Exists(bucketPath);
    }

    public Task<List<Guid>> GetLocalProjectIdsAsync(string databasePath)
    {
        var configuration = new DatabaseConfiguration() 
        { 
            Directory = databasePath 
        };

        var sourceDatabase = new Database(Bucket.localonlydata.ToString(), configuration);

        return Task.Run(() =>
        {
            var projectIds = new List<Guid>();

            sourceDatabase.InBatch(() =>
            {
                using var result = QueryBuilder.Select(SelectResult.Expression(Meta.ID))
                                               .From(DataSource.Collection(sourceDatabase?.GetDefaultCollection()!));

                foreach (var row in result.Execute())
                {
                    var documentId = row.GetString(0);
                    if (documentId is null) 
                    { 
                        continue; 
                    }

                    var document = sourceDatabase?.GetDefaultCollection()?.GetDocument(documentId);
                    if (document?["Type"].String != nameof(LocalProjects))
                    {
                        continue;
                    }

                    var localProjects = document.GetArray("Projects");
                    if (localProjects is null)
                    {
                        continue;
                    }

                    foreach (var localProjectDictionary in localProjects)
                    {
                        var localProject = (DictionaryObject)localProjectDictionary;
                        var projectId = localProject["ProjectId"].String;

                        if (Guid.TryParse(projectId, out var guidProjectId))
                        {
                            projectIds.Add(guidProjectId);
                        }
                    }
                }
            });

            sourceDatabase.Close();
            return projectIds;
        });
    }

    private Task<string> BackupDatabaseAsync(string databasePath, bool reverse, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return null;
        }

        var sourcePath = reverse ? GetOrCreateBackupDirectoryPath() : databasePath;
        var targetPath = reverse ? databasePath : GetOrCreateBackupDirectoryPath();

        return Task.Run(() =>
        {
            foreach (var bucket in _buckets)
            {
                CopyDirectory(
                    sourcePath: GetBucketPath(sourcePath, bucket.Key),
                    destinationPath: GetBucketPath(targetPath, bucket.Key),
                    recursive: true,
                    cancellationToken: cancellationToken);
            }

            return targetPath;
        });
    }

    private Task RemoveDatabaseBackupAsync(string path)
    {
        return Task.Run(() =>
        {
            if (Directory.Exists(path))
            {
                foreach (var filePath in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
                {
                    File.SetAttributes(filePath, FileAttributes.Normal);
                }

                Directory.Delete(path, true);
            }
        });
    }

    private string GetBucketPath(string databasePath, Bucket bucket)
    {
        return Path.Combine(databasePath, _buckets[bucket]);
    }

    private static void CopyDirectory(string sourcePath,
        string destinationPath,
        bool recursive,
        CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }

        var sourceDirectory = new DirectoryInfo(sourcePath);

        if (sourceDirectory.Exists is false)
        {
            return;
        }

        var directories = sourceDirectory.GetDirectories();

        Directory.CreateDirectory(destinationPath);

        foreach (var file in sourceDirectory.GetFiles())
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            var targetFilePath = Path.Combine(destinationPath, file.Name);
            file.CopyTo(targetFilePath, true);

            File.SetAttributes(targetFilePath, FileAttributes.Normal);
        }

        if (recursive)
        {
            foreach (var subDir in directories)
            {
                var newDestinationDir = Path.Combine(destinationPath, subDir.Name);
                CopyDirectory(subDir.FullName, newDestinationDir, true);
            }
        }
    }
}