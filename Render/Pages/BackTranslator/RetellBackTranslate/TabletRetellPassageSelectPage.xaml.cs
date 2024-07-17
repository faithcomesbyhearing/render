using ReactiveUI;

namespace Render.Pages.BackTranslator.RetellBackTranslate;

public partial class TabletRetellPassageSelectPage 
{
    public TabletRetellPassageSelectPage()
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

    protected override void Dispose(bool disposing)
    {
        ProceedButton?.Dispose();
        base.Dispose(disposing);
    }
}