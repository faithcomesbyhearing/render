using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Timers;
using Microsoft.Extensions.Configuration;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Render.Interfaces;
using Render.Repositories.Kernel;
using Render.TempFromVessel.Kernel;
using Timer = System.Timers.Timer;

namespace Render.Services.SyncService
{
    public class LocalSyncService : ReactiveObject, ILocalSyncService
    {
        [Reactive]
        public CurrentSyncStatus CurrentSyncStatus { get; private set; } = CurrentSyncStatus.NotStarted;
        public int ConnectionCount { get; private set; }
        public LocalSyncStatus LocalSyncStatus { get; private set; }
        public List<Device> ConnectedDevices { get; set; } = new List<Device>();

        private SemaphoreSlim ReplicationStatusSemaphoreSlim { get; } = new SemaphoreSlim(1);
        private SemaphoreSlim HandshakeSemaphoreSlim { get; } = new SemaphoreSlim(1);

        private readonly IRenderLogger _logger;
        private readonly ILocalReplicator _renderReplicatorWrapper;
        private readonly ILocalReplicator _renderRenderProjectsReplicatorWrapper;
        private readonly ILocalReplicator _audioReplicatorWrapper;
        
        private UdpClient _discoveryAsActive;
        private UdpClient _discoveryAsPassive;
        private readonly Guid _thisAppId = Guid.NewGuid();
        private const int PortNumber = 15000;
        private string _username;
        private Guid _projectId;
        private IPEndPoint _listenEndPoint;
        private Timer _broadcastTimer;
        private Timer _listenTimer;
        private bool _isInHandshakeProcess;

        public LocalSyncService(
            IAppSettings appSettings,
            IRenderLogger logger,
            ILocalReplicator renderReplicatorWrapper,
            ILocalReplicator renderProjectsReplicatorWrapper,
            ILocalReplicator audioReplicatorWrapper)
        {
            _logger = logger;
            _renderReplicatorWrapper = renderReplicatorWrapper;
            _renderRenderProjectsReplicatorWrapper = renderProjectsReplicatorWrapper;
            _audioReplicatorWrapper = audioReplicatorWrapper;
            
            _renderReplicatorWrapper.Configure(
                Buckets.render.ToString(),
                appSettings.CouchbaseStartingPort,
                appSettings.CouchbasePeerUsername,
                appSettings.CouchbasePeerPassword);
            _renderRenderProjectsReplicatorWrapper.Configure(
                Buckets.render.ToString(),
                appSettings.CouchbaseStartingPort + 1,
                appSettings.CouchbasePeerUsername,
                appSettings.CouchbasePeerPassword);
            _audioReplicatorWrapper.Configure(
                Buckets.renderaudio.ToString(),
                appSettings.CouchbaseStartingPort + 3,
                appSettings.CouchbasePeerUsername,
                appSettings.CouchbasePeerPassword);

            _renderReplicatorWrapper.SyncStatusUpdate += SetCurrentReplicationStatus;
            _renderRenderProjectsReplicatorWrapper.SyncStatusUpdate += SetCurrentReplicationStatus;
            _audioReplicatorWrapper.SyncStatusUpdate += SetCurrentReplicationStatus;

            _broadcastTimer = new Timer();
            _broadcastTimer.Interval = 1000;
            _broadcastTimer.AutoReset = true;
            _broadcastTimer.Elapsed += BroadcastHelloAsPassiveServer;
        }

        public bool BeginActiveLocalReplication(Device device, Guid projectId)
        {
            try
            {
                _logger.LogInfo("Local replication started");

                SetStatus(LocalSyncStatus.Active);
                if (ConnectedDevices.All(a => !a.Address.Equals(device.Address) && !a.Name.Equals(device.Name)))
                {
                    ConnectedDevices.Add(device);
                }

                device.IsConnected = true;
                _renderReplicatorWrapper.AddReplicator(device, projectId, new List<string>{projectId.ToString()});
                _renderRenderProjectsReplicatorWrapper.AddReplicator(device, projectId, new List<string>{projectId.ToString()});
                _audioReplicatorWrapper.AddReplicator(device, projectId, new List<string>{projectId.ToString()});
                return true;
            }
            catch (Exception e)
            {
                _logger.LogError(e);
                return false;
            }
        }

        public void StopLocalSync()
        {
            if (LocalSyncStatus == LocalSyncStatus.Active)
            {
                CancelActiveLocalReplications();
            }
            else if (LocalSyncStatus == LocalSyncStatus.Passive)
            {
                CancelPassiveConnection();
            }
        }

