using Render.Services.SyncService;

namespace Render.Kernel.SyncServices;

public interface IUsbSyncService : IDisposable
{
    Task StartUsbSync(Guid projectId);
    void StopUsbSync();
    CurrentSyncStatus CurrentSyncStatus { get; }
}