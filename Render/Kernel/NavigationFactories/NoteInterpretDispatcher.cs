using Render.Models.Sections;
using Render.Models.Workflow;
using Render.Models.Workflow.Stage;
using Render.Pages.Interpreter;
using Render.Services;
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
            var grandCentralStation = viewModelContextProvider.GetGrandCentralStation();
            var stage = grandCentralStation.ProjectWorkflow.GetStage(step.Id);

            if (!string.IsNullOrEmpty(sessionPage))
            {
                return await GetViewModelFromSessionPage(
                    viewModelContextProvider,
                    sessionPage,
                    step,
                    section,
                    stage,
                    grandCentralStation,
                    sessionService);
            }

            var dictionary = await grandCentralStation.FindMessagesNeedingInterpretation(section, stage.Id, step.Id, stage.StageType);
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
            IGrandCentralStation grandCentralStation,
            ISessionStateService sessionService)
        {
            var dictionary = await grandCentralStation.FindMessagesNeedingInterpretation(section, stage.Id, step.Id, stage.StageType);
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