        private void CancelActiveLocalReplications()
        {
            _renderReplicatorWrapper.CancelActiveReplications(ConnectedDevices);
            _renderRenderProjectsReplicatorWrapper.CancelActiveReplications(ConnectedDevices);
            _audioReplicatorWrapper.CancelActiveReplications(ConnectedDevices);
            ConnectedDevices.Clear();
            SetStatus(LocalSyncStatus.NotSet);
        }

        public void BeginPassiveLocalReplication()
        {
            SetStatus(LocalSyncStatus.Passive);
            _renderReplicatorWrapper.StartPassiveListener();
            _renderRenderProjectsReplicatorWrapper.StartPassiveListener();
            _audioReplicatorWrapper.StartPassiveListener();
        }

        private void CancelPassiveConnection()
        {
            _renderReplicatorWrapper.CancelPassiveListener();
            _renderRenderProjectsReplicatorWrapper.CancelPassiveListener();
            _audioReplicatorWrapper.CancelPassiveListener();
            ConnectionCount = 0;
            SetStatus(LocalSyncStatus.NotSet);
        }

        private void ResetForNewHandshakeProcess()
        {
            HandshakeSemaphoreSlim.Wait();
            try
            {
                _isInHandshakeProcess = false;
                _broadcastTimer.Stop();
                _discoveryAsActive?.Close();
                _discoveryAsActive?.Dispose();
                _discoveryAsPassive?.Close();
                _discoveryAsPassive?.Dispose();
            }
            catch (Exception e)
            {
                _logger.LogError(e);
            }
            finally
            {
                HandshakeSemaphoreSlim.Release();
            }
        }

        private async void SetCurrentReplicationStatus()
        {
            await ReplicationStatusSemaphoreSlim.WaitAsync();
            try
            {
                switch (LocalSyncStatus)
                {
                    case LocalSyncStatus.Passive:
                    {
                        var listOfPassiveConnections = new List<int>
                        {
                            _renderReplicatorWrapper.PassiveConnections,
                            _renderRenderProjectsReplicatorWrapper.PassiveConnections,
                            _audioReplicatorWrapper.PassiveConnections
                        };
                        ConnectionCount = listOfPassiveConnections.Min();
                        if (ConnectionCount == 0)
                        {
                            CurrentSyncStatus = CurrentSyncStatus.Looking;
                            //Set local sync status to NotSet?
                        }
                        else
                        {
                            CurrentSyncStatus = CurrentSyncStatus.ActiveReplication;
                        }
                        break;
                    }
                    case LocalSyncStatus.Active:

                        ConnectionCount = ConnectedDevices.Count(x => x.IsConnected);
                        if (ConnectedDevices.Any(x => !x.IsConnected))
                        {
                            CurrentSyncStatus = CurrentSyncStatus.ErrorEncountered;
                            CancelActiveLocalReplications();
                        }
                        else if (ConnectionCount > 0)
                        {
                            CurrentSyncStatus = CurrentSyncStatus.ActiveReplication;
                        }

                        break;
                    case LocalSyncStatus.NotSet:
                    default:
                        ConnectionCount = 0;
                        break;
                }
            }
            finally
            {
                ReplicationStatusSemaphoreSlim.Release();
            }
        }

        private void SetStatus(LocalSyncStatus status)
        {
            LocalSyncStatus = status;
            if (status == LocalSyncStatus.NotSet)
            {
                ResetForNewHandshakeProcess();
            }
        }

        public void StartLocalSync(string username, Guid projectId)
        {
            _projectId = projectId;
            // prevent multiple replicators from starting eg upon multiple presses of sync button
            if (LocalSyncStatus != LocalSyncStatus.NotSet) return;
            
            _username = username;
            CurrentSyncStatus = CurrentSyncStatus.Looking;
            Task.Run(BeginListeningToBecomeActiveReplicator);
        }
        
