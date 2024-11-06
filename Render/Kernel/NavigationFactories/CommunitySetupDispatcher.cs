using Render.Models.LocalOnlyData;
using Render.Models.Sections;
using Render.Models.Workflow;
using Render.Pages.CommunityTester.CommunityTestSetup;

namespace Render.Kernel.NavigationFactories
{
    public class CommunitySetupDispatcher : IStepViewModelDispatcher
    {
        public Task<ViewModelBase> GetViewModelToNavigateTo(IViewModelContextProvider viewModelContextProvider,
            Step step, Section section)
        {
            var sessionService = viewModelContextProvider.GetSessionStateService();
            sessionService.SetCurrentStep(step.Id, section.Id);

            var passage = SelectPassage(section, sessionService.ActiveSession);
          
            var workflowService = viewModelContextProvider.GetWorkflowService();
            var stage = workflowService.ProjectWorkflow.GetStage(step.Id);

            return Task.FromResult<ViewModelBase>(CommunityTestSetupPageViewModel.Create(
                viewModelContextProvider,
                section,
                passage,
                step,
                stage));
        }

        private Passage SelectPassage(Section section, UserProjectSession session)
        {
            var passageNumber = session?.PassageNumber;

            return passageNumber == null ? 
                section.Passages.First() : 
                section.Passages.First(passage => passage.PassageNumber.Equals(passageNumber));
        }
    }
}