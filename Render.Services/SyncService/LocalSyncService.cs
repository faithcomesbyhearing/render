using System.Net;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Render.Interfaces;

namespace Render.Services.SyncService
{
    public enum CurrentSynchronizationRole
    {
        NotSet,
        Hub,
        ActiveReplicator
    }

    public class LocalSyncService : ReactiveObject, ILocalSyncService
    {
        [Reactive] public CurrentSyncStatus CurrentSyncStatus { get; private set; } = CurrentSyncStatus.NotStarted;

        private CurrentSynchronizationRole CurrentSyncRole { get; set; }

        private SemaphoreSlim ReplicationStatusSemaphoreSlim { get; set; } = new(1);

        private readonly IHandshakeService _handshakeService;
        private readonly ILocalReplicationService _localReplicationService;
        private readonly IRenderLogger _renderLogger;

        private bool _alreadyCancelled;

        public LocalSyncService(
            IHandshakeService handshakeService,
            ILocalReplicationService localReplicationService,
            IRenderLogger renderLogger)
        {
            _handshakeService = handshakeService;
            _localReplicationService = localReplicationService;
            _renderLogger = renderLogger;

            _localReplicationService.SubscribeOnSyncUpdateAction(SetCurrentReplicationStatus);
        }

        public async Task StartLocalSync(string username, Guid projectId)
        {
            if (_handshakeService.IsInHandshakeProcess())
            {
                return; // subsequent threads do nothing because the first set this
            }

            // prevent multiple replicators from starting eg upon multiple presses of sync button
            if (CurrentSyncRole != CurrentSynchronizationRole.NotSet) return;

            _renderLogger.LogInfo("Local synchronization has been started");

            CurrentSyncStatus = CurrentSyncStatus.Looking;

            var broadcastMessage = await _handshakeService.TryToFindBroadcastForSync(projectId);

            if (broadcastMessage is not null)
            {
                var hubToConnect = await
                    _handshakeService.StartToConnectToServer(broadcastMessage, ConnectionTask.SyncProject);

                if (hubToConnect is not null)
                {
                    BecomeAnActiveLocalReplicator(hubToConnect);
                    return;
                }
            }

            BecomeAHub(projectId, username);
        }

        public void StopLocalSync()
        {
            switch (CurrentSyncRole)
            {
                case CurrentSynchronizationRole.ActiveReplicator:
                    CancelActiveLocalReplication();
                    break;
                case CurrentSynchronizationRole.Hub:
                    CancelHubRole();
                    break;
                case CurrentSynchronizationRole.NotSet:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void BecomeAnActiveLocalReplicator(Device hubToConnect)
        {
            _alreadyCancelled = false;
            
            _handshakeService.SubscribeOnServerDisconnected(CancelActiveLocalReplication);

            _localReplicationService.BeginActiveLocalReplicators(hubToConnect, hubToConnect.ProjectId);

            SetStatus(CurrentSynchronizationRole.ActiveReplicator);

            _renderLogger.LogInfo("This device has become an active replicator");
        }

        private void BecomeAHub(Guid projectId, string username)
        {
            _localReplicationService.BeginPassiveLocalListeners();

            _handshakeService.StartServerAndBroadcast(projectId, username);

            SetStatus(CurrentSynchronizationRole.Hub);

            _renderLogger.LogInfo("This device has become a HUB");

            _ = TryAgainToFindAHubToBecomeAnActiveReplicator(projectId);
        }

        private void CancelActiveLocalReplication()
        {
            if (_alreadyCancelled)
            {
                return;
            }

            _handshakeService.UnsubscribeOnServerDisconnected(CancelActiveLocalReplication);
            
            _alreadyCancelled = true;

            _localReplicationService.CancelActiveLocalReplications(_handshakeService.GetConnectedServer());

            _handshakeService.DisconnectFromServer();

            SetStatus(CurrentSynchronizationRole.NotSet);

            _renderLogger.LogInfo("This device stopped active replication");
        }

        private void CancelHubRole()
        {
            _handshakeService.StopServerAndBroadcast();

            _localReplicationService.CancelPassiveLocalListeners();

            SetStatus(CurrentSynchronizationRole.NotSet);

            _renderLogger.LogInfo("This device stopped being a hub");
        }

        private void SetStatus(CurrentSynchronizationRole currentSynchronizationRole)
        {
            CurrentSyncRole = currentSynchronizationRole;

            if (currentSynchronizationRole == CurrentSynchronizationRole.NotSet)
            {
                _handshakeService.ResetHandshakeProcess();

                _localReplicationService.DisposeReplicators();

                ResetStatusState();
            }
        }

        private async void SetCurrentReplicationStatus()
        {
            await ReplicationStatusSemaphoreSlim.WaitAsync();

            try
            {
                switch (CurrentSyncRole)
                {
                    case CurrentSynchronizationRole.Hub:
                    {
                        CurrentSyncStatus = HasActiveConnections()
                            ? CurrentSyncStatus.ActiveReplication
                            : CurrentSyncStatus.Looking;
                        break;
                    }
                    case CurrentSynchronizationRole.ActiveReplicator:

                        CurrentSyncStatus = GetActiveReplicationStatus();
                        break;
                    case CurrentSynchronizationRole.NotSet:
                        break;
                }
            }
            finally
            {
                ReplicationStatusSemaphoreSlim.Release();
            }
        }

        private CurrentSyncStatus GetActiveReplicationStatus()
        {
            var currentHub = _handshakeService.GetConnectedServer();

            if (currentHub is { IsConnected: true })
            {
                return CurrentSyncStatus.ActiveReplication;
            }

            CancelActiveLocalReplication();
            return CurrentSyncStatus.ErrorEncountered;
        }

        private bool HasActiveConnections()
        {
            return _localReplicationService.GetCouchbasePassiveConnectionCount() > 0
                   && _handshakeService.GetConnectedClientsCount() > 0;
        }

        private async Task TryAgainToFindAHubToBecomeAnActiveReplicator(Guid projectId)
        {
            var broadcastMessage = await _handshakeService.TryToFindBroadcastForSync(projectId);

            if (broadcastMessage != null)
            {
                var remoteHubIpAddress = IPAddress.Parse(broadcastMessage.IpAddress);
                var currentDeviceShouldBecomeClient =
                    _handshakeService.CurrentDeviceShouldBecomeClient(remoteHubIpAddress);

                if (currentDeviceShouldBecomeClient)
                {
                    _renderLogger.LogInfo(
                        "This device discovered another hub on the network and is trying to become an active replicator");

                    CancelHubRole();
                    var hubToConnect = await _handshakeService.StartToConnectToServer(broadcastMessage,
                        ConnectionTask.SyncProject);
                    if (hubToConnect is null)
                    {
                        return;
                    }
                    BecomeAnActiveLocalReplicator(hubToConnect);
                }
            }
        }

        private void ResetStatusState()
        {
            _localReplicationService.UnsubscribeOnSyncUpdateAction(SetCurrentReplicationStatus);
            ReplicationStatusSemaphoreSlim = new SemaphoreSlim(1);
            _localReplicationService.SubscribeOnSyncUpdateAction(SetCurrentReplicationStatus);
        }
    }
}