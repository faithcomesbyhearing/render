using Render.Kernel;
using Render.Models.Sections;
using Render.Models.Workflow;
using Render.Pages.AppStart.Home;

namespace Render.Pages.Transcribe.TranscribeSegmentBackTranslate
{
    public class TranscribeSegmentBackTranslateResolver
    {
        public static async Task<ViewModelBase> GetSegmentSelectPageViewModel(
            Section section,
            Step step,
            IViewModelContextProvider viewModelContextProvider,
            SegmentBackTranslation segmentBackTranslation = null)
        {
            var idiom = viewModelContextProvider.GetCurrentDeviceIdiom();
            var grandCentral = viewModelContextProvider.GetGrandCentralStation();
            var stage = grandCentral.ProjectWorkflow.GetStage(step.Id);

            ViewModelBase viewModelToNavigateTo;

            if (idiom == DeviceIdiom.Tablet || idiom == DeviceIdiom.Desktop)
            {
                viewModelToNavigateTo = await TranscribeSegmentSelectPageViewModel.CreateAsync(
                    step,
                    section,
                    viewModelContextProvider,
                    stage,
                    segmentBackTranslation);
            }
            else
            {
                viewModelToNavigateTo = await HomeViewModel.CreateAsync(section.ProjectId, viewModelContextProvider);
            }
            
            return viewModelToNavigateTo;
        }
        
        public static async Task<ViewModelBase> GetSegmentTranslatePageViewModel(
            Section section,
            Passage passage,
            Step step,
            SegmentBackTranslation segmentBackTranslation,
            string segmentName,
            IViewModelContextProvider viewModelContextProvider)
        {
            var idiom = viewModelContextProvider.GetCurrentDeviceIdiom();
            var grandCentral = viewModelContextProvider.GetGrandCentralStation();
            var stage = grandCentral.ProjectWorkflow.GetStage(step.Id);

            ViewModelBase viewModelToNavigateTo;

            if (idiom == DeviceIdiom.Tablet || idiom == DeviceIdiom.Desktop)
            {
                viewModelToNavigateTo = await TranscribeSegmentTranslateViewModel.CreateAsync(
                    viewModelContextProvider,
                    step,
                    section,
                    passage,
                    segmentBackTranslation,
                    segmentName,
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