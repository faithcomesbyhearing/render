namespace Render.Kernel.WrappersAndExtensions;

public interface IAudioLossRetryDownloadService
{
    Task<AudioLossRetryDownloadStatus> RetryDownloadIfAudioLoss(Func<Task> downloadProject, Guid projectId, int retryCount);
}