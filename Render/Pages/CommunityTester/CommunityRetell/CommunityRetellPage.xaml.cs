using ReactiveUI;

namespace Render.Pages.CommunityTester.CommunityRetell;

public partial class CommunityRetellPage
{
    public const double MiniPlayerWidth = 400;

    public CommunityRetellPage()
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
            d(this.OneWayBind(ViewModel, vm => vm.SequencerRecorderViewModel,
                    v => v.Sequencer.BindingContext));
            d(this.OneWayBind(ViewModel, vm => vm.DraftPassagePlayerViewModel,
                v => v.PassagePlayer.BindingContext));
            d(this.OneWayBind(ViewModel, vm => vm.IsLoading, v => v.loadingView.IsVisible));
            d(this.OneWayBind(ViewModel, vm => vm.SectionPlayerViewModel,
                v => v.SectionPlayer.BindingContext));
        });
    }
}