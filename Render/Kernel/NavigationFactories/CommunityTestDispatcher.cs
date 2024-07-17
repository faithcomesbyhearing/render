using Render.Components.StageSettings.CommunityTestStageSettings;
using Render.Models.LocalOnlyData;
using Render.Models.Sections;
using Render.Models.Workflow;
using Render.Models.Workflow.Stage;
using Render.Pages.CommunityTester.CommunityQAndR;
using Render.Pages.CommunityTester.CommunityRetell;
using Render.Services.SessionStateServices;

namespace Render.Kernel.NavigationFactories
{
    public class CommunityTestDispatcher : IStepViewModelDispatcher
    {
        private ISessionStateService sessionService;

        public Task<ViewModelBase> GetViewModelToNavigateTo(IViewModelContextProvider viewModelContextProvider, Step step, Section section)
        {
            return Task.Run(() =>
            {
                sessionService = viewModelContextProvider.GetSessionStateService();
                sessionService.SetCurrentStep(step.Id, section.Id);

                var passage = SelectPassage(section, sessionService.ActiveSession);
                var sessionPage = sessionService.GetSessionPage(step.Id, section.Id, passage.PassageNumber);

                var grandCentral = viewModelContextProvider.GetGrandCentralStation();
                var stage = grandCentral.ProjectWorkflow.GetStage(step.Id);

                ViewModelBase viewModel = null;
                if (!string.IsNullOrEmpty(sessionPage))
                {
                    viewModel = GetViewModelFromSessionPage(viewModelContextProvider, sessionPage, stage, step, section, passage);
                }

                var setting = Utilities.Utilities.GetRetellQuestionResponseSettingFrom(step);
                switch (setting)
                {
                    case RetellQuestionResponseSettings.Both:
                        return viewModel is null ? 
                            new CommunityRetellPageViewModel(viewModelContextProvider, section, passage, stage, step) : 
                            viewModel;

                    case RetellQuestionResponseSettings.Retell:
                        return viewModel = viewModel is CommunityRetellPageViewModel ? 
                            viewModel : 
                            new CommunityRetellPageViewModel(viewModelContextProvider, section, passage, stage, step);

                    case RetellQuestionResponseSettings.QuestionAndResponse:
                        return viewModel = viewModel is CommunityQAndRPageViewModel ? 
                            viewModel : 
                            CommunityQAndRPageViewModel.Create(viewModelContextProvider, section, passage, stage, step);
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            });
        }

        private ViewModelBase GetViewModelFromSessionPage(
            IViewModelContextProvider viewModelContextProvider,
            string sessionPage,
            Stage stage,
            Step step,
            Section section,
            Passage passage)
        {
            if (sessionPage.Contains("CommunityRetell") || sessionPage.Contains("SectionListen"))
            {
                var vm = new CommunityRetellPageViewModel(viewModelContextProvider, section, passage, stage, step);
                return vm;
            }

            if (!sessionPage.Contains("CommunityQAndRPage")) return null;
            {
                var vm = CommunityQAndRPageViewModel.CreateFromSession( sessionService, viewModelContextProvider, section, passage, stage, step);
                return vm;
            }
        }

        private Passage SelectPassage(Section section, UserProjectSession session)
        {
            var passageNumber = session?.PassageNumber;
            return passageNumber == null ? 
                section.Passages.First() : 
                section.Passages.First(x => x.PassageNumber.Equals(passageNumber));
        }
    }
}