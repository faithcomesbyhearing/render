using System.Reflection;
using Couchbase.Lite.Sync;
using Microsoft.Extensions.Configuration;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Render.Repositories.Kernel;
using Render.Interfaces.EssentialsWrappers;
using Render.TempFromVessel.Kernel;
using System.Reactive.Linq;
using Render.Repositories.UserRepositories;
using Render.Models.Users;
using Render.Interfaces;

namespace Render.Services.SyncService
{
    public class SyncService : ReactiveObject, ISyncService, IDisposable
    {
        private const int MinutesBetweenSync = 5;

        private List<string> _permittedProjectStrings = new List<string>();
        private List<string> _permittedGlobalUserStrings = new List<string>();

        private readonly List<IDisposable> _disposables = new List<IDisposable>();
        private readonly IRenderLogger _logger;
        private readonly IUserRepository _userRepository;

        private IReplicatorWrapper RenderReplicator { get; }
        private IReplicatorWrapper RenderProjectsReplicator { get; }
        private IReplicatorWrapper AudioReplicator { get; }
        private IEssentialsWrapper EssentialsWrapper { get; }
        private IDateTimeWrapper DateTimeWrapper { get; }
        private Timer SyncTimer { get; set; }

        [Reactive]
        public CurrentSyncStatus CurrentSyncStatus { get; private set; } = CurrentSyncStatus.NotStarted;
        
        public string ConnectionString { get; }
        public int MaxSyncAttempts { get; }
        public string SyncGatewayUsername { get; private set; }
        public string SyncGatewayPassword { get; private set; }

        public SyncService(
            IAppSettings appSettings,
            IRenderLogger logger,
            IUserRepository userRepository,
            IReplicatorWrapper renderReplicator,
            IReplicatorWrapper audioReplicator,
            IReplicatorWrapper renderProjectsReplicator,
            IEssentialsWrapper essentialsWrapper,
            IDateTimeWrapper dateTimeWrapper)
        {
            _logger = logger;
            _userRepository = userRepository;

            RenderReplicator = renderReplicator;
            AudioReplicator = audioReplicator;
            EssentialsWrapper = essentialsWrapper;
            DateTimeWrapper = dateTimeWrapper;
            RenderProjectsReplicator = renderProjectsReplicator;

            //Set up listeners to respond to when the sync status changes
            _disposables.Add(this
                .WhenAnyValue(x => x.RenderReplicator.CurrentStatus)
                .Subscribe(SetCurrentReplicationStatus));

            _disposables.Add(this
                .WhenAnyValue(x => x.AudioReplicator.CurrentStatus)
                .Subscribe(SetCurrentReplicationStatus));

            _disposables.Add(this
                .WhenAnyValue(x => x.RenderProjectsReplicator.CurrentStatus)
                .Subscribe(SetCurrentReplicationStatus));

            _disposables.Add(this
                .WhenAnyValue(x => x.RenderReplicator.CurrentStatus.Activity)
                .Where(status => status is ReplicatorActivityLevel.Stopped)
                .Subscribe(ClearAdminCredentialsForRenderUser));

            ConnectionString = $"{appSettings.WebSocketProtocol}://{appSettings.CouchbaseReplicationUri}:{appSettings.ReplicationPort}/";
            MaxSyncAttempts = appSettings.CouchbaseMaxSyncAttempts;
        }

        public void StartAllSync()
        {
           StartAllSync(_permittedProjectStrings, _permittedGlobalUserStrings);
        }

        public void StartAllSync(
            List<Guid> projectIds,
            List<Guid> globalUserIds,
            string syncGatewayUsername,
            string syncGatewayPassword,
            bool startTimer = true)
        {
            //Only replace the credentials if we actually passed something useful in
            if (!string.IsNullOrEmpty(syncGatewayUsername) && !string.IsNullOrEmpty(syncGatewayPassword))
            {
                SyncGatewayUsername = syncGatewayUsername;
                SyncGatewayPassword = syncGatewayPassword;
            }

            var projectIdStrings = projectIds.Select(x => x.ToString()).ToList();
            var globalUserIdStrings = globalUserIds.Select(x => x.ToString()).ToList();

            StartAllSync(projectIdStrings, globalUserIdStrings, startTimer);
        }

