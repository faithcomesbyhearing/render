using System.Reactive.Linq;
using ReactiveUI;

namespace Render.Components.Modal.ModalComponents;

public partial class SectionApproveComponent
{
    public SectionApproveComponent()
    {
        InitializeComponent();
        this.WhenActivated(d =>
        {
            d(this.Bind(ViewModel, vm => vm.Value, v => v.PasswordEntry.Text));
            d(this.OneWayBind(ViewModel, vm => vm.ValidationMessage, v => v.ValidationLabel.Text));
        });
    }
}