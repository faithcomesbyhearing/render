using System.Reactive.Linq;
using ReactiveUI;
using Render.Kernel.WrappersAndExtensions;
using Render.Models.LocalOnlyData;
using Render.Resources;
using Render.Resources.Localization;

namespace Render.Pages.AppStart.ProjectSelect
{
    public partial class ProjectSelectCard
    {
        private string _animationHandle;
        private Animation _animation;

        public ProjectSelectCard()
        {
            InitializeComponent();

            Unloaded += ProjectSelectCardUnloaded;

            StatusIcon.Text = IconExtensions.BuildFontImageSource(Icon.Sync).Glyph;

            this.WhenActivated(d =>
            {
                d(this.OneWayBind(ViewModel, vm => vm.FlowDirection, v => v.Chevron.Text,
                    flow => IconExtensions.GetIconGlyph(flow == FlowDirection.RightToLeft ? Icon.ChevronLeft : Icon.ChevronRight)));

                d(this.OneWayBind(ViewModel, vm => vm.ProjectTitle, v => v.ProjectName.Text));

                d(this.OneWayBind(ViewModel, vm => vm.CanBeOpened, v => v.Chevron.IsVisible));
                
                d(this.OneWayBind(ViewModel, vm => vm.CanBeOpened, v => v.ExportButton.IsVisible));
                
                d(this.OneWayBind(ViewModel, vm => vm.OffloadMode, v => v.OffloadButton.IsVisible,
                    offload => (ViewModel == null || ViewModel.DownloadState is DownloadState.Finished or DownloadState.ExportDone && offload)));
                
                d(this.OneWayBind(ViewModel, vm => vm.CanBeExported,
                    v => v.ExportButton.Opacity, SetOverlayOpacity));
                
                d(this.BindCommandCustom(ExportButtonTap, v => v.ViewModel.ExportCommand));
                
                d(this.BindCommandCustom(OffloadButtonTap, v => v.ViewModel.OpenOffloadWarningModalCommand));

                d(this.BindCommandCustom(SelectProjectButton, v => v.ViewModel.NavigateToProjectCommand));

                d(this.WhenAnyValue(x => x.ViewModel.DownloadState)
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Subscribe(SetProjectCardState));
            });
        }

        private void ProjectSelectCardUnloaded(object sender, EventArgs e)
        {
            DestroyAnimation();
        }

        private void SetProjectCardState(DownloadState downloadState)
        {
            ProjectStatus.IsVisible = true;
            StatusIcon.IsVisible = true;
            
            switch (downloadState)
            {
                case DownloadState.Downloading:
                    StatusText.Text = AppResources.Adding;
                    break;
                case DownloadState.Exporting:
                    StatusText.Text = AppResources.ExportingProject;
                    break;
                case DownloadState.ExportDone:
                    StatusText.Text = AppResources.Exported; 
                    StatusIcon.IsVisible = false;
                    break;
                case DownloadState.FinishedPartially:
                    StatusText.Text = AppResources.Error;
                    StatusIcon.IsVisible = false;
                    break;
                case DownloadState.Finished:
                    ProjectStatus.IsVisible = false;
                    break;
                case DownloadState.Offloading:
                    StatusText.Text = AppResources.Offloading;
                    ViewModel.OffloadMode = false;
                    break;
                case DownloadState.Canceling:
                    StatusText.Text = AppResources.Canceling;
                    break;
                case DownloadState.NotStarted:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(downloadState), downloadState, null);
            }

            DestroyAnimation();
            
            if (ProjectStatus.IsVisible)
            {
                _animationHandle = Guid.NewGuid().ToString();
                _animation = new Animation(d => StatusIcon.Rotation = d, 0, 360);
                _animation.Commit(this, _animationHandle, length: 1000, repeat: () => true);
            }
        }
        
        private double SetOverlayOpacity(bool isExportButtonActive)
        {
            return isExportButtonActive ? 1 : 0.6;
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