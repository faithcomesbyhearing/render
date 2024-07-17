using ReactiveUI;
using Render.Kernel.WrappersAndExtensions;
using Render.Utilities;

namespace Render.Pages.Configurator.WorkflowAssignment.Stages;

public partial class WorkflowPeerCheckStageColumn
{
    public WorkflowPeerCheckStageColumn()
    {
        InitializeComponent();
        this.WhenActivated(d =>
        {
			d(this.OneWayBind(ViewModel, vm => vm.Name, v => v.StageName.Text,
					TailTruncationHelper.AddTailTruncation));
			d(this.OneWayBind(ViewModel, vm => vm.StageGlyph, v => v.IconLabel.Text));

            d(this.WhenAnyValue(x => x.ViewModel.TeamList)
                .Subscribe(x =>
                {
                    var source = BindableLayout.GetItemsSource(TeamCollection);
                    if (source == null)
                    {
                        BindableLayout.SetItemsSource(TeamCollection, x);
                    }
                }));

            d(this.BindCommandCustom(SettingsButtonGestureRecognizer, v => v.ViewModel.OpenStageSettingsCommand));
        });
    }
}