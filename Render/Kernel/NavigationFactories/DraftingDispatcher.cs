using Render.Models.LocalOnlyData;
using Render.Models.Sections;
using Render.Models.Workflow;
using Render.Models.Workflow.Stage;
using Render.Pages.Translator.DraftingPage;
using Render.Pages.Translator.DraftSelectPage;
using Render.Pages.Translator.PassageListen;
using Render.Pages.Translator.PassageReview;
using Render.Pages.Translator.SectionListen;
using Render.Pages.Translator.SectionReview;
using Render.Services.SessionStateServices;

namespace Render.Kernel.NavigationFactories
{
    public class DraftingDispatcher : IStepViewModelDispatcher
    {
        /// <summary>
        /// This will check against the <see cref="UserProjectSession"/> for the user's current spot in the drafting step
        /// for the current section. If there is no session for the current section or step, then it will return a
        /// <see cref="ViewModelBase"/> based on the current settings for the step.
        /// </summary>
        /// <param name="viewModelContextProvider">The singleton IViewModelContextProvider.</param>
        /// <param name="step">The drafting step for the drafting stage of the workflow.</param>
        /// <param name="section">The current section to be drafted.</param>
        /// <returns>The correct <see cref="ViewModelBase"/> to navigate to in the drafting step series of pages.</returns>
        public async Task<ViewModelBase> GetViewModelToNavigateTo(IViewModelContextProvider viewModelContextProvider,
            Step step, Section section)
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
					var sectionInWork = await viewModelContextProvider.GetSectionRepository().GetSectionAsync(sessionService.ActiveSession.SectionId, true);
					section = sectionInWork is not null ? sectionInWork : section;
				}
			}

			var passage = SelectPassageToDraft(section, sessionService.ActiveSession);
			var sessionPage = sessionService.GetSessionPage(step.Id, section.Id, passage.PassageNumber);

			var workflowService = viewModelContextProvider.GetWorkflowService();
			var stage = workflowService.ProjectWorkflow.GetStage(step.Id);

			if (!string.IsNullOrEmpty(sessionPage))
            {
                return await GetSessionNavigationViewModel(
                    viewModelContextProvider,
                    step,
                    section,
                    passage,
                    sessionService,
                    sessionPage,
                    stage);
            }

            return await GetSettingsNavigationViewModel(viewModelContextProvider, step, section, passage, stage);
        }

        private Passage SelectPassageToDraft(Section section, UserProjectSession activeSession)
        {
            var passageNumber = activeSession?.PassageNumber;
            if (passageNumber is null)
            {
                return FindUnDraftedPassage(section);
            }

            var sectionPassage = section.Passages
                .FirstOrDefault(passage => passage.PassageNumber.Equals(passageNumber));

            return sectionPassage ?? FindUnDraftedPassage(section);
        }

        private Passage FindUnDraftedPassage(Section section)
        {
            var unDraftedPassages = section.Passages
                .Where(passage => passage.HasAudio is false)
                .OrderBy(passage => passage.PassageNumber)
                .ToList();

            return !unDraftedPassages.Any() ?
                section.Passages.LastOrDefault() :
                unDraftedPassages.FirstOrDefault();
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
            string sessionPage, Stage stage)
        {

            if (sessionPage.Contains("SectionListen"))
            {
                return SectionListenViewModel.Create(viewModelContextProvider, section, step, stage);
            }

            if (sessionPage.Contains("PassageListen"))
            {
                return await PassageListenViewModel.CreateAsync(
                    viewModelContextProvider,
                    section,
                    step,
                    passage,
                    stage);
            }


            if (sessionPage.Contains("Drafting"))
            {
                return await DraftingViewModel.CreateAsync(section, passage, step, viewModelContextProvider, stage);
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

            if (sessionPage.Contains("SectionReview"))
            {
                return await SectionReview.GetSectionReviewViewModelAsync(
                    section,
                    viewModelContextProvider,
                    step);
            }


            //This is only if we get into a really weird situation where the current session for this section
            //is in this step, but has an invalid page name. Is this even possible?

            // TODO: Remove after complete drafting page
            return await DraftingViewModel.CreateAsync(section, passage, step, viewModelContextProvider, stage);
        }

        /// <summary>
        /// Creates and returns a <see cref="ViewModelBase"/> depending on the current settings of a drafting step,
        /// completely separate from a user session.
        /// </summary>
        private async Task<ViewModelBase> GetSettingsNavigationViewModel(
            IViewModelContextProvider viewModelContextProvider,
            Step step,
            Section section,
            Passage passage,
            Stage stage)
        {
            if (step.StepSettings.GetSetting(SettingType.DoSectionListen))
            {
                return SectionListenViewModel.Create(
                    viewModelContextProvider,
                    section,
                    step,
                    stage);
            }

            if (step.StepSettings.GetSetting(SettingType.DoPassageListen))
            {
                return await PassageListenViewModel.CreateAsync(
                    viewModelContextProvider,
                    section,
                    step,
                    passage,
                    stage);
            }

            return await DraftingViewModel.CreateAsync(section, passage, step, viewModelContextProvider, stage);
        }
    }
}