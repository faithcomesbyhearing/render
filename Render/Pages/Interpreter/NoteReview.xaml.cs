using ReactiveUI;

namespace Render.Pages.Interpreter;

public partial class NoteReview
{
    public NoteReview()
    {
        InitializeComponent();
        this.WhenActivated(d =>
        {
            d(this.OneWayBind(ViewModel, vm => vm.FlowDirection,
                v => v.TopLevelElement.FlowDirection));
            d(this.OneWayBind(ViewModel, vm => vm.TitleBarViewModel, v => v.TitleBar.BindingContext));
            d(this.OneWayBind(ViewModel, vm => vm.ProceedButtonViewModel, v => v.ProceedButton.BindingContext));
            d(this.OneWayBind(ViewModel, vm => vm.OriginalNotePlayer, v => v.OriginalNotePlayer.BindingContext));
            d(this.OneWayBind(ViewModel, vm => vm.InterpretedNotePlayer, v => v.InterpretedNotePlayer.BindingContext));
            d(this.BindCommand(ViewModel, vm => vm.ReRecordNoteCommand, v => v.ReRecordButtonGestureRecognizer));
            d(this.OneWayBind(ViewModel, vm => vm.IsLoading, v => v.LoadingView.IsVisible));
        });
    }
}