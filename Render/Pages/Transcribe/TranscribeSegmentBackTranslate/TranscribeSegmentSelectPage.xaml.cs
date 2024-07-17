using ReactiveUI;

namespace Render.Pages.Transcribe.TranscribeSegmentBackTranslate;

public partial class TranscribeSegmentSelectPage 
{
    public TranscribeSegmentSelectPage()
    {
        InitializeComponent();
            
        this.WhenActivated(d =>
        {
            d(this.OneWayBind(ViewModel, vm => vm.FlowDirection, 
                v => v.TopLevelElement.FlowDirection));
            d(this.OneWayBind(ViewModel, vm => vm.SequencerPlayerViewModel, 
                v => v.Sequencer.BindingContext));
            d(this.OneWayBind(ViewModel, vm => vm.TitleBarViewModel, 
                v => v.TitleBar.BindingContext));
            d(this.OneWayBind(ViewModel, vm => vm.ProceedButtonViewModel,
                v => v.ProceedButton.BindingContext));
            d(this.OneWayBind(ViewModel, vm => vm.IsLoading, 
                v => v.LoadingView.IsVisible));
        });
    }
        
    protected override void OnDisappearing()
    {
        ViewModel?.SequencerPlayerViewModel?.StopCommand?.Execute().Subscribe();
        ViewModel?.PauseSectionTitlePlayer();
        base.OnDisappearing();
    }
}