using System.Net;
using System.Globalization;
using Couchbase.Lite;
using Couchbase.Lite.Sync;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Render.Interfaces;
using Render.Repositories.Kernel;

namespace Render.Services.SyncService
{
    /// <summary>
    /// Replicates Json Documents between a source (Local DB) and a target (Sync Gateway)
    /// </summary>
    public class ReplicatorWrapper : ReactiveObject, IReplicatorWrapper, IDisposable
    {
        private readonly string _databasePath;
        private readonly IRenderLogger _logger;

        private string _connectionString;
        private int _maxSyncAttempts;
        private bool _pushOnly;
        private string _syncGatewayUsername;
        private string _syncGatewayPassword;
        private Replicator _webReplicator;
        private ListenerToken _webListenerToken;
        private List<string> _projectChannels;
        private Database _localDatabase;
        private ReplicatorConfiguration _webConfig;

        //General sync usages
        [Reactive] public ReplicatorStatus CurrentStatus { get; set; }

        [Reactive] public bool Completed { get; private set; }

        [Reactive] public bool Error { get; private set; }

        public int TotalToSync { get; private set; }

        public int TotalSynced { get; private set; }

        /// <summary>
        /// Constructor: give this app instance an "Id" to make sure we don't try to sync to ourselves, and a default
        /// status of "Offline".
        /// </summary>
        public ReplicatorWrapper(IRenderLogger logger, string databasePath = null)
        {
            _logger = logger;
            _databasePath = databasePath;
        }

        /// <summary>
        /// Set up the replicator for all cases, both local and web.
        /// </summary>
        /// <param name="databaseName">The name of the database for this replicator to connect to locally.</param>
        /// <param name="connectionString">The connection string for web replication (including web socket protocol, uri, and port)</param>
        /// <param name="userId">Username to authenticate web sync.</param>
        /// <param name="projectIds">Project Ids the logged in user is permitted to sync.</param>
        public void Configure(string databaseName,
            string connectionString,
            int maxSyncAttempts,
            string syncGatewayUsername,
            string syncGatewayPassword,
            List<string> projectIds = null)
        {
            if (_localDatabase == null)
            {
                var configuration = _databasePath is not null ? new DatabaseConfiguration { Directory = _databasePath } : null;
                _localDatabase = new Database(databaseName, configuration);
            }

            _pushOnly = databaseName == Buckets.logs.ToString();
            _connectionString = connectionString;
            _maxSyncAttempts = maxSyncAttempts;
            _projectChannels = projectIds?.Count > 0 ? projectIds : null;
            _syncGatewayUsername = syncGatewayUsername;
            _syncGatewayPassword = syncGatewayPassword;
        }

        /// <summary>
        /// Begin sync, targeting either a local mesh network or the web.
        /// </summary>
        public void StartSync()
        {
            if (CurrentStatus.Activity == ReplicatorActivityLevel.Busy)
            {
                _logger.LogInfo("Skipped replication on database", new Dictionary<string, string>
                {
                    { "Database", _localDatabase.Name }
                });
                return;
            }

            InitializeWebReplicatorAsync(_pushOnly ? ReplicatorType.Push : ReplicatorType.PushAndPull, false);
        }

        public void StartDownload(List<string> channels, bool freshDownload = false)
        {
            _projectChannels = channels;
            InitializeWebReplicatorAsync(ReplicatorType.Pull, false, freshDownload);

            _logger.LogInfo("Download channels started", new Dictionary<string, string>
            {
                { "Database", _localDatabase.Name },
                { "Channels", string.Join(",", channels) },
                { "Time", DateTime.UtcNow.ToString(CultureInfo.InvariantCulture) }
            });
        }

