using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Timers;
using Render.Interfaces;

namespace Render.Services.SyncService
{
    public class HandshakeService : IHandshakeService
    {
        private UdpClient _discovery;
        private IPEndPoint _listenEndPoint;
        private readonly Guid _thisAppId = Guid.NewGuid();
        private readonly int PortNumber = 15000;
        private System.Timers.Timer _timer;
        private IRenderLogger _logger;

        public HandshakeService(IRenderLogger renderLogger)
        {
            _logger = renderLogger;
        }

        public List<Device> AvailableDevices { get; } = new ();

        public event Action<Device> DeviceAvailable;
        public event Action Timeout;

        public void BeginListener(bool includeTimeout = false)
        {
            if (_discovery == null)
            {
                _discovery = new UdpClient(PortNumber);
            }

            if (_listenEndPoint == null)
            {
                _listenEndPoint = new IPEndPoint(IPAddress.Any, PortNumber);
            }

            if (includeTimeout)
            {
                _timer = new ();
                _timer.Interval = 30000;
                _timer.AutoReset = false;
                _timer.Elapsed += ListenTimeout;
                _timer.Start();
            }
            
            _discovery.BeginReceive(ReceiveResult, _listenEndPoint);
        }

        private void ListenTimeout(object sender, ElapsedEventArgs e)
        {
            if(_timer is null)
            {
                return;
            }

            if (!AvailableDevices.Any())
            {
                CloseUDPListener();
                Timeout?.Invoke();
            }
        }

        public void MakeDiscoverable(string username)
        {
            var endpoint = NetworkInterface
                .GetAllNetworkInterfaces()
                .FirstOrDefault(x => x.OperationalStatus == OperationalStatus.Up);

            var ipProps = endpoint?.GetIPProperties();
            var ip = ipProps?.UnicastAddresses
                .FirstOrDefault(x => x.Address.AddressFamily == AddressFamily.InterNetwork);

            using (var socket = new Socket(
                addressFamily: AddressFamily.InterNetwork,
                socketType: SocketType.Dgram,
                protocolType: ProtocolType.Udp))
            {
                var group = new IPEndPoint(IPAddress.Broadcast, PortNumber);
                socket.EnableBroadcast = true;
                var hi = Encoding.ASCII.GetBytes($"{_thisAppId}:{ip?.Address}:{username}");

                try
                {
                    socket.SendTo(hi, group);
                }
                // on android, the SocketException exception is thrown if "Network is unreachable" (10051)
                catch (SocketException e) when (e.ErrorCode == 10051)
                {
                    _logger.LogInfo("HandshakeService failed. Socket Error 10051: 'Network is unreachable'");
                }
                finally
                {
                    socket.Close();
                }
            }
        }
        
        public void CloseUDPListener()
        {
			_discovery?.Close();
            _discovery = null;

            _timer?.Stop();
            _timer?.Dispose();
            _timer = null;
        }

        private void ReceiveResult(IAsyncResult result)
        {
            try
            {
                //If we closed the connection due to a timeout, don't execute the rest of the method
                if (_discovery?.Client == null)
                {
                    _discovery?.Close();
                    _discovery = null;

                    return;
                }

                //new message: "appId:Ip:Name:channel,channel,channel"
                var data = _discovery.EndReceive(result, ref _listenEndPoint);
                var msg = Encoding.ASCII.GetString(data);
                var msgArr = msg.Split(':');
                var remoteId = Guid.Parse(msgArr[0]);
                if (remoteId == _thisAppId)
                {
                    _discovery.BeginReceive(ReceiveResult, _listenEndPoint);
                    return;
                }

                var remoteIp = IPAddress.Parse(msgArr[1]);
                var name = msgArr[2];
                var device = new Device
                {
                    Address = remoteIp,
                    Name = name
                };

                AvailableDevices.Add(device);
                DeviceAvailable?.Invoke(device);
                _discovery.BeginReceive(ReceiveResult, _listenEndPoint);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                _logger.LogError(ex);
            }
        }
    }
}