        private void StartAllSync(List<string> projectIds, List<string> globalUserIds, bool startTimer = true)
        {
            if (string.IsNullOrEmpty(SyncGatewayUsername) || string.IsNullOrEmpty(SyncGatewayPassword))
            {
                return;
            }

            if (SyncTimer == null)
            {
                var secondsToNextSync = StartSyncTimer();
                
                if (secondsToNextSync > 20)
                {
                    SyncNow(projectIds, globalUserIds);
                }
            }
            else
            {
                SyncNow(projectIds, globalUserIds);
            }
            
            _permittedProjectStrings = projectIds;
            _permittedGlobalUserStrings = globalUserIds;
        }

        private double StartSyncTimer()
        {
            var currentTime = DateTimeWrapper.GetTimeOfDay();
            var minutes = currentTime.Minutes;
            var minutesToAdd = 1;

            while ((minutes + minutesToAdd) % MinutesBetweenSync != 0)
            {
                minutesToAdd++;
            }

            var secondsToAdd = minutesToAdd * 60 - currentTime.Seconds;
            var millisecondsToAdd = Math.Max(1, secondsToAdd * 1000 - currentTime.Milliseconds);
            var timeSpanToAdd = TimeSpan.FromMilliseconds(millisecondsToAdd);

            SyncTimer = new Timer(TimerSyncCallback, null, timeSpanToAdd, TimeSpan.FromMinutes(MinutesBetweenSync));
            
            _logger.LogInfo("All sync started on timer", new Dictionary<string, string>
            {
                {"Interval", MinutesBetweenSync.ToString()},
                {"Delay", timeSpanToAdd.ToString()}
            });

            return timeSpanToAdd.TotalSeconds;
        }

        private void SyncNow(List<string> projectIdStrings, List<string> globalUserIdStrings)
        {
            //If we have no projects and are trying to sync, just sync the admin/logs bucket
            if (projectIdStrings.Count == 0)
            {
                _logger.LogInfo("Sync was started with no projects to sync.");
                RenderReplicator.StartSync();
            }
            //If our projectIds have updated since last sync, reconfigure the replicators
            else if (!projectIdStrings.SequenceEqual(_permittedProjectStrings) || 
                     !globalUserIdStrings.SequenceEqual(_permittedGlobalUserStrings))
            {
                _logger.LogInfo("The project list changed. Sync was reconfigured and started");
                TrySync(projectIdStrings, globalUserIdStrings);
            }
            else
            {
                _logger.LogInfo("All replicators started with previous configuration.");
                RenderReplicator.StartSync();
                RenderProjectsReplicator.StartSync();
                AudioReplicator.StartSync();
            }
            
            _permittedProjectStrings = projectIdStrings;
            _permittedGlobalUserStrings = globalUserIdStrings;
        }
       
        //public void SyncNow(List<Guid> projectIds, List<Guid> globalUserIds)
        //{
        //    var projectIdsAsStrings = projectIds.Select(x => x.ToString()).ToList();
        //    var globalUserIdsAsStrings = globalUserIds.Select(x => x.ToString()).ToList();
            
        //    SyncNow(projectIdsAsStrings, globalUserIdsAsStrings);
        //}

        private void TrySync(List<string> permittedProjects, List<string> permittedGlobalUserIds = null)
        {
            RenderReplicator.Configure(nameof(Buckets.render),
                ConnectionString,
                MaxSyncAttempts,
                SyncGatewayUsername, 
                SyncGatewayPassword,
                new List<string>{SyncGatewayUsername, "admin"});

            RenderReplicator.StartSync();
            
            if (permittedProjects.Count > 0)
            {
                var renderProjectChannels = permittedGlobalUserIds != null && permittedGlobalUserIds.Any()
                    ? permittedProjects.Union(permittedGlobalUserIds).ToList()
                    : permittedProjects;
                
                RenderProjectsReplicator.Configure(
                    Buckets.render.ToString(),
                    ConnectionString,
                    MaxSyncAttempts,
                    SyncGatewayUsername,
                    SyncGatewayPassword,
                    renderProjectChannels);

                AudioReplicator.Configure(
                    Buckets.renderaudio.ToString(),
                    ConnectionString,
                    MaxSyncAttempts,
                    SyncGatewayUsername,
                    SyncGatewayPassword,
                    permittedProjects);

                RenderProjectsReplicator.StartSync();
                AudioReplicator.StartSync();
            }
        }