        private void BeginListeningToBecomeActiveReplicator()
        {
            // prevent multiple concurrent handshakes, eg upon quick presses of sync button
            HandshakeSemaphoreSlim.Wait();
            try
            {
                if (_isInHandshakeProcess)
                {
                    return;     // subsequent threads do nothing because the first set this
                }
                else
                {
                    _isInHandshakeProcess = true;   // the first thread sets this and proceeds
                }
            }
            finally
            {
                HandshakeSemaphoreSlim.Release();
            }
            
            _listenEndPoint = new IPEndPoint(IPAddress.Any, PortNumber);
            _discoveryAsActive = new UdpClient(PortNumber);

            var timeToWait = TimeSpan.FromSeconds(3);
            var asyncResult = _discoveryAsActive.BeginReceive(null, _listenEndPoint);
            asyncResult.AsyncWaitHandle.WaitOne(timeToWait);
            if (asyncResult.IsCompleted)
            {
                ReceiveHiFromPassiveServerToStartActiveReplication(asyncResult);
            }
            else
            {
                _discoveryAsActive.Close();
                BecomePassiveServer();
            }
        }

        private void ReceiveHiFromPassiveServerToStartActiveReplication(IAsyncResult asyncResult)
        {
            try
            {
                var data = _discoveryAsActive.EndReceive(asyncResult, ref _listenEndPoint);
                var msg = Encoding.ASCII.GetString(data);
                var msgArr = msg.Split(':');
                var remoteId = Guid.Parse(msgArr[0]);
                if (remoteId != _thisAppId)
                {
                    Guid listenerCurrentProjectId = Guid.Empty;
                    if (msgArr.Length >=4 && Guid.TryParse(msgArr[3], out listenerCurrentProjectId) && listenerCurrentProjectId != _projectId)
                    {
                        _isInHandshakeProcess = false;
                        _discoveryAsActive.Close();
                        BecomePassiveServer();
                        return;
                    }

                    var remoteIp = IPAddress.Parse(msgArr[1]);
                    var name = msgArr[2];

                    var device = new Device
                    {
                        Address = remoteIp,
                        Name = name,
                        ProjectId = listenerCurrentProjectId
                    };

                    BeginActiveLocalReplication(device, _projectId);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e);
            }
            finally
            {
                _discoveryAsActive?.Close();
            }
        }

        private void BecomePassiveServer()
        {
            BeginPassiveLocalReplication();
            _broadcastTimer.Start();
            _discoveryAsPassive = new UdpClient(PortNumber);
            
            _listenTimer = new Timer();
            _listenTimer.Interval = 3000;
            _listenTimer.AutoReset = false;
            _listenTimer.Elapsed += PassiveServerListenTimeout;
            _listenTimer.Start();
            _discoveryAsPassive.BeginReceive(ReceiveHiFromAnotherPassiveServer, _listenEndPoint);
        }
        
        private void PassiveServerListenTimeout(object sender, ElapsedEventArgs e)
        {
            _listenTimer.Stop();
            _discoveryAsPassive?.Close();
        }

        private void ReceiveHiFromAnotherPassiveServer(IAsyncResult asyncResult)
        {
            try
            {
                var data = _discoveryAsPassive.EndReceive(asyncResult, ref _listenEndPoint);
                var msg = Encoding.ASCII.GetString(data);
                var msgArr = msg.Split(':');
                var remoteId = Guid.Parse(msgArr[0]);
                if (remoteId != _thisAppId)
                {
                    CancelPassiveConnection();
                    BeginListeningToBecomeActiveReplicator();
                }
                else
                {
                    _discoveryAsPassive.BeginReceive(ReceiveHiFromAnotherPassiveServer, _listenEndPoint);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e);
            }
        }
        
        private void BroadcastHelloAsPassiveServer(object sender, ElapsedEventArgs e)
        {
            var endpoint = NetworkInterface.GetAllNetworkInterfaces()
                .FirstOrDefault(x => x.OperationalStatus == OperationalStatus.Up);
            var ipProps = endpoint?.GetIPProperties();
            var ip = ipProps?.UnicastAddresses.FirstOrDefault(x =>
                x.Address.AddressFamily == AddressFamily.InterNetwork);
            using (var socket = new Socket(
                       AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            {
                var group = new IPEndPoint(IPAddress.Broadcast, PortNumber);
                socket.EnableBroadcast = true;
                var hi = Encoding.ASCII.GetBytes(
                    $"{_thisAppId}:{ip?.Address}:{_username}:{_projectId}");
                try
                {
                    socket.SendTo(hi, group);
                }
                catch (SocketException ex)
                {
                    // on android, the SocketException exception is thrown if "Network is unreachable" (10051)
                    if (ex.ErrorCode == 10051)
                    {
                        _logger.LogInfo("HandshakeService failed. Socket Error 10051: 'Network is unreachable'");
                    }
                    else
                    {
                        _logger.LogError(ex);
                    }
                }
                finally
                {
                    socket.Close();
                }
            }
        }
    }
}