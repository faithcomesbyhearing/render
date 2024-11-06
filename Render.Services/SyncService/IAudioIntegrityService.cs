namespace Render.Services.SyncService;

public interface IAudioIntegrityService
{
    Task<bool> DoesProjectContainAudioLoss(Guid id);
    
    Task<bool> DoesLocalDatabaseContainAudioLoss();
    
    Task<bool?> DoesRemoteBucketContainAudioLoss(string sourceDatabasePath);

    string ComputeDigest(byte[] data);

    Task<int> CalculateProjectSize(Guid projectId);
}