namespace Render.Services.SyncService
{
    public interface IOneShotReplicator : IDisposable
    {
        /// <summary>
        /// Occurs when the download operation completes.
        /// The result bool parameter indicates whether the download completed successfully (true) or not (false).
        /// </summary>
        event Action<bool> DownloadFinished;

        void StartDownload(bool freshDownload = false);

        void StopDownload();
    }
}