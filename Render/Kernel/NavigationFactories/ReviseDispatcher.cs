using Render.Models.LocalOnlyData;
using Render.Models.Sections;
using Render.Models.Workflow;
using Render.Pages.Revise.NoteListen;
using Render.Pages.Translator.DividePassagePage;
using Render.Pages.Translator.DraftingPage;
using Render.Pages.Translator.DraftSelectPage;
using Render.Pages.Translator.PassageReview;
using Render.Services.SessionStateServices;

namespace Render.Kernel.NavigationFactories
{
    public class ReviseDispatcher : IStepViewModelDispatcher
    {
        /// <summary>
        /// This will check against the <see cref="UserProjectSession"/> for the user's current spot in the revise step
        /// for the current section. If there is no session for the current section or step, then it will return a
        /// <see cref="ViewModelBase"/> based on the current settings for the step.
        /// </summary>
        /// <param name="viewModelContextProvider">The singleton IViewModelContextProvider.</param>
        /// <param name="step">The revise step for a stage of the workflow.</param>
        /// <param name="section">The current section to be drafted.</param>
        /// <returns>The correct <see cref="ViewModelBase"/> to navigate to in the revise step series of pages.</returns>
        public async Task<ViewModelBase> GetViewModelToNavigateTo(
            IViewModelContextProvider viewModelContextProvider,
            Step step, 
            Section section)
        {
			var sessionService = viewModelContextProvider.GetSessionStateService();
			sessionService.SetCurrentStepForDraftOrRevise(step.Id, section.Id);
			var isSectionInWorkForCurrentStageExist = sessionService.ActiveSession.SectionId != default;
			if (isSectionInWorkForCurrentStageExist)
			{
				var workflowRepository = viewModelContextProvider.GetWorkflowRepository();
				var workflow = await workflowRepository.GetWorkflowForProjectIdAsync(section.ProjectId);
				var userId = viewModelContextProvider.GetLoggedInUser().Id;
				var isSectionInWorkForCurrentUser = workflow.GetTeams().Any(x =>
							   x.SectionAssignments.Any(y => y.SectionId == sessionService.ActiveSession.SectionId && x.TranslatorId == userId));
				if (isSectionInWorkForCurrentUser)
				{
					var sectionInWork = await viewModelContextProvider.GetSectionRepository().GetSectionWithDraftsAsync(sessionService.ActiveSession.SectionId, true, true, false, true);
					section = sectionInWork is not null ? sectionInWork : section;
				}
			}

			var passage = sessionService.ActiveSession?.PassageNumber == null
                ? section.Passages.First()
                : section.Passages.First(x => x.PassageNumber.Equals(sessionService.ActiveSession.PassageNumber));

            var sessionPage = sessionService.GetSessionPage(
                step.Id,
                section.Id,
                passage.PassageNumber);

            if (!string.IsNullOrEmpty(sessionPage))
            {
                return await GetSessionNavigationViewModel(
                    viewModelContextProvider,
                    step,
                    section,
                    passage,
                    sessionService,
                    sessionPage);
            }
            return await NoteListen.GetNoteListenViewModelAsync(viewModelContextProvider, section, step);
        }

        /// <summary>
        /// Creates and returns a <see cref="ViewModelBase"/> based on the current page name in the session.
        /// Assumes the session page is not an empty string. 
        /// </summary>
        private async Task<ViewModelBase> GetSessionNavigationViewModel(
            IViewModelContextProvider viewModelContextProvider,
            Step step,
            Section section,
            Passage passage,
            ISessionStateService sessionService,
            string sessionPage)
        {
            
            var workflowService = viewModelContextProvider.GetWorkflowService();
            var stage = workflowService.ProjectWorkflow.GetStage(step.Id);
            if (sessionPage.Contains("Drafting"))
            {
                return await DraftingViewModel.CreateAsync(
                    section,
                    passage,
                    step,
                    viewModelContextProvider,
                    stage);
            }

            if (sessionPage.Contains("Divide"))
            {
                return DividePassageDispatcher.GetDividePassagePageViewModel(
                    section,
                    passage.PassageNumber,
                    viewModelContextProvider,
                    step);
            }

            if (sessionPage.Contains("DraftSelect"))
            {
                var temporaryAudioRepository = viewModelContextProvider.GetTemporaryAudioRepository();
                var draftNumber = 1;
                var draftList = new List<DraftViewModel>();

                foreach (var draftId in sessionService.AudioIds)
                {
                    var audio = await temporaryAudioRepository.GetByIdAsync(draftId);
                    if (audio == null)
                    {
                        continue;
                    }

                    var draftVm = new DraftViewModel(draftNumber++, viewModelContextProvider);
                    draftVm.SetAudio(audio);
                    draftVm.Deselect();
                    draftList.Add(draftVm);
                }

                return new DraftSelectViewModel(
                    section,
                    draftList,
                    viewModelContextProvider,
                    passage,
                    step,
                    stage);
            }

            if (sessionPage.Contains("PassageReview"))
            {
                return await PassageReview.GetPassageReviewViewModelAsync(
                    viewModelContextProvider,
                    section,
                    passage,
                    step);
            }

            return await NoteListen.GetNoteListenViewModelAsync(viewModelContextProvider, section, step);
        }
    }
}