using Render.Pages.AppStart.Home;
using Render.Pages.Configurator.WorkflowManagement;

namespace Render.Kernel.NavigationFactories;

public static class WorkflowConfigurationDispatcher
{
    public static async Task<ViewModelBase> GetWorkflowConfigurationViewModelAsync(Guid projectId,
        IViewModelContextProvider viewModelContextProvider)
    {
        var idiom = viewModelContextProvider.GetCurrentDeviceIdiom();
        ViewModelBase viewModelToNavigateTo = null;

        if (idiom  == DeviceIdiom.Tablet ||  idiom == DeviceIdiom.Desktop)
        {
            viewModelToNavigateTo = await WorkflowConfigurationViewModel.CreateAsync(projectId, viewModelContextProvider);
        }
        else
        {
            viewModelToNavigateTo = await HomeViewModel.CreateAsync
                (projectId, viewModelContextProvider);
        }
        
        return viewModelToNavigateTo;
    }
}