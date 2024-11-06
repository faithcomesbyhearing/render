using ReactiveUI;

namespace Render.Pages.Revise.NoteListen;

public partial class TabletNoteListenPage
{
    public TabletNoteListenPage()
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
            d(this.OneWayBind(ViewModel, vm => vm.RevisionActionViewModel, v => v.RevisionComponent.BindingContext));
        });
    }

    protected override void Dispose(bool disposing)
    {
        RevisionComponent?.Dispose();
        base.Dispose(disposing);
    }
}