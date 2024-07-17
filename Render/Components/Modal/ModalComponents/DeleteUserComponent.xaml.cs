
using ReactiveUI;

namespace Render.Components.Modal.ModalComponents;

public partial class DeleteUserComponent 
{
    public DeleteUserComponent()
    {
        InitializeComponent();
        
        this.WhenActivated(d =>
        {
            d(this.OneWayBind(ViewModel, vm => vm.FlowDirection, 
                v => v.TopLevelElement.FlowDirection));

            d(this.BindCommand(ViewModel, vm => vm.ViewSectionAssignmentsCommand,
                v => v.ViewSectionAssignmentsGesture));
            d(this.OneWayBind(ViewModel, vm => vm.EnableSectionAssignment,
                v => v.ViewSectionAssignmentsButton.IsVisible));
            d(this.OneWayBind(ViewModel, vm => vm.DeleteUserMessage, v => v.DeleteUserMessage.Text));
        });
    }
}