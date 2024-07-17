using System.Globalization;
using Render.Interfaces;
using Timer = System.Timers.Timer;

namespace Render.Services.SyncService;

public class OneShotReplicatorRetryProxy : IOneShotReplicator
{
    // The minimum interval (in seconds) after which the Retry logic starts to use RetryIntervals from the begging.
    private const int MinimumDownloadingIntervalBeforeNewRetries = 10;
    
    // Intervals (in seconds) for each retry attempt when the Internet connection is lost.
    // After the last interval value from the collection is used the Retry logic stops.
    private static readonly int[] RetryIntervals = { 1, 30, 60, 90, 150 };
    
    private int _retryIntervalsCurrentIndex;
    private int _totalRetries;
    private DateTime _firstDownloadStarted;
    private DateTime _retryDownloadStarted;
    private Timer _retryTimer;

    private readonly Func<IOneShotReplicator> _oneShotReplicatorFactory;
    private IOneShotReplicator _replicator;
    private readonly IRenderLogger _logger;
    private readonly Guid _projectId;

    public event Action<bool> DownloadFinished;

    public OneShotReplicatorRetryProxy(IRenderLogger logger, Guid projectId, Func<IOneShotReplicator> oneShotReplicatorFactory)
    {
        _logger = logger;
        _projectId = projectId;
        _oneShotReplicatorFactory = oneShotReplicatorFactory;
        _replicator = oneShotReplicatorFactory();
        _replicator.DownloadFinished += ReplicatorOnDownloadFinished;
    }

    public void StartDownload(bool freshDownload = false)
    {
        _replicator.StartDownload(freshDownload);
        _firstDownloadStarted = DateTime.Now;
    }

    public void StopDownload()
    {
        _replicator.StopDownload();
    }

    private void ReplicatorOnDownloadFinished(bool success)
    {
        if (success) // Downloaded successfully. No retry needed. 
        {
            if (_totalRetries > 0)
            {
                _logger.LogInfo("Download completed after several retry attempts", new Dictionary<string, string>
                {
                    { "Project Id", _projectId.ToString() },
                    { "Total Retries", _totalRetries.ToString() },
                    { "Download time (seconds)", GetDownloadTimeInSeconds() },
                });
            }

            DownloadFinished?.Invoke(true);
            return;
        }

        _replicator.DownloadFinished -= ReplicatorOnDownloadFinished;
        _replicator.Dispose();

        // Retry download
        if (TryGetNextRetryInterval(out var interval))
        {
            _retryTimer?.Stop();
            _retryTimer?.Dispose();

            _retryTimer = new Timer();
            _retryTimer.Interval = interval * 1000;
            _retryTimer.Elapsed += (_, _) =>
            {
                _retryTimer.Stop();

                _replicator = _oneShotReplicatorFactory();
                _replicator.DownloadFinished += ReplicatorOnDownloadFinished;
                _replicator.StartDownload();
                _retryDownloadStarted = DateTime.Now;
                _totalRetries++;

                _logger.LogInfo("Download failed. Retrying to resume downloading", new Dictionary<string, string>
                {
                    { "Project Id", _projectId.ToString() },
                    { "Retried after (seconds)", interval.ToString(CultureInfo.InvariantCulture) },
                });
            };

            _retryTimer.Start();
        }
        else // Number of retry attempts exceeded. Raise download failed event.
        {
            if (_totalRetries > 0)
            {
                _logger.LogInfo("Download failed after several retry attempts", new Dictionary<string, string>
                {
                    { "Project Id", _projectId.ToString() },
                    { "Total Retries", _totalRetries.ToString() },
                    { "Download time (seconds)", GetDownloadTimeInSeconds() },
                });
            }

            DownloadFinished?.Invoke(false);
        }
    }

    private string GetDownloadTimeInSeconds()
    {
        return ((int)(DateTime.Now - _firstDownloadStarted).TotalSeconds).ToString(CultureInfo.InvariantCulture);
    }

    private bool TryGetNextRetryInterval(out double interval)
    {
        // If downloading continues without errors during the MinimumDownloadingIntervalBeforeNewRetries interval,
        // retry logic should be started from the beginning.
        if ((DateTime.Now - _retryDownloadStarted).TotalSeconds > MinimumDownloadingIntervalBeforeNewRetries)
        {
            _retryIntervalsCurrentIndex = 0;
        }

        // Number of retry attempts exceeded
        if (_retryIntervalsCurrentIndex > RetryIntervals.Length - 1)
        {
            interval = -1;
            return false;
        }

        interval = RetryIntervals[_retryIntervalsCurrentIndex];
        _retryIntervalsCurrentIndex++;
        return true;
    }

    public void Dispose()
    {
        _retryTimer?.Stop();
        _retryTimer?.Dispose();
        
        _replicator.DownloadFinished -= ReplicatorOnDownloadFinished;
        _replicator.Dispose();

        DownloadFinished = null;
    }
}