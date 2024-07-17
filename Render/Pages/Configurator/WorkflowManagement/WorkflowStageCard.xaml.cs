using ReactiveUI;
using Render.Kernel.DragAndDrop;
using Render.Kernel.WrappersAndExtensions;
using Render.Models.Workflow;
using Render.Resources;
using Render.Resources.Styles;

namespace Render.Pages.Configurator.WorkflowManagement;

public partial class WorkflowStageCard
{
    public WorkflowStageCard()
    {
        InitializeComponent();

        this.WhenActivated(d =>
        {
            d(this.OneWayBind(ViewModel, vm => vm.FlowDirection, v => v.AfterArrowhead.Rotation, SetArrowDirection));
            d(this.OneWayBind(ViewModel, vm => vm.FlowDirection, v => v.AfterNewStageArrowhead.Rotation, SetArrowDirection));
            d(this.OneWayBind(ViewModel, vm => vm.Locked, v => v.LockIcon.IsVisible));
            d(this.OneWayBind(ViewModel, vm => vm.Locked, v => v.DeleteButton.IsVisible, Selector));
            d(this.OneWayBind(ViewModel, vm => vm.Name, v => v.StageName.Text));
            d(this.OneWayBind(ViewModel, vm => vm.Stage.StageType, v => v.OriginalCard.BackgroundColor, Selector));
            d(this.OneWayBind(ViewModel, vm => vm.StageIcon, v => v.StageIcon.Text));
            d(this.OneWayBind(ViewModel, vm => vm.ShowAddStepAfterCard, v => v.AddNewStageBox.IsVisible));
            d(this.OneWayBind(ViewModel, vm => vm.ShowAddStepAfterCard, v => v.AfterNewStageArrow.IsVisible));
            d(this.OneWayBind(ViewModel, vm => vm.EndOfWorkflow, v => v.AfterArrow.IsVisible, Selector));
            d(this.BindCommand(ViewModel, vm => vm.OnDragOverCommand, v => v.DropRecognizerEffect, nameof(Kernel.DragAndDrop.DropRecognizerEffect.DragOver)));
            d(this.BindCommand(ViewModel, vm => vm.OnDragLeaveCommand, v => v.DropRecognizerEffect, nameof(Kernel.DragAndDrop.DropRecognizerEffect.DragLeave)));
            d(this.BindCommand(ViewModel, vm => vm.DeleteStageCommand, v => v.DeleteButtonGestureRecognizer));
            d(this.OneWayBind(ViewModel, vm => vm.ShowSettingsIcon, v => v.SettingsIcon.IsVisible));
            d(this.BindCommandCustom(SettingsIconGestureRecognizer, v => v.ViewModel.OpenStageSettingsCommand));

            d(this.WhenAnyValue(x => x.ViewModel.StepList)
                .Subscribe(stepList =>
                {
                    var source = BindableLayout.GetItemsSource(StepListView);
                    if (source == null)
                    {
                        BindableLayout.SetItemsSource(StepListView, stepList);
                    }
                }));
        });
    }

    private double SetArrowDirection(FlowDirection flowDirection)
    {
        return flowDirection == FlowDirection.LeftToRight ? 0 : 180;
    }

    private static bool Selector(bool arg)
    {
        return !arg;
    }

    private static Color Selector(StageTypes stageTypes)
    {
        Color color;
        switch (stageTypes)
        {
            case StageTypes.PeerCheck:
            case StageTypes.CommunityTest:
            case StageTypes.ConsultantCheck:
                color = ((ColorReference)ResourceExtensions.GetResourceValue("Option")).Color;
                break;
            default:
                color = ((ColorReference)ResourceExtensions.GetResourceValue("StageCardBackground")).Color;
                break;
        }

        return color;
    }

    private void DropRecognizerEffect_Drop(object sender, DragAndDropEventArgs args)
    {
        var vm = (StageTypeCardViewModel)args.Data;
        // need to override default stubs for prevent error throwing via default throw stub
        ViewModel?.AddStageCommand?.Execute(vm.StageType).Subscribe(Kernel.Stubs.ActionNop, Kernel.Stubs.ExceptionNop);

        ViewModel?.LogInfo("Stage added to workflow", new Dictionary<string, string>
        {
            { "Stage Name", vm.StageType.ToString() }
        });
    }
}