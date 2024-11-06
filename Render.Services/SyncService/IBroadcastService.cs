using System.Net;

namespace Render.Services.SyncService;

public interface IBroadcastService
{
    public event Action Timeout;
    void StartBroadcast(Guid projectId, string username, int portNumber);
    void StopBroadcast();
    Task<BroadcastMessage> TryToFindABroadcastForTheProject(int portNumber, Guid projectId, bool includeTimeout = false);
    IPAddress GetIpAddress();
}