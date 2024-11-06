using Render.Kernel;
using Render.Models.Sections;
using Render.Models.Workflow;
using Render.Pages.AppStart.Home;
using Render.Utilities;

namespace Render.Pages.Translator.DividePassagePage
{
    public static class DividePassageDispatcher
    {
        public static ViewModelBase GetDividePassagePageViewModel(Section section,
            PassageNumber passageNumber,
            IViewModelContextProvider viewModelContextProvider, Step step)
        {
            try
            {
                var workflowService = viewModelContextProvider.GetWorkflowService();
                var stage = workflowService.ProjectWorkflow.GetStage(step.Id);

                ViewModelBase viewModelToNavigateTo = DividePassageViewModel.Create(
                    viewModelContextProvider,
                    section,
                    passageNumber,
                    step,
                    stage);

                return viewModelToNavigateTo;
            }
            catch (Exception e)
            {
                RenderLogger.LogError(e);
                throw;
            }
        }
    }
}