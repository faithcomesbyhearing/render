using ReactiveUI;

namespace Render.Pages.BackTranslator.SegmentBackTranslate;

public partial class SegmentReviewPage
{
    public SegmentReviewPage()
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
            d(this.OneWayBind(ViewModel, vm => vm.SegmentPlayer, 
                v => v.SegmentPlayer.BindingContext));
            d(this.OneWayBind(ViewModel, vm => vm.TwoStepSegmentPlayer, 
                v => v.TwoStepSegmentPlayer.BindingContext));
            d(this.OneWayBind(ViewModel, vm => vm.BackTranslatePlayer,
                v => v.BackTranslatePlayer.BindingContext));
            d(this.BindCommand(ViewModel, vm => vm.NavigateBackCommand,
                v => v.ReRecordButtonGestureRecognizer));
            d(this.OneWayBind(ViewModel, vm => vm.IsLoading, 
                v => v.LoadingView.IsVisible));

            d(this.WhenAnyValue(x => x.ViewModel.IsTwoStepBackTranslate)
                .Subscribe(isTwoStepBackTranslate =>
                {
                    TwoStepSegmentPlayer.IsVisible = isTwoStepBackTranslate;
                    SegmentPlayer.IsVisible = !isTwoStepBackTranslate;
                }));
        });
    }
}