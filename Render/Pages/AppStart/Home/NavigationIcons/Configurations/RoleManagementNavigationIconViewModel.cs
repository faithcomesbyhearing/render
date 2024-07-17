using System.Reactive.Linq;
using ReactiveUI;
using Render.Kernel;
using Render.Pages.Configurator.WorkflowAssignment;
using Render.Resources;
using Render.Resources.Localization;

namespace Render.Pages.AppStart.Home.NavigationIcons.Configurations;

public class RoleManagementNavigationIconViewModel : NavigationIconViewModel
{
    private readonly Guid _projectId;

    public RoleManagementNavigationIconViewModel(IViewModelContextProvider viewModelContextProvider, Guid projectId)
        : base(viewModelContextProvider, AppResources.AssignRoles)
    {
        _projectId = projectId;
        NavigateToPageCommand = ReactiveCommand.CreateFromTask(NavigateAsync, CanExecute);
        IconImageGlyph = (string)ResourceExtensions.GetResourceValue(Icon.AssignRoles.ToString());
        IconName = Icon.AssignRoles.ToString();
        ActionState = ActionState.Optional;
    }

    private async Task<IRoutableViewModel> NavigateAsync()
    {
        var vm = await WorkflowAssignmentViewModel.CreateAsync(ViewModelContextProvider, _projectId);
        return await NavigateTo(vm);
    }
}