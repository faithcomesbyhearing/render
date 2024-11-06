using System.Reactive.Linq;
using ReactiveUI;
using Render.Resources;
using Render.Resources.Styles;
using Render.Services.SyncService;

namespace Render.Components.TitleBar.MenuActions;

public partial class SyncFromUsbActionView
{
    private string _spinAnimationHandle;
    private Animation _spinAnimation;
    
    public SyncFromUsbActionView()
    {
        InitializeComponent();
        
        Unloaded += SyncMenuActionUnloaded;
        
        this.WhenActivated(d =>
        {
            d(this.BindCommand(ViewModel, vm => vm.SyncCommand, v => v.GestureRecognizer));
            d(this.WhenAnyValue(x => x.ViewModel.CurrentSyncStatus)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(AnimateSyncIcon));
        });
    }
    
    private void SyncMenuActionUnloaded(object sender, EventArgs e)
    {
        DestroyAnimation();
    }
    
    private void AnimateSyncIcon(CurrentSyncStatus currentSyncStatus)
    {
        DestroyAnimation();
        
        switch (currentSyncStatus)
        {
            case CurrentSyncStatus.ErrorEncountered:
                SecondaryImage.IsVisible = true;
                SecondaryImage.Text = IconExtensions.GetIconGlyph(Icon.CancelOrClose);
                SecondaryImage.TextColor = ((ColorReference)ResourceExtensions.GetResourceValue("Error")).Color;
                break;
            case CurrentSyncStatus.ActiveReplication:
                SecondaryImage.IsVisible = false;
                _spinAnimationHandle = Guid.NewGuid().ToString();
                _spinAnimation = new Animation(v => SyncSpinner.Rotation = v, 0, 360, Easing.Linear);
                _spinAnimation.Commit(this, nameof(_spinAnimation), length: 1000,
                    repeat: () => ViewModel?.SyncManager.CurrentUsbSyncStatus == CurrentSyncStatus.ActiveReplication);
                break;
            case CurrentSyncStatus.NotStarted:
            case CurrentSyncStatus.Finished:
                break;
        }
    }
    
    private void DestroyAnimation()
    {
        if (string.IsNullOrEmpty(_spinAnimationHandle) is false)
        {
            this.AbortAnimation(_spinAnimationHandle);
        }

        _spinAnimation?.Dispose();
        _spinAnimation = null;
        _spinAnimationHandle = null;
    }
}