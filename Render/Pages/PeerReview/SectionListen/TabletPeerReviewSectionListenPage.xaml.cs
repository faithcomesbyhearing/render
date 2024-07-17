using ReactiveUI;
using System.Reactive.Linq;

namespace Render.Pages.PeerReview.SectionListen;

public partial class TabletPeerReviewSectionListenPage
{
    public TabletPeerReviewSectionListenPage()
    {
        InitializeComponent();
        this.WhenActivated(d =>
        {
            d(this.OneWayBind(ViewModel, vm => vm.FlowDirection,
                v => v.TopLevelElement.FlowDirection));
            d(this.OneWayBind(ViewModel, vm => vm.SequencerPlayerViewModel, v => v.Sequencer.BindingContext));
            d(this.OneWayBind(ViewModel, vm => vm.TitleBarViewModel, v => v.TitleBar.BindingContext));
            d(this.OneWayBind(ViewModel, vm => vm.ProceedButtonViewModel, v => v.ProceedButton.BindingContext));
            d(this.OneWayBind(ViewModel, vm => vm.IsLoading, v => v.LoadingView.IsVisible));

            d(this.OneWayBind(ViewModel, vm => vm.RevisionActionViewModel.RevisionItems, v => v.RevisionPicker.ItemsSource));
            d(this.Bind(ViewModel, vm => vm.RevisionActionViewModel.SelectedRevisionItem, v => v.RevisionPicker.SelectedItem));
            d(this.WhenAnyValue(x => x.ViewModel.RevisionActionViewModel.RevisionItems.Count)
                .Subscribe(count =>
                {
                    RevisionLayout.SetValue(IsVisibleProperty, count > 1);
                }));
        });
    }
}