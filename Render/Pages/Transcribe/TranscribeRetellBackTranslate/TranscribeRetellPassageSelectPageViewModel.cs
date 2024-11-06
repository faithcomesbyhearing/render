using System.Reactive.Linq;
using DynamicData;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Render.Extensions;
using Render.Kernel;
using Render.Models.Sections;
using Render.Models.Workflow;
using Render.Models.Workflow.Stage;
using Render.Pages.Transcribe.TranscribeSegmentBackTranslate;
using Render.Resources;
using Render.Resources.Localization;
using Render.Sequencer.Contracts.Enums;
using Render.Sequencer.Contracts.Interfaces;
using Render.Sequencer.Contracts.Models;

namespace Render.Pages.Transcribe.TranscribeRetellBackTranslate
{
    public class TranscribeRetellPassageSelectPageViewModel : WorkflowPageBaseViewModel
    {
        private int _audioToSelectIndex;
        private ActionViewModelBase SequencerActionViewModel { get; set; }

        [Reactive] public ISequencerPlayerViewModel SequencerPlayerViewModel { get; private set; }
        
        public static TranscribeRetellPassageSelectPageViewModel Create(
            IViewModelContextProvider viewModelContextProvider,
            Step step,
            Section section,
            Stage stage)
        {
            // WorkflowPageBaseViewModel base constructor initializes SessionStateService and should be invoked at the beginning
            var pageVm = new TranscribeRetellPassageSelectPageViewModel(viewModelContextProvider, step, section, stage);
                
            pageVm.Initialize();

            return pageVm;
        }
        
        private TranscribeRetellPassageSelectPageViewModel(
            IViewModelContextProvider viewModelContextProvider,
            Step step,
            Section section,
            Stage stage) :
            base(
                urlPathSegment: "TabletTranscribeRetellBTPassageSelect",
                viewModelContextProvider: viewModelContextProvider,
                pageName: GetStepName(step),
                section: section,
                stage: stage,
                step: step,
                secondPageName: AppResources.PassageSelect)
        {

            TitleBarViewModel.PageGlyph = IconExtensions
                .BuildFontImageSource(Icon.Transcribe, ResourceExtensions.GetColor("SecondaryText"))?.Glyph;
            
            ProceedButtonViewModel.SetCommand(NavigateToSelectedDraftAsync);
            
            Disposables.Add(ProceedButtonViewModel.NavigateToPageCommand.IsExecuting
                .Subscribe(isExecuting => { IsLoading = isExecuting; }));
        }

        private void Initialize()
        {
            SetupSequencer();
            SetProceedButtonIcon();
		}

        private void SetupSequencer()
        {
            SequencerPlayerViewModel = ViewModelContextProvider
                .GetSequencerFactory()
                .CreatePlayer(ViewModelContextProvider.GetAudioPlayer);

            SequencerPlayerViewModel.LoadedCommand = ReactiveCommand.Create(TrySelectNextPassageToRetell);
            SequencerPlayerViewModel.IsRightToLeftDirection = FlowDirection is FlowDirection.RightToLeft;
            
            var audios = GetAudios();
            SetAudioToSelectIndex(audios);

            SequencerPlayerViewModel.SetAudio(audios);
            SequencerPlayerViewModel.SetupActivityService(ViewModelContextProvider, Disposables);

            SequencerActionViewModel = SequencerPlayerViewModel.CreateActionViewModel(
                required: Step.StepSettings.GetSetting(SettingType.RequirePassageTranscribeListen),
                requirementId: Section.Id,
                provider: ViewModelContextProvider, 
                disposables: Disposables);
            ActionViewModelBaseSourceList.Add(SequencerActionViewModel);

            SequencerPlayerViewModel
                .WhenAnyValue(player => player.State)
                .Where(state => state == SequencerState.Playing)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ => {
                    SequencerActionViewModel.ActionState = ActionState.Optional;
                });
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

