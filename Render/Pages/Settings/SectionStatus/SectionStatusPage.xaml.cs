using ReactiveUI;
using Render.Kernel.WrappersAndExtensions;
using Render.Resources;
using Render.Resources.Localization;
using Render.Resources.Styles;
using System.Reactive.Linq;

namespace Render.Pages.Settings.SectionStatus
{
    public partial class SectionStatusPage
    {
        private static double _exportButtonrePreviousOpacity;
        private static bool _exportButtonPreviousAvailability;
        public SectionStatusPage()
        {
            InitializeComponent();
            this.WhenActivated(d =>
            {
                d(this.OneWayBind(ViewModel, vm => vm.FlowDirection,
                    v => v.TopLevelElement.FlowDirection));
                d(this.OneWayBind(ViewModel, vm => vm.ProcessesViewModel,
                    v => v.ProcessesView.BindingContext));
                d(this.OneWayBind(ViewModel, vm => vm.RecoveryViewModel,
                    v => v.RecoveryView.BindingContext));
                d(this.OneWayBind(ViewModel, vm => vm.TitleBarViewModel,
                    v => v.TitleBar.BindingContext));
                d(this.OneWayBind(ViewModel, vm => vm.ShowProcessesView,
                    v => v.ProcessesView.IsVisible));
                d(this.OneWayBind(ViewModel, vm => vm.IsConfigure,
                    v => v.TabStack.IsVisible));
                d(this.OneWayBind(ViewModel, vm => vm.ConflictPresent,
                    v => v.ConflictDot.IsVisible));
                d(this.OneWayBind(ViewModel, vm => vm.ShowProcessesView,
                    v => v.RecoveryView.IsVisible, Selector));

                d(this.BindCommand(ViewModel, vm => vm.SelectProcessesViewCommand,
                    v => v.SelectProcessesViewTap));
                d(this.BindCommand(ViewModel, vm => vm.SelectRecoveryViewCommand,
                    v => v.SelectRecoveryViewTap));

                d(this.WhenAnyValue(x => x.ViewModel.ShowProcessesView)
                    .Subscribe(UpdateViewButtons));
                d(this.OneWayBind(ViewModel, vm => vm.IsLoading, v => v.LoadingView.IsVisible));

                d(this.Bind(ViewModel, vm => vm.EnableExportButton, v => v.ExportButton.IsEnabled));
                d(this.OneWayBind(ViewModel, vm => vm.EnableExportButton, v => v.ExportButton.Opacity, ExportButtonOpacitySelector));
                d(this.BindCommandCustom(ExportAudioGuGestureRecognizer, v => v.ViewModel.ExportCommand));
                d(this.WhenAnyValue(x => x.ViewModel.Status)
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Subscribe(SetProgressBarStatus));
                d(this.OneWayBind(ViewModel, vm => vm.ExportPercent, v => v.ProgressBar.ProgressPercent));
                d(this.OneWayBind(ViewModel, vm => vm.ExportPercent, v => v.ProgressLabel.Text, ProgressBarPercentSelector));
                d(this.OneWayBind(ViewModel, vm => vm.ExportedString, v => v.ExportingLabel.Text));
            });
        }

        public bool Selector(bool isVisible)
        {
            return !isVisible;
        }

        private double ExportButtonOpacitySelector(bool enableExportButton)
        {
            return enableExportButton ? 1.0 : 0.3;
        }

        private string ProgressBarPercentSelector(double arg)
        {
            return $"{arg:P0}";
        }

        private void UpdateViewButtons(bool showProcessesView)
        {
            if (showProcessesView)
            {
                SetButtonActive(ProcessStack, ProcessesViewButton);
                SetButtonInactive(RecoveryStack, RecoveryViewButton);
            }
            else
            {
                SetButtonActive(RecoveryStack, RecoveryViewButton);
                SetButtonInactive(ProcessStack, ProcessesViewButton);
            }

            SetExportButtonVisibility(showProcessesView);
        }

        private void SetButtonActive(View stack, View text)
        {
            var option = (ColorReference)ResourceExtensions.GetResourceValue("Option");
            var textColor = (ColorReference)ResourceExtensions.GetResourceValue("SecondaryText");
            stack.SetValue(BackgroundColorProperty, option);
            text.SetValue(Label.TextColorProperty, textColor);
        }

        private void SetButtonInactive(View stack, View text)
        {
            var offBackground = (ColorReference)ResourceExtensions.GetResourceValue("AlternateBackground");
            var offText = (ColorReference)ResourceExtensions.GetResourceValue("Option");
            stack.SetValue(BackgroundColorProperty, offBackground);
            text.SetValue(Label.TextColorProperty, offText);
        }

        private void SetExportButtonVisibility(bool shouldBeVisible)
        {
            if (shouldBeVisible)
            {
                ExportButton.IsEnabled = _exportButtonPreviousAvailability;
                ExportButton.Opacity = _exportButtonrePreviousOpacity;
            }
            else
            {
                _exportButtonPreviousAvailability = ExportButton.IsEnabled;
                _exportButtonrePreviousOpacity = ExportButton.Opacity;
                ExportButton.IsEnabled = false;
                ExportButton.Opacity = 0;
            }
        }

        private void SetProgressBarStatus(ExportingStatus status)
        {
            SuccessIcon.IsVisible = false;
            switch (status)
            {
                case ExportingStatus.None:
                    ProgressStack.IsVisible = false;
                    break;
                case ExportingStatus.Exporting:
                    ProgressStack.IsVisible = true;
                    break;
                case ExportingStatus.Completed:
                    SuccessIcon.IsVisible = true;
                    ExportingLabel.Text = AppResources.ExportComplete;
                    break;
            }
        }

        protected override void OnDisappearing()
        {
            ViewModel?.Pause();
        }

        protected override void Dispose(bool disposing)
        {
            TitleBar?.Dispose();
            ProcessesView?.Dispose();
            RecoveryView?.Dispose();

            base.Dispose(disposing);
        }
    }
}