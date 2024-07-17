using ReactiveUI;

namespace Render.Pages.Configurator.WorkflowManagement;

public partial class StepLabel 
{
	public StepLabel()
	{
		InitializeComponent();
            
		this.WhenActivated(d =>
		{
			d(this.OneWayBind(ViewModel, vm => vm.Title, v => v.Title.Text));
            d(this.OneWayBind(ViewModel, vm => vm.ShowSeparator, v => v.Separator.IsVisible));
        });
	}
}