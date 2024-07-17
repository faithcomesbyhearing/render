using System.Globalization;
using ReactiveUI;
using Render.Repositories.Kernel;
using Render.Interfaces;

namespace Render.Services.SyncService
{
    public class OneShotAdminDownloader : ReactiveObject, IOneShotReplicator
    {
        private readonly IRenderLogger _logger;
        private readonly string _syncGatewayUsername;
        private readonly List<IDisposable> _disposables = new();
        private readonly IReplicatorWrapper _renderAdminReplicatorWrapper;

        private List<string> Channels { get; }

        public event Action<bool> DownloadFinished;

        public void StartDownload(bool freshDownload = false)
        {
            _logger.LogInfo("OneShot Admin Downloader starting", new Dictionary<string, string>
            {
                { "SyncGatewayUsername", _syncGatewayUsername },
                { "Channels", string.Join(",", Channels) },
                { "Time", DateTime.UtcNow.ToString(CultureInfo.InvariantCulture) }
            });

            _renderAdminReplicatorWrapper.StartDownload(Channels);
        }

        public void StopDownload()
        {
            _renderAdminReplicatorWrapper.Stop();
        }

        public OneShotAdminDownloader(
            IRenderLogger logger,
            string connectionString,
            int maxSyncAttempts,
            string syncGatewayUsername,
            string syncGatewayPassword,
            string databasePath)
        {
            _logger = logger;
            _syncGatewayUsername = syncGatewayUsername;
            Channels = new List<string> { syncGatewayUsername, "admin" };

            _renderAdminReplicatorWrapper = new ReplicatorWrapper(logger, databasePath);
            _renderAdminReplicatorWrapper.Configure(
                databaseName: Buckets.render.ToString(),
                connectionString: connectionString,
                maxSyncAttempts: maxSyncAttempts,
                syncGatewayUsername: syncGatewayUsername,
                syncGatewayPassword: syncGatewayPassword);

            _disposables.Add(_renderAdminReplicatorWrapper
                .WhenAnyValue(x => x.Completed)
                .Subscribe(completed => { DownloadFinished?.Invoke(completed); }));
        }

        public void Dispose()
        {
            foreach (var item in _disposables)
            {
                item.Dispose();
            }
            
            DownloadFinished = null;
        }
    }
}