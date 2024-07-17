using Render.Kernel;
using Render.Models.Sections;
using Render.Models.Workflow;
using Render.Pages.AppStart.Home;

namespace Render.Pages.PeerReview.SectionListen
{
    public static class PeerReviewSectionListen
    {
        public static async Task<ViewModelBase> GetViewModelFromIdiomAsync(
            Section section, 
            Step step,
            IViewModelContextProvider viewModelContextProvider)
        {
            var idiom = viewModelContextProvider.GetCurrentDeviceIdiom();
            var grandCentral = viewModelContextProvider.GetGrandCentralStation();
            var stage = grandCentral.ProjectWorkflow.GetStage(step.Id);

            ViewModelBase viewModelToNavigateTo;

            if (idiom == DeviceIdiom.Tablet || idiom == DeviceIdiom.Desktop)
            {
                viewModelToNavigateTo = await TabletPeerReviewSectionListenPageViewModel.CreateAsync(
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