using Couchbase.Lite;
using Couchbase.Lite.Query;
using Render.Interfaces;
using Render.Repositories.Kernel;
using Render.TempFromVessel.Kernel;

namespace Render.Services.SyncService;

public class AudioIntegrityService(IDatabaseWrapper databaseWrapper, IRenderLogger logger) : IAudioIntegrityService
{
    public async Task<bool> IsProjectContainAudioLoss(Guid projectId)
    {
        var audioList = await GetBlobsForDatabase(projectId);
        var result = false;
        foreach (var audio in audioList.Where(audioBlob => audioBlob.Blob.Content is null || audioBlob.Blob.Length != audioBlob.Blob.Content.Length
                                                                                          || audioBlob.Blob.Length == 0))
        {
            logger.LogInfo("Audio Loss Detected: Audio is empty ", new Dictionary<string, string>()
            {
                { "ProjectId", projectId.ToString() },
                { "AudioType", audio.EntityType },
                { "AudioId", audio.EntityId.ToString() },
            });
            result = true;
        }

        foreach (var audio in audioList.Where(blob => blob.Blob.Digest != ComputeDigest(blob.Blob.Content)))
        {
            logger.LogInfo("Audio Loss Detected: Checksum is not valid", new Dictionary<string, string>()
            {
                { "ProjectId", projectId.ToString() },
                { "AudioType", audio.EntityType },
                { "AudioId", audio.EntityId.ToString() },
            });
            result = true;
        }

        return result;
    }

	//The project size is calculated as a sum of all project audios in the local DB.
	public async Task<int> CalculateProjectSize(Guid projectId)
    {
		var audioList = await GetBlobsForDatabase(projectId);
        var result = audioList.GroupBy(digest => digest.Blob.Digest).Select(blob => blob.First().Blob.Length).Sum(x => x);
		return result;
	}

    private async Task<List<AudioInfo>> GetBlobsForDatabase(Guid projectId)
    {
        var listOfAudios = new List<AudioInfo>();
        string entityId;
        var queryResults = await Task.Run(() =>
        {
            databaseWrapper.InBatch(() =>
            {
                var result = QueryBuilder.Select(
                        SelectResult.Property(nameof(DomainEntity.Id)),
                        SelectResult.Property(nameof(DomainEntity.Type)),
                        SelectResult.Expression(Expression.Property("_attachments")))
                    .From(databaseWrapper.GetDataSource())
                    .Where(Expression.Property("ProjectId").EqualTo(Expression.String(projectId.ToString())));
                foreach (var row in result.Execute())
                {
                    entityId = row.GetString(0);
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
            });
            return listOfAudios;
        });
        return queryResults;
    }

    public string ComputeDigest(byte[] data)
    {
        byte[] hash;
        using (var sha1 = System.Security.Cryptography.SHA1.Create())
        {
            hash = sha1.ComputeHash(data);
        }

        var base64Hash = Convert.ToBase64String(hash);

        // Format the result
        return $"sha1-{base64Hash}";
    }
}

public class AudioInfo
{
    public Blob Blob { get; set; }
    public string EntityType { get; set; }
    public Guid EntityId { get; set; }
}