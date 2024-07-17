using System.Reactive.Linq;
using ReactiveUI;
using Render.Kernel;
using Render.Pages.Configurator.UserManagement;
using Render.Resources;
using Render.Resources.Localization;

namespace Render.Pages.AppStart.Home.NavigationIcons.Configurations;

public class ManagerUsersNavigationIconViewModel : NavigationIconViewModel
{
    private readonly Guid _projectId;

    public ManagerUsersNavigationIconViewModel(IViewModelContextProvider viewModelContextProvider, Guid projectId)
        : base(viewModelContextProvider, AppResources.ManageUsers)
    {
        _projectId = projectId;
        NavigateToPageCommand = ReactiveCommand.CreateFromTask(NavigateAsync, CanExecute);
        IconImageGlyph = (string)ResourceExtensions.GetResourceValue(Icon.ManageUsers.ToString());
        IconName = Icon.ManageUsers.ToString();
        ActionState = ActionState.Optional;
    }
        
    private async Task<IRoutableViewModel> NavigateAsync()
    {
        var vm = await Task.Run(async () => await UserManagementPageViewModel.CreateAsync( 
            ViewModelContextProvider, _projectId));
        return await NavigateTo(vm);
    }
}