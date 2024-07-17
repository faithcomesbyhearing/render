using ReactiveUI;

namespace Render.Pages.Configurator.WorkflowManagement;

public partial class StageTypeCard
{
	public StageTypeCard()
	{
		InitializeComponent();

		this.WhenActivated(d =>
		{
			d(this.OneWayBind(ViewModel, vm => vm.Glyph, v => v.Icon.Text));
			d(this.OneWayBind(ViewModel, vm => vm.Name, v => v.Label.Text));
		});
	}

	private void DragRecognizerEffect_DragStarting(object sender, Kernel.DragAndDrop.DragAndDropEventArgs args)
	{
		args.Data = ViewModel;

		LogInfo("Drag started", new Dictionary<string, string>
		{
			{ "Stage Type", ViewModel?.StageType.ToString() }
		});
	}
}