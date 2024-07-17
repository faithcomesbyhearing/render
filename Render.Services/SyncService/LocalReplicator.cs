using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security;
using Couchbase.Lite;
using Couchbase.Lite.P2P;
using Couchbase.Lite.Sync;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Render.Interfaces;
using Render.Repositories.Extensions;

namespace Render.Services.SyncService
{
    public enum LocalReplicationResult
    {
        NotYetComplete,
        Failed,
        ProjectNotFound,
        Succeeded,
        NotStarted
    }

    public class LocalReplicator : ReactiveObject, ILocalReplicator, IDisposable
    {
        private readonly string _databasePath;

        private readonly List<(Replicator replicator, ListenerToken listenerToken, Device device)>
            _localReplicators = new();

        private URLEndpointListener _passiveListener;
        private Database _localDatabase;
        private readonly IRenderLogger _logger;

        private System.Timers.Timer Timer { get; set; }
        private bool Listening { get; set; }
        private int PortNumber { get; set; }
        private string PeerUsername { get; set; }
        private string PeerPassword { get; set; }

        public int PassiveConnections { get; private set; }

        public ReplicatorStatus CurrentStatus { get; private set; }

        [Reactive] public LocalReplicationResult Result { get; private set; }

        public event Action SyncStatusUpdate;

        public LocalReplicator(IRenderLogger renderLogger, string databasePath = null)
        {
            _databasePath = databasePath;
            _logger = renderLogger;
        }

        public void Configure(
            string databaseName,
            int portNumber,
            string peerUsername,
            string peerPassword)
        {
            var configuration = _databasePath is null
                ? null
                : new DatabaseConfiguration()
                {
                    Directory = _databasePath,
                };
            _localDatabase = new Database(databaseName, configuration);
            PortNumber = portNumber;
            PeerUsername = peerUsername;
            PeerPassword = peerPassword;
        }


        //IMPORTANT: The filterIds list is expecting the project id to be the first in the list
        public void AddReplicator(Device device, Guid projectGuid, List<string> filterIds = null,
            bool oneShotDownload = false, bool freshDownload = false)
        {
            var replicator = new Replicator(ConfigureReplicator(device, filterIds, oneShotDownload));

            var listenerToken = replicator.AddChangeListener(
                (sender, args) =>
                {
                    if (args.Status.Error != null)
                    {
                        _logger.LogError(args.Status.Error);
                    }

                    CurrentStatus = args.Status;
                    if (!oneShotDownload)
                    {
                        if (!device.IsConnected)
                        {
                            return;
                        }

                        device.IsConnected =
                            !(replicator.Status.Activity is ReplicatorActivityLevel.Offline) &&
                            !(replicator.Status.Activity is ReplicatorActivityLevel.Stopped);

                        if (!device.IsConnected)
                        {
                            //Where the replicator is disposed / removed
                            replicator.Stop();
                            replicator.Dispose();
                            _localReplicators.Remove(_localReplicators.Find(x => x.replicator == replicator));
                        }
                    }
                    else
                    {
                        if (replicator.Status.Activity == ReplicatorActivityLevel.Stopped ||
                            replicator.Status.Activity == ReplicatorActivityLevel.Offline)
                        {
                            //If it's a one-shot replication, check if we got everything we wanted
                            Result = (int)args.Status.Progress.Total == (int)args.Status.Progress.Completed ? LocalReplicationResult.Succeeded : LocalReplicationResult.Failed;

                            //Where the replicator is disposed / removed
                            replicator.Stop();
                            replicator.Dispose();

                            _localReplicators.Remove(_localReplicators.Find(x => x.replicator == replicator));
                        }
                    }

                    SyncStatusUpdate?.Invoke();
                });
            replicator.Start(freshDownload);
            _localReplicators.Add((replicator, listenerToken, device));
        }

        public void StartPassiveListener()
        {
            if (_passiveListener != null)
            {
                _passiveListener.Start();
                Timer.Start();
            }
            else
            {
                var validCredential = new NetworkCredential(PeerUsername, PeerPassword);

                //This is for Android because it didn't like me not giving a specific Network Interface for tha passive listener
                var endpoint = NetworkInterface
                    .GetAllNetworkInterfaces()
                    .FirstOrDefault(x => x.OperationalStatus == OperationalStatus.Up);

                var ipProps = endpoint?.GetIPProperties();
                var ip = ipProps?.UnicastAddresses.FirstOrDefault(x => x.Address.AddressFamily == AddressFamily.InterNetwork);
                //Go see if anyone out there is looking to replicate
                var listenerConfiguration = new URLEndpointListenerConfiguration(new[] { _localDatabase.GetDefaultCollection() })
                {
                    Authenticator = new ListenerPasswordAuthenticator((sender,
                        username,
                        password) =>
                    {
                        var passwordMatch = AuthenticateRemoteReplicator(password,
                            validCredential.SecurePassword);
                        var userMatch = username == validCredential.UserName;
                        return passwordMatch && userMatch;
                    }),

                    TlsIdentity = null,
                    DisableTLS = true,
                    EnableDeltaSync = true,
                    Port = (ushort)PortNumber,
                    //Here is where I set the specific IP address to make this work for Android.
                    NetworkInterface = ip?.Address.ToString(),
                };

                _passiveListener = new URLEndpointListener(listenerConfiguration);
                _passiveListener.Start();


                //If we are listening, set up a timer to check to see if we are a passive listener on a regular basis`
                Timer = new()
                {
                    Interval = 500,
                    Enabled = true,
                    AutoReset = true
                };

                Timer.Elapsed += (sender, args) =>
                {
                    if (Timer.Enabled)
                    {
                        if (_passiveListener.Status.ConnectionCount > 0)
                        {
                            Listening = true;
                        }

                        PassiveConnections = (int)_passiveListener.Status.ConnectionCount;

                        SyncStatusUpdate?.Invoke();
                    }
                };
            }
        }

