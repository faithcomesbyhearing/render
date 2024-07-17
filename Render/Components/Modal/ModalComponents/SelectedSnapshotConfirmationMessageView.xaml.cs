using ReactiveUI;

namespace Render.Components.Modal.ModalComponents;

public partial class SelectedSnapshotConfirmationMessageView
{
    public SelectedSnapshotConfirmationMessageView()
    {
        InitializeComponent();

        this.WhenActivated(d =>
        {
            d(this.OneWayBind(ViewModel, x => x.FlowDirection, v => v.MainGrid.FlowDirection));
            d(this.OneWayBind(ViewModel, x => x.BodyMessage, v => v.MainMessage.Text));
        });
    }
}