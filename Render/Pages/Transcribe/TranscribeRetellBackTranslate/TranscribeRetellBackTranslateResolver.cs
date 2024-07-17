using Render.Kernel;
using Render.Models.Sections;
using Render.Models.Workflow;
using Render.Pages.AppStart.Home;

namespace Render.Pages.Transcribe.TranscribeRetellBackTranslate
{
    public class TranscribeRetellBackTranslateResolver
    {
        public static async Task<ViewModelBase> GetTranscribeRetellPassageSelectViewModelAsync(
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
                viewModelToNavigateTo = await TranscribeRetellPassageSelectPageViewModel.CreateAsync(
                    viewModelContextProvider,
                    step,
                    section,
                    stage);
            }
            else
            {
                viewModelToNavigateTo = await HomeViewModel.CreateAsync(section.ProjectId, viewModelContextProvider);
            }

            return viewModelToNavigateTo;
        }

        public static async Task<ViewModelBase> GetTranscribeRetellPassageTranslateViewModelAsync(
            Section section,
            Passage passage,
            RetellBackTranslation retellBackTranslation,
            Step step,
            IViewModelContextProvider viewModelContextProvider)
        {
            var idiom = viewModelContextProvider.GetCurrentDeviceIdiom();
            var grandCentral = viewModelContextProvider.GetGrandCentralStation();
            var stage = grandCentral.ProjectWorkflow.GetStage(step.Id);

            ViewModelBase viewModelToNavigateTo;

            if (idiom == DeviceIdiom.Tablet || idiom == DeviceIdiom.Desktop)
            {
                viewModelToNavigateTo = await TranscribeRetellPassageTranslatePageViewModel.CreateAsync(
                    viewModelContextProvider,
                    step,
                    section,
                    passage,
                    retellBackTranslation,
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