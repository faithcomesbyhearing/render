using System.Collections.Concurrent;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using Render.Interfaces;
using Render.Repositories.Extensions;

namespace Render.Services.SyncService;

public enum ConnectionState
{
    NotSet,
    Listening,
    Connected
}

public class NetworkConnectionService : INetworkConnectionService
{
    private const int TimeoutDuration = 7000;
    private const int HeartbeatInterval = 5000;
    private const string Alive = "ALIVE";
    private const string Keepalive = "KEEPALIVE";

    public Device AvailableDevice { get; private set; }
    public event Action ServerWasDisconnected;
    public int ConnectedClientsCount => _connectedDevices.Keys.Count;

    private TcpListener _hubServerTcpListener;

    private TcpClient _tcpClient;

    private ConnectionState ConnectionState { get; set; } = ConnectionState.NotSet;

    private CancellationTokenSource _cancellationTokenSource;

    private readonly ConcurrentDictionary<ConnectedDevice, CancellationTokenSource> _connectedDevices = [];

    private readonly IRenderLogger _renderLogger;

    private bool _thereWasALossOfConnection;
    private bool _operationAlreadyCancelled;

    public NetworkConnectionService(IRenderLogger renderLogger)
    {
        _renderLogger = renderLogger;
    }

    public async Task StartListener(int portNumber, IPAddress hostAddress)
    {
        if (ConnectionState is ConnectionState.Listening)
        {
            return;
        }

        _operationAlreadyCancelled = false;
        using (_hubServerTcpListener = new TcpListener(hostAddress, portNumber))
        {
            _hubServerTcpListener.Start();
            ConnectionState = ConnectionState.Listening;
            _cancellationTokenSource = new CancellationTokenSource();
            _renderLogger.LogInfo($"Start listen - {hostAddress}:{portNumber}");

            _renderLogger.LogInfo("Start waiting for clients");
            while (_cancellationTokenSource.Token.IsCancellationRequested is false)
            {
                try
                {
                    var tcpClient = await _hubServerTcpListener.AcceptTcpClientAsync(_cancellationTokenSource.Token);

                    var client = new ConnectedDevice(tcpClient)
                    {
                        Address = (((IPEndPoint)tcpClient.Client.RemoteEndPoint)!).Address,
                        IsConnected = true,
                    };

                    //Try to add clients
                    if (_connectedDevices.TryGetValue(client, out _) is false)
                    {
                        var cancellationTokenSource = new CancellationTokenSource();
                        _connectedDevices.TryAdd(client, cancellationTokenSource);

                        _ = HandleClientData(client);
                    }
                }
                catch (OperationCanceledException)
                {
                    _renderLogger.LogInfo("The server canceled the operation and was stopped");
                }
            }
        }
    }

    public async Task<Device> StartToConnectToTheServer(
        BroadcastMessage broadcastMessage,
        int portNumber,
        ConnectionTask connectionTask)
    {
        _ = ConnectToTcpServer(broadcastMessage, portNumber, connectionTask);
        if (AvailableDevice is not null)
        {
            AvailableDevice.IsConnected = true;
        }

        return AvailableDevice;
    }

    public void CloseClientConnection()
    {
        if (_operationAlreadyCancelled)
        {
            return;
        }

        _operationAlreadyCancelled = true;

        _cancellationTokenSource?.Cancel();
        _tcpClient?.Close();
        _tcpClient = null;

        _renderLogger.LogInfo(
            $"Current device disconnected from server {AvailableDevice?.Address} - {AvailableDevice?.Name}");

        ConnectionState = ConnectionState.NotSet;

        AvailableDevice = null;
    }

    public void DisconnectClientsAndCloseListener()
    {
        if (_operationAlreadyCancelled)
        {
            return;
        }

        _operationAlreadyCancelled = true;
        _cancellationTokenSource?.Cancel();

        foreach (var client in _connectedDevices)
        {
            if (_connectedDevices.TryRemove(client.Key, out var tokenSource))
            {
                tokenSource?.Cancel();
            }

            client.Key.CloseClient();
        }

        _connectedDevices.Clear();
        _hubServerTcpListener?.Stop();
        _hubServerTcpListener = null;

        ConnectionState = ConnectionState.NotSet;

        _renderLogger.LogInfo("Listener was closed and all connected clients was disconnected");
    }

