using Render.Models.Sections;
using Render.Models.Workflow;
using Render.Models.Workflow.Stage;
using Render.Pages.Interpreter;
using Render.Services.InterpretationService;
using Render.Services.SessionStateServices;

namespace Render.Kernel.NavigationFactories
{
    public class NoteInterpretDispatcher : IStepViewModelDispatcher
    {
        public async Task<ViewModelBase> GetViewModelToNavigateTo(
            IViewModelContextProvider viewModelContextProvider,
            Step step, 
            Section section)
        {
            var sessionService = viewModelContextProvider.GetSessionStateService();
            sessionService.SetCurrentStep(step.Id, section.Id);

            var sessionPage = sessionService.GetSessionPage(step.Id, section.Id, null);
            var workflowService = viewModelContextProvider.GetWorkflowService();
            var interpretationService = viewModelContextProvider.GetInterpretationService();
            var stage = workflowService.ProjectWorkflow.GetStage(step.Id);

            if (!string.IsNullOrEmpty(sessionPage))
            {
                return await GetViewModelFromSessionPage(
                    viewModelContextProvider,
                    sessionPage,
                    step,
                    section,
                    stage,
                    interpretationService,
                    sessionService);
            }

            var dictionary = await interpretationService.FindMessagesNeedingInterpretation(section.Id, stage.Id, step.Id, stage.StageType);
            var (draft, message) = NoteInterpretViewModel.FindNextMessageNeedingInterpretation(dictionary);

            return NoteInterpretViewModel.Create(
                step,
                section,
                draft,
                message,
                stage,
                dictionary,
                viewModelContextProvider);
        }

        private async Task<ViewModelBase> GetViewModelFromSessionPage(
            IViewModelContextProvider viewModelContextProvider,
            string sessionPage,
            Step step,
            Section section,
            Stage stage,
            IInterpretationService interpretationService,
            ISessionStateService sessionService)
        {
            var dictionary = await interpretationService.FindMessagesNeedingInterpretation(section.Id, stage.Id, step.Id, stage.StageType);
            if (sessionPage.Contains("NoteInterpret"))
            {
                var tuple = FindDraftAndMessage(dictionary, sessionService.ActiveSession.NonDraftTranslationId);
                return NoteInterpretViewModel.Create(
                    step,
                    section,
                    tuple.draft,
                    tuple.message,
                    stage,
                    dictionary,
                    viewModelContextProvider);
            }
            if (sessionPage.Contains("NoteReview"))
            {
                var tuple = FindDraftAndMessage(dictionary, sessionService.ActiveSession.NonDraftTranslationId);
                return NoteReviewViewModel.Create(
                    step,
                    section,
                    tuple.draft,
                    tuple.message,
                    stage,
                    dictionary,
                    viewModelContextProvider);
            }

            var (draft, message) = NoteInterpretViewModel.FindNextMessageNeedingInterpretation(dictionary);
            return NoteInterpretViewModel.Create(step, section, draft, message, stage, dictionary, viewModelContextProvider);
        }

        private (Draft draft, Message message) FindDraftAndMessage(
            Dictionary<Draft, List<Message>> dictionary,
            Guid messageId)
        {
            if (dictionary.Any(x => x.Value.SingleOrDefault(y => y.Id == messageId) != null))
            {
                var pair = dictionary
                    .SingleOrDefault(x => x.Value
                    .SingleOrDefault(y => y.Id == messageId) != null);

                var message = pair.Value.SingleOrDefault(x => x.Id == messageId);
                var draft = pair.Key;
                pair.Value.Remove(message);

                if (!pair.Value.Any())
                {
                    dictionary.Remove(pair.Key);
                }

                return (draft, message);
            }

            return default;
        }
    }
}