using Render.Models.Sections;
using Render.Models.Workflow;
using Render.Models.Workflow.Stage;
using Render.Pages.BackTranslator.RetellBackTranslate;
using Render.Pages.BackTranslator.SegmentBackTranslate;
using Render.Resources.Localization;
using Render.Services.SessionStateServices;

namespace Render.Kernel.NavigationFactories
{
    public class BackTranslateDispatcher : IStepViewModelDispatcher
    {
        public async Task<ViewModelBase> GetViewModelToNavigateTo(IViewModelContextProvider viewModelContextProvider,
            Step step, Section section)
        {
            var sessionService = viewModelContextProvider.GetSessionStateService();
            sessionService.SetCurrentStep(step.Id, section.Id);

            var passage = sessionService.ActiveSession?.PassageNumber == null
                ? section.Passages.First()
                : section.Passages.First(passage => passage.PassageNumber.Equals(sessionService.ActiveSession.PassageNumber));

            var sessionPage = sessionService.GetSessionPage(step.Id, section.Id, passage.PassageNumber);
            var workflowService = viewModelContextProvider.GetWorkflowService();
            var stage = workflowService.ProjectWorkflow.GetStage(step.Id);

            if (!string.IsNullOrEmpty(sessionPage))
            {
                return await GetViewModelFromSessionPage(
                    viewModelContextProvider,
                    sessionPage,
                    step,
                    section,
                    passage,
                    stage,
                    sessionService);
            }

            return await FindDefaultViewModel(section, step, passage, viewModelContextProvider);
        }

        private async Task<ViewModelBase> GetViewModelFromSessionPage(IViewModelContextProvider viewModelContextProvider,
            string sessionPage,
            Step step,
            Section section,
            Passage passage, 
            Stage stage,
            ISessionStateService sessionService)
        {
            if (sessionPage.Contains("RetellBTPassageTranslate"))
            {
                return await RetellBackTranslateResolver.GetRetellPassageTranslateViewModelAsync(
                    section: section,
                    passage: passage,
                    retellBackTranslation: passage.CurrentDraftAudio.RetellBackTranslationAudio,
                    step: step,
                    viewModelContextProvider: viewModelContextProvider);
            }
            if (sessionPage.Contains("RetellBTPassageReview"))
            {
                return await RetellPassageReviewPageViewModel.CreateAsync(
                    viewModelContextProvider: viewModelContextProvider,
                    step: step,
                    section: section,
                    passage: passage,
                    retellBackTranslation: passage.CurrentDraftAudio.RetellBackTranslationAudio,
                    passageName: "Passage " + passage.PassageNumber.Number,
                    stage: stage);
            }
            if (sessionPage.Contains("SegmentTranslate"))
            {
                var segmentBackTranslation = FindSegmentBackTranslation(sessionService, passage);
                return await SegmentBackTranslateResolver.GetSegmentTranslatePageViewModel(
                    section: section,
                    passage: passage,
                    stage: stage,
                    step: step,
                    segmentBackTranslation: segmentBackTranslation,
                    segmentName: GetSegmentNumber(passage, segmentBackTranslation.Id),
                    viewModelContextProvider: viewModelContextProvider);
            }
            if (sessionPage.Contains("SegmentReview"))
            {
                var segmentBackTranslation = FindSegmentBackTranslation(sessionService, passage);
                return await SegmentReviewPageViewModel.CreateAsync(
                    viewModelContextProvider: viewModelContextProvider,
                    step: step,
                    section: section,
                    passage: passage,
                    segmentBackTranslation: segmentBackTranslation,
                    segmentName: GetSegmentNumber(passage, segmentBackTranslation.Id),
                    stage: stage);
            }

            //For the cases when we're on segment select, segment free cut or segment combine and we go to the home
            //page, we don't want to go back to free cut or combine, so drop them all into segment select as a catch all
            if (sessionPage.Contains("Segment"))
            {
                return await SegmentBackTranslateResolver.GetSegmentSelectPageViewModel(
                    section,
                    step,
                    passage,
                    viewModelContextProvider);
            }
            
            return await FindDefaultViewModel(section, step, passage, viewModelContextProvider);
        }
        
        private async Task<ViewModelBase> FindDefaultViewModel(Section section, Step step, Passage passage,
            IViewModelContextProvider viewModelContextProvider)
        {
            if (step.StepSettings.GetSetting(SettingType.DoRetellBackTranslate))
            {
                return await RetellBackTranslateResolver.GetRetellPassageSelectViewModelAsync(
                    section,
                    step,
                    viewModelContextProvider);
            }

            if (!step.StepSettings.GetSetting(SettingType.DoSegmentBackTranslate))
            {
                return null;
            }

            return await SegmentBackTranslateResolver.GetSegmentSelectPageViewModel(
                section,
                step,
                passage,
                viewModelContextProvider);
        }
        
        private SegmentBackTranslation FindSegmentBackTranslation(
            ISessionStateService sessionStateService,
            Passage passage)
        {
            var segmentId = sessionStateService.ActiveSession.NonDraftTranslationId;
            return passage.CurrentDraftAudio.SegmentBackTranslationAudios.First(x => x.Id == segmentId);
        }
        
        public static string GetSegmentNumber(Passage passage, Guid segmentBackTranslationId)
        {
            var segmentCount = 0;
            foreach (var breathPauseSegment in passage.CurrentDraftAudio.SegmentBackTranslationAudios)
            {
                ++segmentCount;
                if (breathPauseSegment.Id == segmentBackTranslationId)
                {
                    break;
                }
            }

            return string.Format(AppResources.Segment, segmentCount.ToString());
        }
    }
}