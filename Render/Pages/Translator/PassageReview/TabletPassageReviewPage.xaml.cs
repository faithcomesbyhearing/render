using ReactiveUI;

namespace Render.Pages.Translator.PassageReview;

public partial class TabletPassageReviewPage
{ 
	public TabletPassageReviewPage()
	{
        InitializeComponent();

        this.WhenActivated(d =>
        {
            d(this.OneWayBind(ViewModel, vm => vm.FlowDirection,
                v => v.TopLevelElement.FlowDirection));
            d(this.OneWayBind(ViewModel, vm => vm.SequencerPlayerViewModel, v => v.Sequencer.BindingContext));
            d(this.OneWayBind(ViewModel, vm => vm.References.Items, v => v.References.ItemsSource));
            d(this.OneWayBind(ViewModel, vm => vm.TitleBarViewModel, v => v.TitleBar.BindingContext));
            d(this.OneWayBind(ViewModel, vm => vm.ProceedButtonViewModel, v => v.ProceedButton.BindingContext));
            d(this.OneWayBind(ViewModel, vm => vm.IsLoading, v => v.LoadingView.IsVisible));
        });

        NavigatedTo += OnNavigatedTo;
    }
    
    private void OnNavigatedTo(object sender, NavigatedToEventArgs e)
    {
        ViewModel?.Refresh();
    }

    protected override void Dispose(bool disposing)
    {
        ProceedButton?.Dispose();
        NavigatedTo -= OnNavigatedTo;
        base.Dispose(disposing);
    }
}