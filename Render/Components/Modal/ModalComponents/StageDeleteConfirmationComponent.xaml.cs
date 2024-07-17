using ReactiveUI;
using Render.Models.Workflow;
using Render.Resources;
using Render.Resources.Styles;

namespace Render.Components.Modal.ModalComponents;

public partial class StageDeleteConfirmationComponent
{
    public StageDeleteConfirmationComponent()
    {
        InitializeComponent();

        this.WhenActivated(d =>
        {
            d(this.OneWayBind(ViewModel, vm => vm.FlowDirection,
                v => v.TopLevelElement.FlowDirection));

            d(this.WhenAnyValue(x => x.ViewModel.StageState)
                .Subscribe(StageStateChanged));
        });
    }

    private void OptionTapped(object sender, EventArgs e)
    {
        ViewModel.StageState = (StageState)((TappedEventArgs)e).Parameter;
    }

    private void StageStateChanged(StageState state)
    {
        var unselectedBackgroundColor = ResourceExtensions.GetColor("ModalBackground");
        var selectedForegroundColor = Colors.White;
        var unselectedColor = ResourceExtensions.GetColor("Option");

        if (state == StageState.CompleteWork )
        {
            CompleteWorkFrame.SetValue(BackgroundColorProperty, unselectedColor);
            CompleteWorkImageSource.TextColor = selectedForegroundColor;
            CompleteWorkOptionTitle.TextColor = selectedForegroundColor;
            CompleteWorkOptionDescription.TextColor = selectedForegroundColor;
        }
        else
        {
            CompleteWorkFrame.SetValue(BackgroundColorProperty, unselectedBackgroundColor);
            CompleteWorkImageSource.TextColor = unselectedColor;
            CompleteWorkOptionTitle.TextColor = unselectedColor;
            CompleteWorkOptionDescription.TextColor = unselectedColor;
        }

        if (state == StageState.RemoveWork)
        {
            RemoveWorkFrame.SetValue(BackgroundColorProperty, unselectedColor);
            RemoveWorkImageSource.TextColor = selectedForegroundColor;
            RemoveWorkOptionTitle.TextColor = selectedForegroundColor;
            RemoveWorkOptionDescription.TextColor = selectedForegroundColor;
        }
        else
        {
            RemoveWorkFrame.SetValue(BackgroundColorProperty, unselectedBackgroundColor);
            RemoveWorkImageSource.TextColor = unselectedColor;
            RemoveWorkOptionTitle.TextColor = unselectedColor;
            RemoveWorkOptionDescription.TextColor = unselectedColor;
        }
    }

}