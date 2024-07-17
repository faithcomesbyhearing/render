using ReactiveUI;
using Render.Resources;
using Render.Resources.Styles;

namespace Render.Pages.Settings.SectionStatus
{
    public partial class SectionStatusPage
    {
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
                    .Subscribe(ChangeLabelColors));
                d(this.OneWayBind(ViewModel, vm => vm.IsLoading, v => v.LoadingView.IsVisible));
            });
        }
        
        public bool Selector(bool isVisible)
        {
            return !isVisible;
        }

        private void ChangeLabelColors(bool showProcessesView)
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

        protected override void OnDisappearing()
        {
            ViewModel?.Pause();
        }
        
        protected override void Dispose(bool disposing)
        {
            TitleBar?.Dispose();

            base.Dispose(disposing);
        }
    }
}