using Render.Kernel;
using Render.Models.Sections;
using Render.Models.Workflow;
using Render.Pages.AppStart.Home;

namespace Render.Pages.Translator.SectionReview
{
    public static class SectionReview
    {
        public static async Task<ViewModelBase> GetSectionReviewViewModelAsync(Section section,
            IViewModelContextProvider viewModelContextProvider, Step step)
        {
            var idiom = viewModelContextProvider.GetCurrentDeviceIdiom();
            ViewModelBase viewModelToNavigateTo;
            var grandCentral = viewModelContextProvider.GetGrandCentralStation();
            var stage = grandCentral.ProjectWorkflow.GetStage(step.Id);

            if (idiom == DeviceIdiom.Tablet || idiom == DeviceIdiom.Desktop)
            {
                viewModelToNavigateTo = await TabletSectionReviewPageViewModel.CreateAsync(
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