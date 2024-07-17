using System.Reactive.Linq;
using ReactiveUI;
using Render.Kernel;
using Render.Services.SyncService;

namespace Render;

public class AppLifecycleBus
{
    private ISyncService _webSyncService;

    public AppLifecycleBus(IViewModelContextProvider contextProvider)
    {
        _webSyncService = contextProvider.GetSyncService();
    }

    public async Task<bool> OnAppClosingAsync()
    {
        await TryStopGlobalSyncAsync();

        return true;
    }

    /// <summary>
    /// Double check CurrentSyncStatus property valule due to 
    /// possible changes before actually subscribing to observable.
    /// </summary>
    private async Task TryStopGlobalSyncAsync()
    {
        if (_webSyncService.CurrentSyncStatus is not CurrentSyncStatus.ActiveReplication)
        {
            return;
        }

        _webSyncService.StopAllSync();

        var tcs = new TaskCompletionSource();
        var subscription = _webSyncService
            .WhenAnyValue(service => service.CurrentSyncStatus)
            .Where(status => status is not CurrentSyncStatus.ActiveReplication)
            .Subscribe(_ => tcs.TrySetResult());

        if (_webSyncService.CurrentSyncStatus is not CurrentSyncStatus.ActiveReplication)
        {
            subscription.Dispose();
            tcs.TrySetResult();
        }

        await tcs.Task;
    }
}