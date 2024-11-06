using System.Net;

namespace Render.Services.SyncService;

public interface INetworkConnectionService
{
    event Action ServerWasDisconnected;
    
    int ConnectedClientsCount { get; }
    
    Device AvailableDevice { get; }
    
    Task StartListener(int portNumber, IPAddress hostAddress);

    Task<Device> StartToConnectToTheServer(
        BroadcastMessage broadcastMessage,
        int portNumber,
        ConnectionTask connectionTask);
    
    void CloseClientConnection();

    void DisconnectClientsAndCloseListener();
}