using System.Reactive.Linq;
using DynamicData;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Render.Extensions;
using Render.Kernel;
using Render.Models.Sections;
using Render.Models.Workflow;
using Render.Models.Workflow.Stage;
using Render.Resources;
using Render.Resources.Localization;
using Render.Sequencer.Contracts.Enums;
using Render.Sequencer.Contracts.Interfaces;
using Render.Sequencer.Contracts.Models;

namespace Render.Pages.BackTranslator.RetellBackTranslate
{
    public class TabletRetellPassageSelectPageViewModel : WorkflowPageBaseViewModel
    {
        private int _audioToSelectIndex;
        private ActionViewModelBase SequencerActionViewModel { get; set; }
        
        [Reactive] 
        public ISequencerPlayerViewModel SequencerPlayerViewModel { get; private set; }

        public static TabletRetellPassageSelectPageViewModel Create(
            IViewModelContextProvider viewModelContextProvider,
            Step step,
            Section section,
            Stage stage)
        {
            var pageViewModel = new TabletRetellPassageSelectPageViewModel(
                viewModelContextProvider, 
                step, 
                section, 
                stage);
            
            pageViewModel.Initialize();

            return pageViewModel;
        }

        private TabletRetellPassageSelectPageViewModel(
            IViewModelContextProvider viewModelContextProvider,
            Step step,
            Section section,
            Stage stage) :
            base(urlPathSegment: "TabletRetellBTPassageSelect",
                viewModelContextProvider: viewModelContextProvider,
                pageName: GetStepName(step),
                section: section,
                stage: stage,
                step: step,
                secondPageName: AppResources.PassageSelect)
        {
            
            TitleBarViewModel.PageGlyph =
                ((FontImageSource)ResourceExtensions.GetResourceValue("RetellBackTranslateWhite"))?.Glyph;

            ProceedButtonViewModel.SetCommand(NavigateToSelectedDraftAsync);
            
            Disposables.Add(ProceedButtonViewModel.NavigateToPageCommand.IsExecuting
                .Subscribe(isExecuting => { IsLoading = isExecuting; }));
        }

        private void Initialize()
        {
            SetupSequencer();
        }

        private void SetupSequencer()
        {
            SequencerPlayerViewModel = ViewModelContextProvider
                .GetSequencerFactory()
                .CreatePlayer(playerFactory: ViewModelContextProvider.GetAudioPlayer);
            
            SequencerPlayerViewModel.LoadedCommand = ReactiveCommand.Create(TrySelectNextPassageToRetell);
            SequencerPlayerViewModel.IsRightToLeftDirection = FlowDirection is FlowDirection.RightToLeft;
            
            var audios = Section.Passages.Select(CreateAudioPlayerModel).ToArray();
            SetAudioToSelectIndex(audios);

            SequencerPlayerViewModel.SetAudio(audios);
            SequencerPlayerViewModel.SetupActivityService(ViewModelContextProvider, Disposables);

            SequencerActionViewModel = SequencerPlayerViewModel.CreateActionViewModel(
                required: Step.StepSettings.GetSetting(SettingType.RequireRetellBTSectionListen),
                requirementId: Section.Id,
                provider: ViewModelContextProvider,
                disposables: Disposables);
            ActionViewModelBaseSourceList.Add(SequencerActionViewModel);

            SequencerPlayerViewModel
                .WhenAnyValue(player => player.State)
                .Where(state => state == SequencerState.Playing)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ => SequencerActionViewModel.ActionState = ActionState.Optional);
        }

        private void SetAudioToSelectIndex(PlayerAudioModel[] audios)
        {
            var audioToSelect = audios.FirstOrDefault(audio => audio.Option is not AudioOption.Completed);
            
            _audioToSelectIndex = audioToSelect is null ? -1 : audios.IndexOf(audioToSelect);
        }

        private void TrySelectNextPassageToRetell()
        {
            SequencerPlayerViewModel.TrySelectAudio(_audioToSelectIndex);
        }

