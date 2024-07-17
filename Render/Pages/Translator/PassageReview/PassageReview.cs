using Render.Kernel;
using Render.Models.Sections;
using Render.Models.Workflow;
using Render.Pages.AppStart.Home;

namespace Render.Pages.Translator.PassageReview
{
    public static class PassageReview
    {
        public static async Task<ViewModelBase> GetPassageReviewViewModelAsync(
            IViewModelContextProvider viewModelContextProvider, Section section, Passage passage, Step step)
        {
            var idiom = viewModelContextProvider.GetCurrentDeviceIdiom();
            ViewModelBase viewModelToNavigateTo;
            var grandCentral = viewModelContextProvider.GetGrandCentralStation();
            var stage = grandCentral.ProjectWorkflow.GetStage(step.Id);

            if (idiom == DeviceIdiom.Tablet || idiom == DeviceIdiom.Desktop)
            {
                viewModelToNavigateTo = await TabletPassageReviewPageViewModel.CreateAsync(
                    viewModelContextProvider,
                    section,
                    passage,
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