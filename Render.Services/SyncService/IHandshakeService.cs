using System.Net;

namespace Render.Services.SyncService;

public interface IHandshakeService
{
    Task<BroadcastMessage> TryToFindBroadcastForSync(Guid projectId, bool includeTimeout = false);
    bool IsInHandshakeProcess();
    void ResetHandshakeProcess();

    #region Client

    event Action ConnectionTimeOut;
    public Task<Device> StartToConnectToServer(BroadcastMessage broadcastMessage, ConnectionTask connectionTask);
    void DisconnectFromServer();
    void SubscribeOnServerDisconnected(Action onServerDisconnect);
    void UnsubscribeOnServerDisconnected(Action onServerDisconnect);
    Device GetConnectedServer();

    #endregion

    #region Server

    void StartServerAndBroadcast(Guid projectId, string username);
    void StopServerAndBroadcast();
    int GetConnectedClientsCount();
    bool CurrentDeviceShouldBecomeClient(IPAddress remoteHubIpAddress);

    #endregion
}