        public void CancelPassiveListener()
        {
            _passiveListener.Stop();
            Timer.Stop();
        }

        public void CancelActiveReplications(List<Device> devicesToDisconnect)
        {
            var replicators = _localReplicators.FindAll(x => devicesToDisconnect.Contains(x.device));
            foreach (var localReplicator in replicators)
            {
                localReplicator.replicator.Stop();
                localReplicator.device.IsConnected = false;
            }
            Dispose();
            SyncStatusUpdate?.Invoke();
        }


        /// <summary>
        /// Used by the URLEndpointListener to authenticate a computer on the network to ensure they are allowed to
        /// connect to me and replicate.
        /// </summary>
        /// <param name="authPassword">The password given by the remote replicator</param>
        /// <param name="correctPassword">The correct password for this URLEndpointListener</param>
        /// <returns></returns>
        private bool AuthenticateRemoteReplicator(SecureString authPassword, SecureString correctPassword)
        {
            var authPasswordInMem = Marshal.SecureStringToBSTR(authPassword);
            var correctPasswordInMem = Marshal.SecureStringToBSTR(correctPassword);
            try
            {
                var length1 = Marshal.ReadInt32(correctPasswordInMem, -4);
                var length2 = Marshal.ReadInt32(authPasswordInMem, -4);
                if (length1 != length2) return false;

                for (var i = 0; i < length1; i++)
                {
                    var byte1 = Marshal.ReadByte(correctPasswordInMem + i);
                    var byte2 = Marshal.ReadByte(authPasswordInMem + i);
                    if (byte1 != byte2)
                    {
                        return false;
                    }
                }

                return true;
            }
            finally
            {
                Marshal.ZeroFreeBSTR(authPasswordInMem);
                Marshal.ZeroFreeBSTR(correctPasswordInMem);
            }
        }

        private ReplicatorConfiguration ConfigureReplicator(Device device, ICollection<string> filterIds, bool oneShotDownload)
        {
            var peerEndpointString = $"ws://{device.Address}:{PortNumber}/{_localDatabase.Name}";
            var host = new Uri(peerEndpointString);
            var dbUrlTarget = new Uri(host, _localDatabase.Name);

            var securityString = new SecureString();
            Array.ForEach(PeerPassword.ToCharArray(), securityString.AppendChar);

            var basicAuth = new BasicAuthenticator(PeerUsername, securityString);

            var replicatorConfig = new ReplicatorConfiguration(new URLEndpoint(dbUrlTarget))
            {
                Authenticator = basicAuth,
                ReplicatorType = oneShotDownload ? ReplicatorType.Pull : ReplicatorType.PushAndPull,
                Continuous = !oneShotDownload,
                AcceptOnlySelfSignedServerCertificate = false,
                PinnedServerCertificate = null
            };
            
            replicatorConfig.AddCollection(_localDatabase.GetDefaultCollection(), new CollectionConfiguration
            {
                PullFilter = (document, flags) =>
                {
                    var projectId = document.GetString("ProjectId");
                    if (!projectId.IsNullOrEmpty() && filterIds.Contains(projectId))
                    {
                        return true;
                    }

                    var id = document.GetString("Id");
                    return !id.IsNullOrEmpty() && filterIds.Contains(id);
                },

                PushFilter = (document, flags) =>
                {
                    var projectId = document.GetString("ProjectId");
                    if (!projectId.IsNullOrEmpty() && filterIds.Contains(projectId))
                    {
                        return true;
                    }

                    var id = document.GetString("Id");
                    return !id.IsNullOrEmpty() && filterIds.Contains(id);
                }
            });


            return replicatorConfig;
        }

        public void Dispose()
        {
            if (Listening)
            {
                Timer.Stop();
                Timer.Dispose();
                Timer = null;

                _passiveListener.Stop();
                _passiveListener.Dispose();
            }

            foreach (var replicator in _localReplicators)
            {
                replicator.replicator?.RemoveChangeListener(replicator.listenerToken);
                replicator.replicator?.Stop();
                replicator.replicator?.Dispose();
            }
        }
    }
}