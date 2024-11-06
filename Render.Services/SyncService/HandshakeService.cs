using System.Net;

namespace Render.Services.SyncService;

public class HandshakeService : IHandshakeService
{
    public event Action ConnectionTimeOut;

    private const int BroadcastPortNumber = 15000;
    private const int TcpConnectionPortNumber = 16000;
    
    private readonly INetworkConnectionService _networkConnectionService;
    private readonly IBroadcastService _broadcastService;
    
    private SemaphoreSlim HandshakeSemaphoreSlim { get; } = new(1);
    private bool _isInHandshakeProcess;

    public HandshakeService(INetworkConnectionService networkConnectionService, IBroadcastService broadcastService)
    {
        _networkConnectionService = networkConnectionService;
        _broadcastService = broadcastService;
    }
    
    public async Task<BroadcastMessage> TryToFindBroadcastForSync(Guid projectId, bool includeTimeout = false)
    {
        if (includeTimeout is false)
        {
            // prevent multiple concurrent handshakes, e.g upon quick presses of sync button
            await HandshakeSemaphoreSlim.WaitAsync();
            try
            {
                if (_isInHandshakeProcess is false)
                {
                    _isInHandshakeProcess = true; // the first thread sets this and proceeds 
                }
            }
            finally
            {
                HandshakeSemaphoreSlim.Release();
            }   
        }
        
        if (includeTimeout)
        {
            _broadcastService.Timeout += ExpirationOfTheBroadcastWaitingPeriod;
        }
        
        var broadcastMessage = await _broadcastService.TryToFindABroadcastForTheProject(BroadcastPortNumber, projectId, includeTimeout);
        return broadcastMessage;
    }
    
    public void StartServerAndBroadcast(Guid projectId, string username)
    {
        _ = _networkConnectionService.StartListener(TcpConnectionPortNumber, _broadcastService.GetIpAddress());
        _broadcastService.StartBroadcast(projectId, username, BroadcastPortNumber);
    }
    
    public async Task<Device> StartToConnectToServer(BroadcastMessage broadcastMessage, ConnectionTask connectionTask)
    {
        if (broadcastMessage is not null)
        {
            var hubToConnect = await _networkConnectionService.StartToConnectToTheServer(
                broadcastMessage,
                TcpConnectionPortNumber,
                connectionTask);
            
            return hubToConnect;
        }

        return null;
    }
    
    public void StopServerAndBroadcast()
    {
        _networkConnectionService.DisconnectClientsAndCloseListener();
        _broadcastService.StopBroadcast();
    }
    
    public void DisconnectFromServer()
    {
        _networkConnectionService.CloseClientConnection();
    }
    
    public void SubscribeOnServerDisconnected(Action onServerDisconnect)
    {
        _networkConnectionService.ServerWasDisconnected += onServerDisconnect;
    }
    
    public void UnsubscribeOnServerDisconnected(Action onServerDisconnect)
    {
        _networkConnectionService.ServerWasDisconnected -= onServerDisconnect;
    }

    public Device GetConnectedServer()
    {
        return _networkConnectionService.AvailableDevice;
    }

    public int GetConnectedClientsCount()
    {
        return _networkConnectionService.ConnectedClientsCount;
    }
    
    public bool CurrentDeviceShouldBecomeClient(IPAddress remoteHubIpAddress)
    {
        var currentIpAddress = _broadcastService.GetIpAddress();
        var bytes1 = currentIpAddress.GetAddressBytes();
        var bytes2 = remoteHubIpAddress.GetAddressBytes();

        for (int i = 0; i < bytes1.Length; i++)
        {
            if (bytes1[i] > bytes2[i])
            {
                return true;
            }

            if (bytes1[i] < bytes2[i])
            {
                return false;
            }
        }

        return false; // They are equal
    }

    public bool IsInHandshakeProcess()
    {
        return _isInHandshakeProcess;
    }

    public void ResetHandshakeProcess()
    {
        _isInHandshakeProcess = false;
    }
    
    private void ExpirationOfTheBroadcastWaitingPeriod()
    {
        ConnectionTimeOut?.Invoke();
        _broadcastService.Timeout -= ExpirationOfTheBroadcastWaitingPeriod;
    }
}