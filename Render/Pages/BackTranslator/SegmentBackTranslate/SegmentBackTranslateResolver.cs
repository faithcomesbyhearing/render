using Render.Kernel;
using Render.Models.Sections;
using Render.Models.Workflow;
using Render.Models.Workflow.Stage;
using Render.Pages.AppStart.Home;

namespace Render.Pages.BackTranslator.SegmentBackTranslate
{
    public static class SegmentBackTranslateResolver
    {
        public static async Task<ViewModelBase> GetSegmentSelectPageViewModel(
            Section section,
            Step step,
            Passage passage,
            IViewModelContextProvider viewModelContextProvider,
            SegmentBackTranslation selectedSegment = null)
        {
            var idiom = viewModelContextProvider.GetCurrentDeviceIdiom();
            var workflowService = viewModelContextProvider.GetWorkflowService();
            var stage = workflowService.ProjectWorkflow.GetStage(step.Id);

            ViewModelBase viewModelToNavigateTo;

            if (idiom == DeviceIdiom.Tablet || idiom == DeviceIdiom.Desktop)
            {
                viewModelToNavigateTo = await TabletSegmentSelectPageViewModel.CreateAsync(
                    step,
                    section,
                    passage,
                    viewModelContextProvider,
                    stage,
                    selectedSegment);
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
            Stage stage,
            Step step,
            SegmentBackTranslation segmentBackTranslation,
            string segmentName,
            IViewModelContextProvider viewModelContextProvider,
            bool overrideBackCommand = false)
        {
            var idiom = viewModelContextProvider.GetCurrentDeviceIdiom();
            ViewModelBase viewModelToNavigateTo;

            if (idiom == DeviceIdiom.Tablet || idiom == DeviceIdiom.Desktop)
            {
                viewModelToNavigateTo = TabletSegmentTranslatePageViewModel.Create(
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