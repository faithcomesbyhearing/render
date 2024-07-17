using ReactiveUI.Fody.Helpers;
using Render.Kernel;
using Render.Kernel.WrappersAndExtensions;
using Render.Pages.AppStart.Home.NavigationIcons;
using Render.Pages.AppStart.Home.NavigationIcons.Configurations;

namespace Render.Pages.AppStart.Home.NavigationPanels;

public class AdministrationNavigationPaneViewModel : ViewModelBase, INavigationPane
{
    [Reactive] public bool ShowMiniScrollBar { get; set; }
    public AdministrationNavigationPaneViewModel(IViewModelContextProvider viewModelContextProvider, Guid projectId, bool enableSectionAssignment) 
        : base("AdministrationNavigationPane", viewModelContextProvider)
    {
        ShowMiniScrollBar = false;
        var actionState = enableSectionAssignment ? ActionState.Optional : ActionState.Inactive;
        NavigationIcons.AddRange(new List<NavigationIconViewModel>
        {
            new WorkflowConfigurationNavigationIconViewModel(viewModelContextProvider, projectId)
            {
                IsFirstIcon = true
            },
            new ManagerUsersNavigationIconViewModel(viewModelContextProvider, projectId),
            new RoleManagementNavigationIconViewModel(viewModelContextProvider, projectId),
            new SectionAssignmentNavigationIconViewModel(viewModelContextProvider, projectId, actionState)
        });
    }

    public DynamicDataWrapper<NavigationIconViewModel> NavigationIcons { get; } = new DynamicDataWrapper<NavigationIconViewModel>();

    public override void Dispose()
    {
        NavigationIcons?.Dispose();
        base.Dispose();
    }
}