        private PlayerAudioModel CreateAudioPlayerModel(Passage passage)
        {
            AudioOption option;
            string endIcon = null;

            var playerAudio = Step.Role != Roles.BackTranslate2
                ? passage.CurrentDraftAudio
                : passage.CurrentDraftAudio.RetellBackTranslationAudio;

            var audioPath = ViewModelContextProvider
                .GetTempAudioService(playerAudio)
                .SaveTempAudio();

            bool passagesCheckmark = false;
            bool segmentsCheckmark = false;

            var pbtSetting = Step.StepSettings?.Settings.FirstOrDefault(ss => ss.SettingType == SettingType.DoRetellBackTranslate);                           
            var sbtSetting = Step.StepSettings?.Settings.FirstOrDefault(ss => ss.SettingType == SettingType.DoSegmentBackTranslate);

            var pbtRequired = pbtSetting?.Value ?? false;
            var sbtRequired = sbtSetting?.Value ?? false;

            var passageBackTranslationAudio = Step.Role != Roles.BackTranslate2
                ? passage.CurrentDraftAudio.RetellBackTranslationAudio
                : passage.CurrentDraftAudio.RetellBackTranslationAudio.RetellBackTranslationAudio;
            if (!pbtRequired || passageBackTranslationAudio != null && passageBackTranslationAudio.HasAudio)
            {
                passagesCheckmark = true;
            }

            var allSegmentBackTranslationAudios = passage.CurrentDraftAudio.SegmentBackTranslationAudios.ToList();
            var areAllSegmentsRecordedForFirstStep =
                Step.Role != Roles.Transcribe2 &&
                allSegmentBackTranslationAudios.Count > 0 &&
                allSegmentBackTranslationAudios.All(x => x.HasAudio);
            var areAllSegmentsRecordedForSecondStep =
                Step.Role == Roles.Transcribe2 &&
                allSegmentBackTranslationAudios.Count > 0 &&
                allSegmentBackTranslationAudios.All(x => x.HasAudio);
            if (!sbtRequired || areAllSegmentsRecordedForFirstStep || areAllSegmentsRecordedForSecondStep)
            {
                segmentsCheckmark = true;
            }

            if (passagesCheckmark && segmentsCheckmark)
            {
                option = AudioOption.Completed;
                endIcon = Icon.Checkmark.ToString();
            }
            else
            {
                option = AudioOption.Required;
            }

            return PlayerAudioModel.Create(
                path: audioPath,
                name: string.Format(AppResources.Passage, passage.PassageNumber.PassageNumberString),
                startIcon: Icon.PassageNew.ToString(),
                endIcon: endIcon,
                option: option,
                key: passage.Id,
                number: passage.PassageNumber.PassageNumberString);
        }

        private async Task<IRoutableViewModel> NavigateToSelectedDraftAsync()
        {
            try
            {
                var audio = SequencerPlayerViewModel.GetCurrentAudio();

                if (audio == null)
                {
                    return default;
                }

                var passage = Section.Passages.Single(passage => passage.Id == audio.Key);
                    
                if (passage.CurrentDraftAudio.RetellBackTranslationAudio == null)
                {
                    // TODO: Make these real language IDs
                    var newRetellBackTranslation = new RetellBackTranslation(passage.CurrentDraftAudio.Id,
                        Guid.Empty, Guid.Empty, Section.ProjectId, Section.ScopeId);
                        
                    await ViewModelContextProvider.GetRetellBackTranslationRepository()
                        .SaveAsync(newRetellBackTranslation);
                        
                    passage.CurrentDraftAudio.RetellBackTranslationAudio = newRetellBackTranslation;
                }

                var viewModel = TabletRetellPassageTranslatePageViewModel.Create(ViewModelContextProvider,
                    Step, Section, passage, passage.CurrentDraftAudio.RetellBackTranslationAudio, Stage);

                return await NavigateTo(viewModel);
            }
            catch (Exception e)
            {
                LogError(e);
                throw;
            }
        }

        public override void Dispose()
        {
            SequencerPlayerViewModel?.Dispose();
            SequencerPlayerViewModel = null;
            SequencerActionViewModel?.Dispose();
            SequencerActionViewModel = null;

            base.Dispose();
        }
    }
}