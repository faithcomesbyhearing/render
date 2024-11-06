using System.Reactive.Linq;
using ReactiveUI;
using Render.Kernel;
using Render.Services.SyncService;

namespace Render;

public class AppLifecycleBus
{
    private readonly ISyncManager _syncManager;

    public AppLifecycleBus(IViewModelContextProvider contextProvider)
    {
        _syncManager = contextProvider.GetSyncManager();
    }

    public async Task<bool> OnAppClosingAsync()
    {
        await TryStopGlobalSyncAsync();
        TryStopLocalSync();
        return true;
    }

    /// <summary>
    /// Double check CurrentSyncStatus property valule due to 
    /// possible changes before actually subscribing to observable.
    /// </summary>
    private async Task TryStopGlobalSyncAsync()
    {
        if (_syncManager.CurrentWebSyncStatus is not CurrentSyncStatus.ActiveReplication)
        {
            return;
        }

        _syncManager.StopWebSync();

        var tcs = new TaskCompletionSource();
        var subscription = _syncManager
            .WhenAnyValue(service => service.CurrentWebSyncStatus)
            .Where(status => status is not CurrentSyncStatus.ActiveReplication)
            .Subscribe(_ => tcs.TrySetResult());

        if (_syncManager.CurrentWebSyncStatus is not CurrentSyncStatus.ActiveReplication)
        {
            subscription.Dispose();
            tcs.TrySetResult();
        }

        await tcs.Task;
    }
    
    private void TryStopLocalSync()
    {
        if (_syncManager?.CurrentLocalSyncStatus is not CurrentSyncStatus.ActiveReplication)
        {
            return;
        }

        _syncManager?.StopLocalSync();
    }
}