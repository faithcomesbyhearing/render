namespace Render.Services.SyncService
{
    public interface IHandshakeService
    {
        List<Device> AvailableDevices { get; } 

        event Action<Device> DeviceAvailable;

        event Action Timeout;

        void BeginListener(bool includeTimeout = false);

        void MakeDiscoverable(string username);
        void CloseUDPListener();
    }
}