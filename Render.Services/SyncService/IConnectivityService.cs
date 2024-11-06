
namespace Render.Services.SyncService;

public interface IConnectivityService
{
    event EventHandler<InternetAvailableEventArgs> InternetAvailable;
    bool Initialized { get; }
}