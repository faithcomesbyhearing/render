using Couchbase.Lite;
using Couchbase.Lite.Query;
using Render.Interfaces;
using Render.Repositories.Extensions;
using Render.Repositories.Kernel;
using Render.TempFromVessel.Kernel;

namespace Render.Services.SyncService;

public class AudioIntegrityService(IDatabaseWrapper databaseWrapper, IRenderLogger logger) : IAudioIntegrityService
{
    public async Task<bool> DoesProjectContainAudioLoss(Guid id)
    {
        var audioList = await GetAudioListAsync(projectId: id);
        return IsCollectionContainsAudioLoss(audioList, id);
    }

    public async Task<bool> DoesLocalDatabaseContainAudioLoss()
    {
        var audioList = await GetAudioListAsync();
        return IsCollectionContainsAudioLoss(audioList);
    }

    public async Task<bool?> DoesRemoteBucketContainAudioLoss(string sourceDatabasePath)
    {
        if (sourceDatabasePath.IsNullOrEmpty()) 
        { 
            return true; 
        }

        try
        {
            var sourceDatabaseConfiguration = new DatabaseConfiguration() { Directory = sourceDatabasePath };
            using var sourceDatabase = new Database(Buckets.renderaudio.ToString(), sourceDatabaseConfiguration);
        
            var audioList = await GetAudioListAsync(sourceDatabase);
            return IsCollectionContainsAudioLoss(audioList);
        }
        catch (Exception ex) 
        { 
            logger.LogError(ex);
            return null;
        }
    }

    private bool IsCollectionContainsAudioLoss(List<AudioInfo> audioList, Guid projectId = default)
    {
        foreach (var audio in audioList)
        {
            var content = audio.Blob.Content;
            var length = audio.Blob.Length;

            if(content is null || length is 0 || content.Length != length) // not valid blob
            {
                logger.LogInfo("Audio Loss Detected: Audio is empty ", new Dictionary<string, string>()
                {
                    { "ProjectId", projectId.ToString() },
                    { "AudioType", audio.EntityType },
                    { "AudioId", audio.EntityId.ToString() },
                });

                return true;
            }

            if(content is null || audio.Blob.Digest != ComputeDigest(content))
            {
                logger.LogInfo("Audio Loss Detected: Checksum is not valid", new Dictionary<string, string>()
                {
                    { "ProjectId", projectId.ToString() },
                    { "AudioType", audio.EntityType },
                    { "AudioId", audio.EntityId.ToString() },
                });

                return true;
            }
        }

        return false;
    }

    //The project size is calculated as a sum of all project audios in the local DB.
    public async Task<int> CalculateProjectSize(Guid projectId)
    {
        var audioList = await GetAudioListAsync(projectId: projectId);
        var result = audioList.GroupBy(digest => digest.Blob.Digest).Select(blob => blob.First().Blob.Length).Sum(x => x);
        return result;
    }
    
    public string ComputeDigest(byte[] data)
    {
        var hash = System.Security.Cryptography.SHA1.HashData(data);

        var base64Hash = Convert.ToBase64String(hash);

        // Format the result
        return $"sha1-{base64Hash}";
    }
    
    private async Task<List<AudioInfo>> GetAudioListAsync(Database database = null, Guid? projectId = default)
    {
        var listOfAudios = new List<AudioInfo>();

        await Task.Run(() =>
        {
            if (database is not null)
            {
                database.InBatch(() => { listOfAudios = GetBlobs(DataSource.Collection(database?.GetDefaultCollection()!), projectId); });
            }
            else
            {
                databaseWrapper?.InBatch(() => { listOfAudios = GetBlobs(databaseWrapper.GetDataSource(), projectId); });
            }

            return listOfAudios;
        });
        
        return listOfAudios;
    }

    private static List<AudioInfo> GetBlobs(IDataSourceAs sourceDatabase, Guid? projectId = default)
    {
        var listOfAudios = new List<AudioInfo>();

        var result = QueryBuilder.Select(
                SelectResult.Property(nameof(DomainEntity.Id)),
                SelectResult.Property(nameof(DomainEntity.Type)),
                SelectResult.Expression(Expression.Property("_attachments")))
            .From(sourceDatabase);

        var resultSet = projectId != default ? result.Where(Expression.Property("ProjectId").EqualTo(Expression.String(projectId.ToString()))).Execute() : result.Execute();  
        
        foreach (var row in resultSet)
        {
            var entityId = row.GetString(0);
            var entityType = row.GetString(1);
            var attachments = row.GetDictionary(2);
            if (attachments == null || !attachments.Any()) continue;

            var blob = attachments.GetBlob("blob_/audio") ?? attachments.GetBlob("blob_/blob_~1audio");

            if (entityId != null && blob != null)
            {
                var blobInfo = new AudioInfo() { EntityId = Guid.Parse(entityId), Blob = blob, EntityType = entityType };
                listOfAudios.Add(blobInfo);
            }
        }

        return listOfAudios;
    }
}

public class AudioInfo
{
    public Blob Blob { get; set; }
    public string EntityType { get; set; }
    public Guid EntityId { get; set; }
}