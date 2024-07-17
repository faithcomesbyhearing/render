using System.Reactive.Linq;
using ReactiveUI;
using Render.Kernel;
using Render.Pages.Configurator.SectionAssignment;
using Render.Resources;
using Render.Resources.Localization;

namespace Render.Pages.AppStart.Home.NavigationIcons.Configurations;

public class SectionAssignmentNavigationIconViewModel : NavigationIconViewModel
{
    private readonly Guid _projectId;
        
    public SectionAssignmentNavigationIconViewModel(IViewModelContextProvider viewModelContextProvider, Guid projectId, ActionState actionState)
        :base(viewModelContextProvider, AppResources.AssignSections)
    {
        _projectId = projectId;    
        NavigateToPageCommand = ReactiveCommand.CreateFromTask(NavigateAsync, CanExecute);
        IconImageGlyph = (string)ResourceExtensions.GetResourceValue(Icon.AssignSections.ToString());
        IconName = Icon.AssignSections.ToString();
        ActionState = actionState;
    }

    private async Task<IRoutableViewModel> NavigateAsync()                                                  
    {
        var vm = await Task.Run(async () => await SectionAssignmentPageViewModel.CreateAsync(ViewModelContextProvider, _projectId));
        return await NavigateTo(vm);
    }
}