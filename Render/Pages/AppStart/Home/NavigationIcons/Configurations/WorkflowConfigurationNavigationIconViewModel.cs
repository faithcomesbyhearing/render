using System.Reactive.Linq;
using ReactiveUI;
using Render.Kernel;
using Render.Kernel.NavigationFactories;
using Render.Resources;
using Render.Resources.Localization;

namespace Render.Pages.AppStart.Home.NavigationIcons.Configurations;

public class WorkflowConfigurationNavigationIconViewModel : NavigationIconViewModel
{
    private readonly Guid _projectId;
        
    public WorkflowConfigurationNavigationIconViewModel(IViewModelContextProvider viewModelContextProvider, Guid projectId)
        :base(viewModelContextProvider, AppResources.ConfigureWorkflow)
    {
        _projectId = projectId;
        NavigateToPageCommand = ReactiveCommand.CreateFromTask(NavigateAsync, CanExecute);
        IconImageGlyph = (string)ResourceExtensions.GetResourceValue(Icon.ConfigureWorkflow.ToString());
        IconName = Icon.ConfigureWorkflow.ToString();
        ActionState = ActionState.Optional;
    }

    private async Task<IRoutableViewModel> NavigateAsync()
    {
        var vm = await Task.Run(async () => await WorkflowConfigurationDispatcher.GetWorkflowConfigurationViewModelAsync(_projectId,
            ViewModelContextProvider));
        return await NavigateTo(vm);
    }
}