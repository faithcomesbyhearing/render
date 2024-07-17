using ReactiveUI;
using Render.Resources;
using Render.Services.SyncService;
using Render.Resources.Styles;
using Render.Resources.Localization;

namespace Render.Components.TitleBar.MenuActions
{
    public partial class SyncMenuAction
    {
        private string _spinAnimationHandle;
        private Animation _spinAnimation;

        public SyncMenuAction()
        {
            InitializeComponent();

            Unloaded += SyncMenuActionUnloaded;

            this.WhenActivated(d =>
            {
                d(this.BindCommand(ViewModel, vm => vm.Command, v => v.GestureRecognizer));
                d(this.OneWayBind(ViewModel, vm => vm.Title, v => v.Label.Text));
                d(this.OneWayBind(ViewModel, vm => vm.Glyph, v => v.Image.Text));

                d(this.WhenAnyValue(x => x.ViewModel.CurrentSyncStatus)
                    .Subscribe(AnimateSyncIcon));
            });
        }

        private void SyncMenuActionUnloaded(object sender, EventArgs e)
        {
            DestroyAnimation();
        }

        private void AnimateSyncIcon(CurrentSyncStatus currentSyncStatus)
        {
            if (currentSyncStatus == CurrentSyncStatus.ErrorEncountered)
            {
                SecondaryImage.Text = IconExtensions.GetIconGlyph(Icon.CancelOrClose);
                SecondaryImage.TextColor = ((ColorReference)ResourceExtensions.GetResourceValue("Error")).Color;
                SecondaryLabel.Text = AppResources.SyncFailed;
            }
            else
            {
                SecondaryImage.Text = "";
                SecondaryLabel.Text = "";
            }

            DestroyAnimation();

            if (currentSyncStatus == CurrentSyncStatus.ActiveReplication)
            {
                _spinAnimationHandle = Guid.NewGuid().ToString();
                _spinAnimation = new Animation(v => Image.Rotation = v, 0, 360, Easing.Linear);
                _spinAnimation.Commit(this, nameof(_spinAnimation), length: 1000,
                    repeat: () => ViewModel.CurrentSyncStatus == CurrentSyncStatus.ActiveReplication);
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
}