        private PlayerAudioModel[] GetAudios()
        {
            var audioList = new List<PlayerAudioModel>();

            foreach (var passage in Section.Passages)
            {
                AudioOption option;
                string endIcon = null;

                PlayerAudioModel audio = null;
                
                if (Step.Role != Roles.Transcribe2)
                {
                    if (passage.CurrentDraftAudio.RetellBackTranslationAudio != null &&
                        passage.CurrentDraftAudio.RetellBackTranslationAudio.HasAudio)
                    {
                        if (string.IsNullOrEmpty(passage.CurrentDraftAudio.RetellBackTranslationAudio.Transcription))
                        {
                            option = AudioOption.Required;
                        }
                        else
                        {
                            option = AudioOption.Completed;
                            endIcon = Icon.Checkmark.ToString();
                        }

                        audio = passage.CurrentDraftAudio.RetellBackTranslationAudio
                            .CreatePlayerAudioModel(
                                audioType: ParentAudioType.PassageBackTranslation,
                                name: passage.PassageNumber.PassageNumberString,
                                ViewModelContextProvider
                                    .GetTempAudioService(passage.CurrentDraftAudio.RetellBackTranslationAudio)
                                    .SaveTempAudio(),
                                endIcon: endIcon,
                                option: option,
                                userId: ViewModelContextProvider.GetLoggedInUser().Id);
                    }
                }
                else // Step.Role == Roles.Transcribe2
                {
                    if (passage.CurrentDraftAudio.RetellBackTranslationAudio != null &&
                        passage.CurrentDraftAudio.RetellBackTranslationAudio.RetellBackTranslationAudio != null &&
                        passage.CurrentDraftAudio.RetellBackTranslationAudio.RetellBackTranslationAudio.HasAudio)
                    {
                        if (string.IsNullOrEmpty(passage.CurrentDraftAudio.RetellBackTranslationAudio
                                .RetellBackTranslationAudio.Transcription))
                        {
                            option = AudioOption.Required;
                        }
                        else
                        {
                            option = AudioOption.Completed;
                            endIcon = Icon.Checkmark.ToString();
                        }
                        
                        audio = passage.CurrentDraftAudio.RetellBackTranslationAudio.RetellBackTranslationAudio
                            .CreatePlayerAudioModel(
                                audioType: ParentAudioType.PassageBackTranslation2,
                                name: passage.PassageNumber.PassageNumberString,
                                ViewModelContextProvider
                                    .GetTempAudioService(passage.CurrentDraftAudio.RetellBackTranslationAudio.RetellBackTranslationAudio)
                                    .SaveTempAudio(),
                                endIcon: endIcon,
                                option: option,
                                userId: ViewModelContextProvider.GetLoggedInUser().Id);
                    }
                }

                if (audio != null)
                {
                    audioList.Add(audio);    
                }
            }
            
            return audioList.ToArray();
        }
        
        private async Task<IRoutableViewModel> NavigateToSelectedDraftAsync()
        {
            try
            {
                var audio = SequencerPlayerViewModel.GetCurrentAudio();

                if (audio is null)
                {
                    return default;
                }

                Passage passage;

                if (Step.Role != Roles.Transcribe2)
                {
                    passage = Section.Passages.SingleOrDefault(p => p.CurrentDraftAudio?.RetellBackTranslationAudio?.Id == audio.Key);
                }
                else // Step.Role = Roles.Transcribe2
                {
                    passage = Section.Passages.SingleOrDefault(p =>
                        p.CurrentDraftAudio?.RetellBackTranslationAudio?.RetellBackTranslationAudio?.Id == audio.Key);
                }

                if (passage is null)
                {
                    return default;
                }
                
                //Check if all passages have been transcribed.
                if (Step.Role != Roles.Transcribe2 && Section.Passages.All(x =>
                        x.CurrentDraftAudio.RetellBackTranslationAudio.Transcription != null) ||
                    Step.Role == Roles.Transcribe2 && Section.Passages.All(x =>
                        x.CurrentDraftAudio.RetellBackTranslationAudio.RetellBackTranslationAudio?.Transcription != null))
                {
                    if (Section.Passages.All(x => x.CurrentDraftAudio.SegmentBackTranslationAudios.Count != 0) &&
                        Step.StepSettings.GetSetting(SettingType.DoSegmentTranscribe))
                    {
                        var viewModel = await TranscribeSegmentBackTranslateResolver.GetSegmentSelectPageViewModel(
                            Section, Step,
                            ViewModelContextProvider);
                        
                        return await NavigateTo(viewModel);
                    }
                    
                    var sectionMovementService = ViewModelContextProvider.GetSectionMovementService();
                    await sectionMovementService.AdvanceSectionAsync(Section, Step, GetProjectId(), GetLoggedInUserId()); 
                    
                    return await NavigateToHomeOnMainStackAsync();
                }

                var vm = await TranscribeRetellBackTranslateResolver.GetTranscribeRetellPassageTranslateViewModelAsync(
                    Section, passage, passage.CurrentDraftAudio.RetellBackTranslationAudio, Step,
                    ViewModelContextProvider);
                
                return await NavigateTo(vm);
            }
            catch (Exception e)
            {
                LogError(e);
                throw;
            }
        }

		protected sealed override void SetProceedButtonIcon()
		{
            var isSegmentTransribeSettingDisabled = Step.StepSettings.GetSetting(SettingType.DoSegmentTranscribe) is false;
            var areAllPassagesTransribedForFirstStep = Step.Role != Roles.Transcribe2 && Section.Passages.All(x =>
                        x.CurrentDraftAudio.RetellBackTranslationAudio.Transcription != null);
			var areAllPassagesTransribedForSecondStep = Step.Role == Roles.Transcribe2 && Section.Passages.All(x =>
						x.CurrentDraftAudio.RetellBackTranslationAudio.RetellBackTranslationAudio?.Transcription != null);
			if (isSegmentTransribeSettingDisabled && (areAllPassagesTransribedForFirstStep ||
					areAllPassagesTransribedForSecondStep))
			{
				ProceedButtonViewModel.IsCheckMarkIcon = true;
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