        private void InitializeWebReplicatorAsync(ReplicatorType replicatorType, bool continuous, bool freshDownload = false)
        {
            if (_localDatabase == null)
            {
                return;
            }

            using var password = new NetworkCredential("", _syncGatewayPassword).SecurePassword;
            var target = new URLEndpoint(new Uri($"{_connectionString}{_localDatabase.Name}/"));
            _webConfig = new ReplicatorConfiguration(target)
            {
                Authenticator = new BasicAuthenticator(_syncGatewayUsername, password),
                Continuous = continuous,
                ReplicatorType = replicatorType,
                Heartbeat = TimeSpan.FromMinutes(30),
                MaxAttempts = _maxSyncAttempts,
                //If we are only pushing (for the logs bucket), we don't want to replicate any deletions.
                //This allows us to manage how many logs we keep locally without removing them from the web servers,
                //giving us flexibility for our analytics.
            };

            var defCollectionConf = new CollectionConfiguration()
            {
                Channels = _projectChannels,
                ConflictResolver = CustomConflictResolver.Instance,
            };
            _webConfig.AddCollection(_localDatabase.GetDefaultCollection(), defCollectionConf);

			Completed = false;
            Error = false;

            _webReplicator?.RemoveChangeListener(_webListenerToken);
            _webReplicator = new Replicator(_webConfig);
            _webListenerToken = _webReplicator.AddChangeListener(TaskScheduler.Default, (sender, args) =>
            {
                CurrentStatus = args.Status;
                
                if (args.Status.Error != null)
                {
                    _logger.LogInfo("Web replication failed", new Dictionary<string, string>
                    {
                        { "Database", _localDatabase.Name },
                        { "Time", DateTime.UtcNow.ToString(CultureInfo.InvariantCulture) },
                        { "Connection String", _connectionString },
                        { "Exception", args.Status.Error.Message },
                        { "SyncGatewayUsername", _syncGatewayUsername },
                        { "SyncGatewayPassword (partial)", _syncGatewayPassword.Substring(0, 5) },
                        { "Project Channels", string.Join(",", _projectChannels) },
                    });
                    
                    _webReplicator.Stop();
                    Error = true;
                }
                else if (CurrentStatus.Activity == ReplicatorActivityLevel.Stopped)
                {
                    TotalToSync = (int)args.Status.Progress.Total;
                    TotalSynced = (int)args.Status.Progress.Completed;
                    if (TotalSynced == TotalToSync)
                    {
                        _logger.LogInfo("Web replication completed", new Dictionary<string, string>
                        {
                            { "Database", _localDatabase.Name },
                            { "Time", DateTime.UtcNow.ToString(CultureInfo.InvariantCulture) },
                            { "Total Synced", TotalSynced.ToString() },
                            { "Total to Sync", TotalToSync.ToString() },
                            { "SyncGatewayUsername", _syncGatewayUsername },
                        });
                        
                        Completed = true;
                        Error = false;
                    }
                    //This code will only be hit if you're downloading (admin download or project download)
                    else if (TotalSynced != TotalToSync && replicatorType == ReplicatorType.Pull && !continuous)
                    {
                        _logger.LogInfo("Web replication error state", new Dictionary<string, string>
                        {
                            { "Database", _localDatabase.Name },
                            { "Time", DateTime.UtcNow.ToString(CultureInfo.InvariantCulture) },
                            { "Total Synced", TotalSynced.ToString() },
                            { "Total to Sync", TotalToSync.ToString() },
                            { "SyncGatewayUsername", _syncGatewayUsername },
                        });
                        
                        _webReplicator.Stop();
                        Error = true;
                    }
                }
            });

            _webReplicator.Start(freshDownload);
            _logger.LogInfo("Web replication started", new Dictionary<string, string>
            {
                { "Database", _localDatabase.Name },
                { "Time", DateTime.UtcNow.ToString(CultureInfo.InvariantCulture) },
                { "Sync Gateway username", _syncGatewayUsername },
                { "Connection String", _connectionString },
            });
        }

        public void Stop()
        {
            StopWebSync();
        }

        /// <summary>
        /// Stops a running replicator. This method returns immediately; when the replicator
        /// actually stops, the replicator will change its status's activity level to Couchbase.Lite.Sync.ReplicatorActivityLevel.Stopped
        /// and the replicator change notification will be notified accordingly. Therefore, we should not unsubscribe change listener here.
        /// </summary>
        private void StopWebSync()
        {
            _webReplicator.Stop();
        }

        public void Dispose()
        {
            if (_webReplicator != null)
            {
                _webReplicator.RemoveChangeListener(_webListenerToken);
                _webReplicator.Stop();
                _webReplicator.Dispose();
            }
        }
    }
}