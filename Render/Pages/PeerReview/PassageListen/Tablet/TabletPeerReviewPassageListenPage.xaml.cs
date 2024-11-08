using ReactiveUI;

namespace Render.Pages.PeerReview.PassageListen.Tablet;

public partial class TabletPeerReviewPassageListenPage 
{
    public TabletPeerReviewPassageListenPage()
    {
        InitializeComponent();
            
        this.WhenActivated(d =>
        {
            d(this.OneWayBind(ViewModel, vm => vm.FlowDirection, 
                v => v.TopLevelElement.FlowDirection));
            d(this.OneWayBind(ViewModel, vm => vm.TitleBarViewModel, v => v.TitleBar.BindingContext));
            d(this.OneWayBind(ViewModel, vm => vm.ProceedButtonViewModel, v => v.ProceedButton.BindingContext));
            d(this.OneWayBind(ViewModel, vm => vm.SequencerPlayerViewModel, v => v.Sequencer.BindingContext));
            d(this.OneWayBind(ViewModel, vm => vm.PassageReferences, v => v.References.ItemsSource));
            d(this.OneWayBind(ViewModel, vm => vm.IsLoading, v => v.LoadingView.IsVisible));
        });
    }
    
    protected override void Dispose(bool disposing)
    {
        ProceedButton?.Dispose();

        base.Dispose(disposing);
    }
}