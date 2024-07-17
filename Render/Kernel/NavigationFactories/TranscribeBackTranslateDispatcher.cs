using Render.Models.Sections;
using Render.Models.Workflow;
using Render.Pages.Transcribe.TranscribeRetellBackTranslate;
using Render.Pages.Transcribe.TranscribeSegmentBackTranslate;
using Render.Resources.Localization;
using Render.Services.SessionStateServices;

namespace Render.Kernel.NavigationFactories
{
    public class TranscribeBackTranslateDispatcher : IStepViewModelDispatcher
    {
        private ISessionStateService _sessionService;
        
        public async Task<ViewModelBase> GetViewModelToNavigateTo(IViewModelContextProvider viewModelContextProvider,
            Step step, Section section)
        {
            _sessionService = viewModelContextProvider.GetSessionStateService();
            _sessionService.SetCurrentStep(step.Id, section.Id);
            var passage = _sessionService.ActiveSession?.PassageNumber == null
                ? section.Passages.First()
                : section.Passages.First(x => x.PassageNumber.Equals(_sessionService.ActiveSession.PassageNumber));

            var sessionPage = _sessionService.GetSessionPage(step.Id, section.Id, passage.PassageNumber);
            if (!string.IsNullOrEmpty(sessionPage))
            {
                return await GetViewModelFromSessionPage(viewModelContextProvider, sessionPage, step, section, passage, _sessionService);
            }
         
            return await FindDefaultViewModel(section, passage, step, viewModelContextProvider);
        }

        private async Task<ViewModelBase> GetViewModelFromSessionPage(IViewModelContextProvider viewModelContextProvider,
            string sessionPage,
            Step step,
            Section section,
            Passage passage,
            ISessionStateService sessionService)
        {
            if (sessionPage.Contains("RetellBTPassageTranslate"))
            {
                return await TranscribeRetellBackTranslateResolver.GetTranscribeRetellPassageTranslateViewModelAsync(
                    section,
                    passage,
                    passage.CurrentDraftAudio.RetellBackTranslationAudio,
                    step,
                    viewModelContextProvider);
            }
            
            if (sessionPage.Contains("SegmentTranslate"))
            {
                var segmentBackTranslation = FindSegmentBackTranslation(sessionService, passage);
                return await TranscribeSegmentBackTranslateResolver.GetSegmentTranslatePageViewModel(
                    section: section,
                    passage: passage, 
                    step: step, 
                    segmentBackTranslation: segmentBackTranslation, 
                    segmentName: string.Format(AppResources.Segment, GetSegmentNumber(section, segmentBackTranslation.Id)),
                    viewModelContextProvider: viewModelContextProvider);
            }

            return await FindDefaultViewModel(section, passage, step, viewModelContextProvider);
        }

        private async Task<ViewModelBase> FindDefaultViewModel(Section section, Passage passage,
            Step step, IViewModelContextProvider viewModelContextProvider)
        {
            if (step.StepSettings.GetSetting(SettingType.DoPassageTranscribe))
            {
                return await TranscribeRetellBackTranslateResolver.GetTranscribeRetellPassageSelectViewModelAsync(
                    section,
                    step,
                    viewModelContextProvider);
            }

            if (step.StepSettings.GetSetting(SettingType.DoSegmentTranscribe))
            {
                return await TranscribeSegmentBackTranslateResolver.GetSegmentSelectPageViewModel(
                    section,
                    step,
                    viewModelContextProvider);
            }
            return null;
        }
        
        private SegmentBackTranslation FindSegmentBackTranslation(
            ISessionStateService sessionStateService,
            Passage passage)
        {
            var segmentId = sessionStateService.ActiveSession.NonDraftTranslationId;
            return passage.CurrentDraftAudio.SegmentBackTranslationAudios.First(x => x.Id == segmentId);
        }

        private string GetSegmentNumber(Section section, Guid segmentBackTranslationId)
        {
            var segmentCount = 0;
            foreach (var breathPauseSegment in section.Passages.SelectMany(passage => passage.CurrentDraftAudio.SegmentBackTranslationAudios))
            {
                ++segmentCount;
                if (breathPauseSegment.Id == segmentBackTranslationId)
                {
                    break;
                }
            }

            return segmentCount.ToString();
        }
    }
}