        public IOneShotReplicator GetAdminDownloader(
            string syncGatewayUsername,
            string syncGatewayPassword,
            string databasePath)
        {
            SyncGatewayUsername = syncGatewayUsername;
            SyncGatewayPassword = syncGatewayPassword;

            return new OneShotAdminDownloader(
                logger: _logger,
                connectionString: ConnectionString,
                maxSyncAttempts: MaxSyncAttempts,
                syncGatewayUsername: SyncGatewayUsername,
                syncGatewayPassword: SyncGatewayPassword,
                databasePath: databasePath);
        }

        public void StopAllSync()
        {
            SyncTimer?.Dispose();
            SyncTimer = null;

            RenderReplicator.Stop();
            RenderProjectsReplicator.Stop();
            AudioReplicator.Stop();
        }

        private void TimerSyncCallback(object state)
        {
            SyncNow(_permittedProjectStrings, _permittedGlobalUserStrings);
        }

        private void SetCurrentReplicationStatus(ReplicatorStatus activityLevel)
        {
            CurrentSyncStatus = GetCurrentReplicationStatus();

            _logger.LogInfo($"CurrentSyncStatus is {CurrentSyncStatus}");
        }

        /// <summary>
        /// Conglomerate the status of all of our replicators and determine our current sync state.
        /// </summary>
        private CurrentSyncStatus GetCurrentReplicationStatus()
        {
            if (RenderReplicator.CurrentStatus.Error != null
                || AudioReplicator.CurrentStatus.Error != null
                || RenderProjectsReplicator.CurrentStatus.Error != null)
            {
                return CurrentSyncStatus.ErrorEncountered;
            }

            var adminStatus = RenderReplicator.CurrentStatus.Activity;
            var audioStatus = AudioReplicator.CurrentStatus.Activity;
            var projectStatus = RenderProjectsReplicator.CurrentStatus.Activity;

            // if any of the replicators are starting, or currently syncing set the status to active
            if (adminStatus is ReplicatorActivityLevel.Busy ||
                adminStatus is ReplicatorActivityLevel.Connecting ||
                projectStatus is ReplicatorActivityLevel.Busy ||
                projectStatus is ReplicatorActivityLevel.Connecting ||
                audioStatus is ReplicatorActivityLevel.Busy ||
                audioStatus is ReplicatorActivityLevel.Connecting)
            {
                return CurrentSyncStatus.ActiveReplication;
            }

            // none of the replicators are actively syncing and at least one of them is online
            if (adminStatus is ReplicatorActivityLevel.Stopped ||
                projectStatus is ReplicatorActivityLevel.Stopped ||
                audioStatus is ReplicatorActivityLevel.Stopped)
            {
                return CurrentSyncStatus.Finished;
            }

            // all of the replicators are offline
            return CurrentSyncStatus.NotStarted;
        }

        private async void ClearAdminCredentialsForRenderUser(ReplicatorActivityLevel stoppedLevel)
        {
            var isGuid = Guid.TryParse(SyncGatewayUsername, out var result);
            if (!isGuid)
            {
                return;
            }

            var currentUser = await _userRepository.GetUserAsync(result);
            if (currentUser is null || currentUser?.UserType is UserType.Vessel)
            {
                return;
            }

            var renderUser = currentUser as RenderUser;
            var hasOwnCredentials = renderUser?.SyncGatewayLogin != null;
            var hasAdminCredentials = renderUser?.UserSyncCredentials != null;

            if (hasOwnCredentials && hasAdminCredentials)
            {
                renderUser.UserSyncCredentials = null;

                _ = _userRepository.SaveUserAsync(renderUser);

                _logger.LogInfo("Render user got his credentials. Admin credentials removed");
            }
        }

        public void Dispose()
        {
            StopAllSync();
            foreach (var disposable in _disposables)
            {
                disposable.Dispose();
            }
        }
    }
}