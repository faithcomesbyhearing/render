using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Timers;
using Render.Interfaces;
using Timer = System.Timers.Timer;

namespace Render.Services.SyncService;

public class BroadcastService : IBroadcastService
{
    public event Action Timeout;

    private readonly IRenderLogger _logger;

    private readonly Timer _broadcastTimer;
    private Timer _broadcastSearchTimer;

    private UdpClient _discoveryAsActiveReplicator;
    private IPEndPoint _listenEndPoint;
    private TaskCompletionSource<BroadcastMessage> _taskCompletionSource;

    private readonly Guid _thisAppId = Guid.NewGuid();

    private int PortNumber { get; set; }

    private Guid ProjectId { get; set; }

    private string Username { get; set; }

    private Socket Socket { get; set; }

    private IPEndPoint PeEndpoint { get; set; }

    private byte[] Message { get; set; }

    public BroadcastService(IRenderLogger logger)
    {
        _logger = logger;
        _broadcastTimer = new Timer();
        _broadcastTimer.Interval = 1000;
        _broadcastTimer.AutoReset = true;
        _broadcastTimer.Elapsed += StartBroadcastDataAsHub;
    }

    public void StartBroadcast(Guid projectId, string username, int portNumber)
    {
        ProjectId = projectId;
        Username = username;
        PortNumber = portNumber;
        _broadcastTimer.Start();
    }

    public void StopBroadcast()
    {
        _broadcastTimer.Stop();
        Socket?.Close();
        Socket = null;
        PeEndpoint = null;
        Message = null;
    }

    public async Task<BroadcastMessage> TryToFindABroadcastForTheProject(
        int portNumber,
        Guid projectId,
        bool includeTimeout = false)
    {
        PortNumber = portNumber;
        ProjectId = projectId;

        try
        {
            _listenEndPoint = new IPEndPoint(IPAddress.Any, portNumber);
            _discoveryAsActiveReplicator = new UdpClient(portNumber);
            _taskCompletionSource = new TaskCompletionSource<BroadcastMessage>();

            _broadcastSearchTimer = new Timer();
            _broadcastSearchTimer.Interval = includeTimeout ? 30000 : 7000;
            _broadcastSearchTimer.AutoReset = false;
            _broadcastSearchTimer.Elapsed += (sender, args) =>
            {
                if (includeTimeout)
                {
                    Timeout?.Invoke();
                }

                CloseConnection();

                _taskCompletionSource?.SetResult(null);
            };

            _discoveryAsActiveReplicator.BeginReceive(CreateBroadcastMessageForClient, _listenEndPoint);

            _broadcastSearchTimer?.Start();
        }
        catch (Exception e)
        {
            _logger.LogError(e);
        }

        return await _taskCompletionSource.Task;
    }

    public IPAddress GetIpAddress()
    {
        var endpoint = NetworkInterface.GetAllNetworkInterfaces()
            .FirstOrDefault(x => x.OperationalStatus == OperationalStatus.Up);
        var ipProps = endpoint?.GetIPProperties();
        var ip = ipProps?.UnicastAddresses.FirstOrDefault(x =>
            x.Address.AddressFamily == AddressFamily.InterNetwork);
        return ip?.Address;
    }

    private void CreateBroadcastMessageForClient(IAsyncResult asyncResult)
    {
        try
        {
            //If we closed the connection due to a timeout, don't execute the rest of the method
            if (_discoveryAsActiveReplicator?.Client is null)
            {
                return;
            }

            var data = _discoveryAsActiveReplicator.EndReceive(asyncResult, ref _listenEndPoint);
            var msg = Encoding.ASCII.GetString(data);
            var msgArr = msg.Split(':');
            var remoteId = Guid.Parse(msgArr[0]);

            if (remoteId != _thisAppId)
            {
                if (msgArr.Length <= 3)
                {
                    //Xamarin client
                    var broadcastMessage = new BroadcastMessage()
                    {
                        AppId = msgArr[0],
                        IpAddress = msgArr[1],
                        Username = msgArr[2]
                    };

                    _taskCompletionSource.SetResult(broadcastMessage);
                    CloseConnection();
                    return;
                }

                var projectId = msgArr[3];

                if (projectId == ProjectId.ToString())
                {
                    //MAUI client
                    var broadcastMessage = new BroadcastMessage()
                    {
                        AppId = msgArr[0],
                        IpAddress = msgArr[1],
                        Username = msgArr[2],
                        ProjectId = msgArr[3]
                    };

                    _taskCompletionSource.SetResult(broadcastMessage);
                    CloseConnection();
                    return;
                }
            }

            _discoveryAsActiveReplicator?.BeginReceive(CreateBroadcastMessageForClient, _listenEndPoint);
        }
        catch (Exception e)
        {
            _logger.LogInfo(e.Message);
        }
    }

    private void CloseConnection()
    {
        _broadcastSearchTimer?.Stop();
        _broadcastSearchTimer?.Dispose();
        _broadcastSearchTimer = null;
        _discoveryAsActiveReplicator?.Close();
        _discoveryAsActiveReplicator = null;
        _listenEndPoint = null;
    }

    private void StartBroadcastDataAsHub(object sender, ElapsedEventArgs e)
    {
        var ipAddress = GetIpAddress();
        try
        {
            if (Socket is null)
            {
                Socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                Socket.EnableBroadcast = true;

                _logger.LogInfo("A socket for broadcasting has been created");

                PeEndpoint = new IPEndPoint(IPAddress.Broadcast, PortNumber);
                Message = Encoding.ASCII.GetBytes(
                    $"{_thisAppId}:{ipAddress}:{Username}:{ProjectId}");
            }

            Socket?.SendTo(Message, PeEndpoint);
        }
        catch (SocketException ex)
        {
            _logger.LogError(ex);
        }
    }
}