    private async Task ConnectToTcpServer(BroadcastMessage broadcastMessage, int portNumber,
        ConnectionTask connectionTask)
    {
        if (ConnectionState is ConnectionState.Connected)
        {
            return;
        }

        _operationAlreadyCancelled = false;
        _tcpClient = new TcpClient();

        try
        {
            var serverIpAddress = IPAddress.Parse(broadcastMessage.IpAddress);

            AvailableDevice = new Device
            {
                Address = serverIpAddress,
                Name = broadcastMessage.Username,
                ProjectId = Guid.Parse(broadcastMessage.ProjectId)
            };

            await _tcpClient.ConnectAsync(AvailableDevice.Address.ToString(), portNumber);

            ConnectionState = ConnectionState.Connected;

            _cancellationTokenSource = new CancellationTokenSource();

            _renderLogger.LogInfo(
                $"Connected to server Socket:{AvailableDevice.Address}:{portNumber} - Name:{AvailableDevice.Name} - ProjectId:{AvailableDevice.ProjectId}");

            var clientIpAddress = (((IPEndPoint)_tcpClient.Client.LocalEndPoint)!).Address;
            var bytesToSend = Encoding.ASCII.GetBytes(
                $"{clientIpAddress.ToString()}:{connectionTask.ToString()}:{AvailableDevice.ProjectId}");

            var stream = _tcpClient.GetStream();

            //Send client's info to server.
            await stream.WriteAsync(bytesToSend);

            var keepAliveMessage = Encoding.ASCII.GetBytes(Keepalive);

            while (_cancellationTokenSource.Token.IsCancellationRequested is false)
            {
                // Send a keep-alive message (heartbeat)
                await stream.WriteAsync(keepAliveMessage, _cancellationTokenSource.Token);

                var receiveBuffer = new byte[1024];

                var serverResponse = await WaitForServerResponseAsync(receiveBuffer, stream);

                if (serverResponse is null)
                {
                    _renderLogger.LogInfo("Server not responding, connection lost.");
                    break;
                }

                await Task.Delay(HeartbeatInterval, _cancellationTokenSource.Token);
            }
        }
        catch (OperationCanceledException)
        {
            _renderLogger.LogInfo("Client canceled operation");
        }
        catch (SocketException)
        {
            _renderLogger.LogInfo("Connection error: Server is unavailable");
        }
        catch (IOException e)
        {
            _renderLogger.LogInfo(e.Message);
        }
        finally
        {
            HandleServerDisconnect(portNumber);
            CloseClientConnection();
        }
    }

    private async Task<string> WaitForServerResponseAsync(byte[] receiveBuffer, NetworkStream stream)
    {
        // Wait for a response with a timeout
        var responseTask = ReadResponseAsync(receiveBuffer, stream, _cancellationTokenSource.Token);
        var timeoutTask = Task.Delay(TimeoutDuration, _cancellationTokenSource.Token);
        var completedTask = await Task.WhenAny(responseTask, timeoutTask);

        if (completedTask == responseTask)
        {
            var serverResponse = await responseTask;

            if (serverResponse != Alive)
            {
                if (_thereWasALossOfConnection)
                {
                    _renderLogger.LogInfo(
                        "Invalid response received a second time - something wrong with connection.");
                    return null;
                }

                _renderLogger.LogInfo(
                    $"Unexpected response from server - Retrying to get a valid response.");
                _thereWasALossOfConnection = true;
                return serverResponse;
            }

            _thereWasALossOfConnection = false;
            _renderLogger.LogInfo("Heartbeat received, server is alive");
            return serverResponse;
        }

        return null;
    }

    private async Task HandleClientData(ConnectedDevice client)
    {
        var listenToClientTask = Task.Run(() => ListenToClientAsync(client, _connectedDevices[client].Token));
        var monitoringTask = Task.Run(() => MonitorClientTimeoutAsync(client, _connectedDevices[client].Token));

        await Task.WhenAny(listenToClientTask, monitoringTask);
    }

