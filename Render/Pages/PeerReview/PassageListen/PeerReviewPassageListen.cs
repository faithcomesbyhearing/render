using Render.Kernel;
using Render.Models.Sections;
using Render.Models.Workflow;
using Render.Pages.PeerReview.PassageListen.Tablet;

namespace Render.Pages.PeerReview.PassageListen
{
    public static class PeerReviewPassageListen
    {
        public static async Task<ViewModelBase> GetViewModelFromIdiomAsync(
            IViewModelContextProvider viewModelContextProvider,
            Section section, Passage passage, Step step)
        {
            var idiom = viewModelContextProvider.GetCurrentDeviceIdiom();
            var grandCentral = viewModelContextProvider.GetGrandCentralStation();
            var stage = grandCentral.ProjectWorkflow.GetStage(step.Id);

            ViewModelBase viewModelToNavigateTo = null;

            if (idiom == DeviceIdiom.Tablet || idiom == DeviceIdiom.Desktop)
            {
                viewModelToNavigateTo = await TabletPeerReviewPassageListenPageViewModel.CreateAsync(
                    viewModelContextProvider,
                    section,
                    passage,
                    step,
                    stage);
            }
            
            return viewModelToNavigateTo;
        }
    }
}