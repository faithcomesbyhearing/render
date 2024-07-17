using ReactiveUI.Fody.Helpers;
using Render.Kernel;
using Render.Kernel.WrappersAndExtensions;
using Render.Models.Workflow;
using Render.Pages.AppStart.Home.NavigationIcons;

namespace Render.Pages.AppStart.Home.NavigationPanels;

public class WorkflowNavigationPaneViewModel : ViewModelBase, INavigationPane
{
    public DynamicDataWrapper<NavigationIconViewModel> NavigationIcons { get; } = new DynamicDataWrapper<NavigationIconViewModel>();

    public bool HasWorkAssigned { get; set; }
    [Reactive] public bool ShowMiniScrollBar { get; set; }

    public static async Task<WorkflowNavigationPaneViewModel> CreateAsync(IViewModelContextProvider viewModelContextProvider)
    {
        var vm = new WorkflowNavigationPaneViewModel(viewModelContextProvider);
        var grandCentralStation = viewModelContextProvider.GetGrandCentralStation();
        var work = grandCentralStation.StepsAssignedToUser();
        vm.HasWorkAssigned = work.Count > 0;
        vm.ShowMiniScrollBar = work.Count > 4;
        var index = 0;
        
        foreach (var id in work)
        {
            var step = grandCentralStation.ProjectWorkflow?.GetStep(id);
            
            if (step == null)
            {
                continue;
            }
            
            var stage = grandCentralStation.ProjectWorkflow.GetStage(id);
            var sectionsAtStep = await grandCentralStation.FilterOutConflicts(step);
            var workflowNavigationIconViewModel = WorkflowNavigationIconViewModelMapper.GetNavigationIconForStepType(viewModelContextProvider, stage, step, sectionsAtStep.Count);
            workflowNavigationIconViewModel.IsFirstIcon = index == 0;
            vm.NavigationIcons.Add(workflowNavigationIconViewModel);
            index++;
        }
        
        return vm;
    }

    private WorkflowNavigationPaneViewModel(IViewModelContextProvider viewModelContextProvider)
        : base("WorkflowNavigationPane", viewModelContextProvider)
    {
    }

    public override void Dispose()
    {
        NavigationIcons?.Dispose();
        base.Dispose();
    }
}