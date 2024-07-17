using Render.Kernel;
using Render.Models.Sections;
using Render.Models.Workflow;
using Render.Pages.AppStart.Home;

namespace Render.Pages.Revise.NoteListen
{
    public static class NoteListen
    {
        public static async Task<ViewModelBase> GetNoteListenViewModelAsync(IViewModelContextProvider viewModelContextProvider,
            Section section, Step step)
        {
            var idiom = viewModelContextProvider.GetCurrentDeviceIdiom();
            var grandCentral = viewModelContextProvider.GetGrandCentralStation();
            var stage = grandCentral.ProjectWorkflow.GetStage(step.Id);

            ViewModelBase viewModelToNavigateTo;

            if (idiom == DeviceIdiom.Tablet || idiom == DeviceIdiom.Desktop)
            {
                viewModelToNavigateTo = await TabletNoteListenPageViewModel.CreateAsync(
                    viewModelContextProvider,
                    section,
                    step,
                    stage);
            }
            else
            {
                viewModelToNavigateTo = await HomeViewModel.CreateAsync(section.ProjectId, viewModelContextProvider);
            }

            return viewModelToNavigateTo;
        }
        
    }
}