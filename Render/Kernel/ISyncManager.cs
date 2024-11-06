using Render.Models.Users;
using Render.Services.SyncService;
using Render.WebAuthentication;

namespace Render.Kernel;

public interface ISyncManager
{
    AuthenticationApiWrapper.SyncGatewayUser SyncGatewayUser { get; set; }
    
    Task StartUsbSync(Guid projectId);

    Task StartSync(
        Guid projectId,
        IUser loggedInUser,
        string syncGateWayPassword,
        Action onSyncStarting = null,
        bool needToResetLocalSync = false,
        bool? isInternetAccess = null);

    Task StartLoginSync(
        IUser user,
        IUser loggedInUser,
        string userName,
        string userPassword);

    void StopWebSync();

    void StopLocalSync();

    void StopUsbSync();

    IOneShotReplicator GetWebAdminDownloader(
        string syncGatewayUsername,
        string syncGatewayPassword,
        string databasePath);

    CurrentSyncStatus CurrentWebSyncStatus { get; }

    CurrentSyncStatus CurrentLocalSyncStatus { get; }

    CurrentSyncStatus CurrentUsbSyncStatus { get; }

    bool IsWebSync { get; }

    public void UnsubscribeOnConnectivityChanged();
}