using ReactiveUI.Fody.Helpers;
using Render.Kernel;
using Render.Kernel.WrappersAndExtensions;
using Render.Pages.AppStart.Home.NavigationIcons;

namespace Render.Pages.AppStart.Home.NavigationPanels;

public class WorkflowNavigationPaneViewModel : ViewModelBase, INavigationPane
{
    public DynamicDataWrapper<NavigationIconViewModel> NavigationIcons { get; } = new();

    public bool HasWorkAssigned { get; set; }
    [Reactive] public bool ShowMiniScrollBar { get; set; }

    public static async Task<WorkflowNavigationPaneViewModel> CreateAsync(IViewModelContextProvider viewModelContextProvider)
    {
        var vm = new WorkflowNavigationPaneViewModel(viewModelContextProvider);
        var snapshotService = viewModelContextProvider.GetSnapshotService();
        var workflowService = viewModelContextProvider.GetWorkflowService();
        var stageService = viewModelContextProvider.GetStageService();
        var work = stageService.StepsAssignedToUser();
        vm.HasWorkAssigned = work.Count > 0;
        vm.ShowMiniScrollBar = work.Count > 4;
        var index = 0;

        foreach (var id in work)
        {
            var step = workflowService.ProjectWorkflow?.GetStep(id);

            if (step == null)
            {
                continue;
            }

            var stage = workflowService.ProjectWorkflow.GetStage(id);
            var sectionsAtStep = await snapshotService.FilterOutConflicts(step);
            var workflowNavigationIconViewModel = WorkflowNavigationIconViewModelMapper.GetNavigationIconForStepType(
                viewModelContextProvider,
                stage,
                step,
                sectionsAtStep: sectionsAtStep.Count,
                projectId: vm.GetProjectId());
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