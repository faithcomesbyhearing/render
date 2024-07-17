using Render.Kernel.WrappersAndExtensions;
using Render.Resources;
using Render.Resources.Localization;
using Render.Services.SyncService;

namespace Render.Kernel;

public class AudioLossRetryDownloadStatus
{
    public bool AudioIsBroken { get; set; }
    public bool AutomaticRetryCompleted { get; set; }
}

public class AudioLossRetryDownloadService : IAudioLossRetryDownloadService
{
    private readonly IOffloadService _offloadService;
    private readonly IAudioIntegrityService _audioIntegrityService;
    private readonly IModalService _modalManager;

    private int _currentCountOfTries;

    public AudioLossRetryDownloadService(
        IOffloadService offloadService,
        IAudioIntegrityService integrityService,
        IModalService modalManager)
    {
        _offloadService = offloadService;
        _audioIntegrityService = integrityService;
        _modalManager = modalManager;
    }

    public async Task<AudioLossRetryDownloadStatus> RetryDownloadIfAudioLoss(Func<Task> downloadProject, Guid projectId, int retryCount)
    {
        var retryDownloadStatusResult = new AudioLossRetryDownloadStatus();

        if (await _audioIntegrityService.IsProjectContainAudioLoss(projectId) is false)
        {
            retryDownloadStatusResult.AudioIsBroken = false;
            return retryDownloadStatusResult;
        }
        
        retryDownloadStatusResult.AudioIsBroken = true;

        if (_currentCountOfTries < retryCount)
        {
            await _offloadService.OffloadProject(projectId);
            _currentCountOfTries++;
            await downloadProject.Invoke();
            return retryDownloadStatusResult;
        }

        await ShowAudioDataLossModal();
        retryDownloadStatusResult.AutomaticRetryCompleted = true;
        _currentCountOfTries = 0;
        return retryDownloadStatusResult;
    }

    private async Task ShowAudioDataLossModal()
    {
        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            await _modalManager.ShowInfoModal(Icon.OffloadItem,
                AppResources.CannotLoadAudio,
                AppResources.SyncAndTryAgain);
        });
    }
}