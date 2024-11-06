using System.Reactive;
using System.Reactive.Linq;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Render.Kernel;
using Render.Resources;
using Render.Resources.Localization;
using Render.Services.SyncService;

namespace Render.Components.TitleBar.MenuActions
{
    public class SyncMenuActionViewModel : ViewModelBase
    {
        [Reactive] public ISyncManager SyncManager { get; private set; }
        
        [Reactive] public CurrentSyncStatus CurrentSyncStatus { get; private set; }

        public bool IsManualSync { get; set; }

        public string Title { get; set; }

        public readonly ReactiveCommand<Unit, Unit> Command;

        public string Glyph { get; set; }

        public SyncMenuActionViewModel(IViewModelContextProvider viewModelContextProvider)
            : base("SyncMenuAction", viewModelContextProvider)
        {
            SyncManager = viewModelContextProvider.GetSyncManager();
            
            Glyph = ((FontImageSource)ResourceExtensions.GetResourceValue("SyncIcon"))?.Glyph;
            Title = AppResources.Sync;
            Command = ReactiveCommand.CreateFromTask(SyncNowAsync);
            
            Disposables.Add(this.WhenAnyValue(x => x.SyncManager.CurrentWebSyncStatus)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(status =>
                {
                    CurrentSyncStatus = status;
                }));

            Disposables.Add(this.WhenAnyValue(x => x.SyncManager.CurrentLocalSyncStatus)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(status =>
                {
                    CurrentSyncStatus = status;   
                }));
        }

        private async Task SyncNowAsync()
        {
#if DEMO
            return;
#endif
            IsManualSync = true;
            var loggedInUser = ViewModelContextProvider.GetLoggedInUser();
            if (loggedInUser is null) return;
            await SyncManager.StartSync(GetProjectId(), loggedInUser, loggedInUser.SyncGatewayLogin, onSyncStarting: CloseMenu);
            
            LogInfo("Button Clicked", new Dictionary<string, string>
            {
                { "Button Name", "Sync Now" },
                { "ViewModel", nameof(SyncMenuActionViewModel) },
                { "LoggedInUserId", GetLoggedInUserId().ToString() },
                { "ProjectId", GetProjectId().ToString() }
            });
        }

        protected void CloseMenu() => ViewModelContextProvider.GetMenuPopupService().Close();

        public override void Dispose()
        {
            Command?.Dispose();
            base.Dispose();
        }
    }
}