    private async Task ListenToClientAsync(ConnectedDevice client, CancellationToken cancellationToken)
    {
        try
        {
            var stream = client.GetStream();

            var aliveResponseMessage = Encoding.ASCII.GetBytes(Alive);

            while (cancellationToken.IsCancellationRequested is false)
            {
                var receiveBuffer = new byte[1024];

                var receivedClientsMessage = await ReadResponseAsync(receiveBuffer, stream, cancellationToken);

                if (receivedClientsMessage.IsNullOrEmpty())
                {
                    if (_thereWasALossOfConnection)
                    {
                        break;
                    }

                    _renderLogger.LogInfo(
                        $"Unexpected response from client - Retrying to get a valid response.");
                    _thereWasALossOfConnection = true;
                }
                else
                {
                    if (receivedClientsMessage == Keepalive)
                    {
                        _thereWasALossOfConnection = false;

                        _connectedDevices[client]?.CancelAfter(TimeoutDuration);

                        _renderLogger.LogInfo("Heartbeat received, client is alive, resetting timeout.");

                        await stream.WriteAsync(aliveResponseMessage, cancellationToken);

                        continue;
                    }

                    ReceiveNewInfoAboutConnectedClient(receivedClientsMessage);
                }
            }
        }
        catch (SocketException e)
        {
            _renderLogger.LogInfo(e.Message);
        }
        catch (IOException e)
        {
            _renderLogger.LogInfo(e.Message);
        }
        finally
        {
            RemoveDisconnectedUser(client);
        }
    }

    private async Task MonitorClientTimeoutAsync(ConnectedDevice client, CancellationToken cancellationToken)
    {
        try
        {
            while (cancellationToken.IsCancellationRequested is false)
            {
                await Task.Delay(HeartbeatInterval, cancellationToken);

                // Check if the client has sent a heartbeat recently
                var connectedClientExist = _connectedDevices.TryGetValue(client, out var cancellationTokenSource);
                if (connectedClientExist && cancellationTokenSource.Token.IsCancellationRequested)
                {
                    _renderLogger.LogInfo("Client has timed out due to inactivity.");
                    client.CloseClient();
                    _connectedDevices.TryRemove(client, out _);
                    break;
                }
            }
        }
        catch (TaskCanceledException)
        {
        }
    }

    private void ReceiveNewInfoAboutConnectedClient(string message)
    {
        var msgArr = message.Split(':');

        if (msgArr.Length >= 2)
        {
            var clientToUpdate = _connectedDevices.Keys.FirstOrDefault(x => x.Address.ToString() == msgArr[3]);

            if (clientToUpdate is not null)
            {
                clientToUpdate.ConnectionTask = string.Compare(msgArr[4], ConnectionTask.SyncProject.ToString(),
                    StringComparison.InvariantCultureIgnoreCase) == 0
                    ? ConnectionTask.SyncProject
                    : ConnectionTask.DownloadProject;

                if (Guid.TryParse(msgArr[5], out var projectId))
                {
                    clientToUpdate.ProjectId = projectId;
                }

                _renderLogger.LogInfo(
                    $"Client connected IpAddress: {clientToUpdate.Address} - ProjectId {clientToUpdate.ProjectId} - Task: {clientToUpdate.ConnectionTask}");
            }
        }
    }

    private void RemoveDisconnectedUser(ConnectedDevice client)
    {
        var clientToRemove = _connectedDevices.Keys.FirstOrDefault(c => Equals(c.Address, client.Address));

        if (clientToRemove is not null)
        {
            _connectedDevices.TryRemove(clientToRemove, out var token);
            token?.Cancel();
            token?.Dispose();
            clientToRemove.CloseClient();

            _renderLogger.LogInfo($"Client {clientToRemove.Address} - {clientToRemove.ProjectId} disconnected");
        }
    }

    private void HandleServerDisconnect(int portNumber)
    {
        if (AvailableDevice is null) return;

        _renderLogger.LogInfo(
            $"TCP server {AvailableDevice.Address}:{portNumber} - {AvailableDevice.Name} was closed connection");

        ServerWasDisconnected?.Invoke();
    }

    private async Task<string> ReadResponseAsync(byte[] receiveBuffer, Stream stream,
        CancellationToken cancellationToken)
    {
        try
        {
            var bytesRead = await stream.ReadAsync(receiveBuffer, cancellationToken);
            return Encoding.ASCII.GetString(receiveBuffer, 0, bytesRead);
        }
        catch (Exception e)
        {
            _renderLogger.LogInfo($"Response reading exception: {e.Source} - {e.Message}");
        }

        return string.Empty;
    }
}