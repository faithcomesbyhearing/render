using Microsoft.Maui.Networking;
using Render.WebAuthentication;

namespace Render.Services.SyncService;

public class InternetAvailableEventArgs : EventArgs
{
    public bool InternetAccess { get; set; }
}

public class ConnectivityService : IConnectivityService
{
    public event EventHandler<InternetAvailableEventArgs> InternetAvailable;
    public bool Initialized => InternetAvailable != null && InternetAvailable.GetInvocationList().Length != 0;

    private readonly ISyncGatewayApiWrapper _authenticationApiWrapper;


    public ConnectivityService(ISyncGatewayApiWrapper authenticationApiWrapper)
    {
        _authenticationApiWrapper = authenticationApiWrapper;
        Connectivity.ConnectivityChanged += OnConnectivityOnConnectivityChanged;
    }

    private async void OnConnectivityOnConnectivityChanged(object sender, ConnectivityChangedEventArgs e)
    {
        foreach (var connectionProfile in e.ConnectionProfiles)
        {
            switch (connectionProfile)
            {
                case ConnectionProfile.WiFi when e.NetworkAccess is NetworkAccess.Internet or NetworkAccess.Local:
                {
                    await InvokeInternetAvailableHandler(sender);

                    break;
                }
                case ConnectionProfile.Ethernet:
                {
                    await InvokeInternetAvailableHandler(sender);
                    break;
                }
            }
        }
    }

    private async Task InvokeInternetAvailableHandler(object sender)
    {
        var internetAvailableEventHandler = new InternetAvailableEventArgs()
        {
            InternetAccess = await _authenticationApiWrapper.IsConnected(),
        };

        InternetAvailable?.Invoke(sender, internetAvailableEventHandler);
    }
}