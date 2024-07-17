namespace Render.Services.SyncService;

public interface IAudioIntegrityService
{
    Task<bool> IsProjectContainAudioLoss(Guid projectId);

    string ComputeDigest(byte[] data);

    Task<int> CalculateProjectSize(Guid projectId);
}