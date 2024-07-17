using System.Reactive.Linq;
using ReactiveUI;
using Render.Kernel.WrappersAndExtensions;
using Render.Models.LocalOnlyData;
using Render.Resources;
using Render.Resources.Localization;
using Color = Microsoft.Maui.Graphics.Color;

namespace Render.Pages.AppStart.ProjectDownload
{
    public partial class ProjectDownloadCard
    {
        private readonly Color _statusLabelDefaultColor = ResourceExtensions.GetColor("Option");
        private readonly Color _statusLabelErrorColor = ResourceExtensions.GetColor("Error");
        
        private string _animationHandle;
        private Animation _animation;

        public ProjectDownloadCard()
        {
            InitializeComponent();

            Unloaded += ProjectDownloadCardUnloaded;

            this.WhenActivated(d =>
            {
                d(this.OneWayBind(ViewModel, vm => vm.Project.Name, v => v.ProjectName.Text));
                d(this.BindCommandCustom(DownloadButtonTap, v => v.ViewModel.DownloadProjectCommand));
                d(this.BindCommandCustom(RetryDownloadButtonTap, v => v.ViewModel.RetryDownloadProjectCommand));
                d(this.BindCommandCustom(CancelButtonTap, v => v.ViewModel.CancelProjectCommand));
                d(this.WhenAnyValue(x => x.ViewModel.DownloadState)
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Subscribe(SetProjectCardState));
            });

            Loaded += ProjectDownloadCardLoaded;
        }

        private void ProjectDownloadCardUnloaded(object sender, EventArgs e)
        {
            DestroyAnimation();
        }
        
        private void ProjectDownloadCardLoaded(object sender, EventArgs e)
        {
            if (ViewModel != null)
            {
                SetProjectCardState(ViewModel.DownloadState);
            }
        }

        private void SetProjectCardState(DownloadState state)
        {
            DestroyAnimation();

            _animationHandle = Guid.NewGuid().ToString();
            _animation = new Animation(d => StatusIcon.Rotation = d, 0, 360);

            ProjectStatus.IsVisible = true;
            DownloadButton.IsVisible = false;
            RetryDownloadButton.IsVisible = false;
            CancelButton.IsVisible = false;

            switch (state)
            {
                case DownloadState.NotStarted:
                    ProjectStatus.IsVisible = false;
                    DownloadButton.IsVisible = true;
                    break;
                case DownloadState.Downloading:
                    _animation.Commit(this, _animationHandle, length: 1000, repeat: () => true);
                    SetStatusLabel(Icon.Sync, AppResources.Adding);
                    CancelButton.IsVisible = true;
                    break;
                case DownloadState.Finished:
                    _animation.Commit(this, _animationHandle, length: 0, repeat: () => false);
                    SetStatusLabel(Icon.Checkmark, AppResources.Added);
                    break;
                case DownloadState.FinishedPartially:
                    _animation.Commit(this, _animationHandle, length: 0, repeat: () => false);
                    SetStatusLabel(Icon.PopUpWarning, AppResources.SyncFailed, error: true);
                    RetryDownloadButton.IsVisible = true;
                    CancelButton.IsVisible = true;
                    break;
                case DownloadState.Canceling:
                    _animation.Commit(this, _animationHandle, length: 1000, repeat: () => true);
                    SetStatusLabel(Icon.Sync, AppResources.Canceling);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(state), state, null);
            }
        }

        private void SetStatusLabel(Icon icon, string statusText, bool error = false)
        {
            StatusIcon.Text = IconExtensions.BuildFontImageSource(icon).Glyph;
            StatusText.Text = statusText;

            SetStatusLabelColor(error ? _statusLabelErrorColor : _statusLabelDefaultColor);
        }

        private void SetStatusLabelColor(Color color)
        {
            if (Equals(StatusIcon.TextColor, color)) return;

            StatusIcon.TextColor = color;
            StatusText.TextColor = color;
        }
        
        private void DestroyAnimation()
        {
            if (string.IsNullOrEmpty(_animationHandle) is false)
            {
                this.AbortAnimation(_animationHandle);
            }

            _animation?.Dispose();
            _animation = null;
            _animationHandle = null;
        }
    }
}