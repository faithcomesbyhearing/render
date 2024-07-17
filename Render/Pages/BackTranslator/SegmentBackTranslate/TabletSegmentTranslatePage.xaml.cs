using ReactiveUI;

namespace Render.Pages.BackTranslator.SegmentBackTranslate;

public partial class TabletSegmentTranslatePage
{
    public TabletSegmentTranslatePage()
    {
        InitializeComponent();
        
        this.WhenActivated(d =>
        {
            d(this.OneWayBind(ViewModel, vm => vm.FlowDirection, 
                v => v.TopLevelElement.FlowDirection));
            d(this.OneWayBind(ViewModel, vm => vm.MiniWaveformPlayerViewModel,
                v => v.MiniWaveformPlayer.BindingContext));
            d(this.OneWayBind(ViewModel, vm => vm.BarPlayerViewModel, 
                v => v.BarPlayer.BindingContext));
            d(this.OneWayBind(ViewModel, vm => vm.SequencerRecorderViewModel, 
                v => v.Sequencer.BindingContext));
            d(this.OneWayBind(ViewModel, vm => vm.TitleBarViewModel, 
                v => v.TitleBar.BindingContext));
            d(this.OneWayBind(ViewModel, vm => vm.ProceedButtonViewModel, 
                v => v.ProceedButton.BindingContext));
            d(this.OneWayBind(ViewModel, vm => vm.IsLoading, 
                v => v.LoadingView.IsVisible));

            d(this.WhenAnyValue(x => x.ViewModel.IsTwoStepBackTranslate)
                .Subscribe(isTwoStepBackTranslate =>
                {
                    MiniWaveformPlayer.IsVisible = !isTwoStepBackTranslate;
                    BarPlayer.IsVisible = isTwoStepBackTranslate;
                }));
        });
    }
}