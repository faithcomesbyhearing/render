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
            var workflowService = viewModelContextProvider.GetWorkflowService();
            var stage = workflowService.ProjectWorkflow.GetStage(step.Id);

            if (idiom == DeviceIdiom.Tablet || idiom == DeviceIdiom.Desktop)
            {
                viewModelToNavigateTo = TabletPassageReviewPageViewModel.Create(
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