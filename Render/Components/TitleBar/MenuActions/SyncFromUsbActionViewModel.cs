using System.Reactive;
using System.Reactive.Linq;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Render.Kernel;
using Render.Services.SyncService;

namespace Render.Components.TitleBar.MenuActions;

public class SyncFromUsbActionViewModel : ViewModelBase
{
    [Reactive] public ISyncManager SyncManager { get; private set; }
    [Reactive] public CurrentSyncStatus CurrentSyncStatus { get; private set; }
    
    public ReactiveCommand<Unit, Unit> SyncCommand { get; private set; }
    
    public SyncFromUsbActionViewModel(IViewModelContextProvider viewModelContextProvider)
        : base("SyncViaUsbButton", viewModelContextProvider)
    {
        SyncManager = viewModelContextProvider.GetSyncManager();
        SyncCommand = ReactiveCommand.CreateFromTask(async () => await SyncManager.StartUsbSync(GetProjectId()));
        
        Disposables.Add(this.WhenAnyValue(x => x.SyncManager.CurrentUsbSyncStatus)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(status =>
            {
                CurrentSyncStatus = status;   
            }));
    }
    
    public override void Dispose()
    {
        SyncCommand?.Dispose();
        base.Dispose();
    }
}