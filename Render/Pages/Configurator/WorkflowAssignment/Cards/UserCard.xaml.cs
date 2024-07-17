using ReactiveUI;
using Render.Kernel.DragAndDrop;

namespace Render.Pages.Configurator.WorkflowAssignment;

public partial class UserCard
{
    public UserCard()
    {
        InitializeComponent();
        
        this.WhenActivated(d =>
        {
            d(this.OneWayBind(ViewModel, vm => vm.FullName, v => v.UserFullNameLabel.Text));
        });
    }
        
    private void DragStageTypeGestureEffect_DragStarting(object sender, DragAndDropEventArgs args)
    {
        args.Data = ViewModel;
    }
}