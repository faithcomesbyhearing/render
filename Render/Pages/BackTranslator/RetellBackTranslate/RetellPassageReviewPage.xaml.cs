using ReactiveUI;

namespace Render.Pages.BackTranslator.RetellBackTranslate;

public partial class RetellPassageReviewPage 
{
    public RetellPassageReviewPage()
    {
        InitializeComponent();
        this.WhenActivated(d =>
        {
            d(this.OneWayBind(ViewModel, vm => vm.FlowDirection, 
                v => v.TopLevelElement.FlowDirection));
            d(this.OneWayBind(ViewModel, vm => vm.TitleBarViewModel, 
                v => v.TitleBar.BindingContext));
            d(this.OneWayBind(ViewModel, vm => vm.ProceedButtonViewModel,
                v => v.ProceedButton.BindingContext));
            d(this.OneWayBind(ViewModel, vm => vm.PassagePlayer,
                v => v.PassagePlayer.BindingContext));
            d(this.OneWayBind(ViewModel, vm => vm.TwoStepPassagePlayer, 
                v => v.TwoStepPassagePlayer.BindingContext));
            d(this.OneWayBind(ViewModel, vm => vm.BackTranslatePlayer,
                v => v.BackTranslatePlayer.BindingContext));
            d(this.BindCommand(ViewModel, vm => vm.NavigateBackCommand,
                v => v.ReRecordButtonGestureRecognizer));
            d(this.OneWayBind(ViewModel, vm => vm.IsLoading,
                v => v.LoadingView.IsVisible));
            
            d(this.WhenAnyValue(x => x.ViewModel.IsTwoStepBackTranslate)
                .Subscribe(isTwoStepBackTranslate =>
                {
                    TwoStepPassagePlayer.IsVisible = isTwoStepBackTranslate;
                    PassagePlayer.IsVisible = !isTwoStepBackTranslate;
                }));
        });
    }
}