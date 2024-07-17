using ReactiveUI.Fody.Helpers;
using Render.Kernel;
using Render.Models.Sections;
using Render.Models.Workflow;
using Render.Models.Workflow.Stage;
using Render.Resources.Localization;
using System.Reactive.Linq;
using DynamicData;
using ReactiveUI;
using Render.Resources;
using Render.Extensions;
using Render.Sequencer.Contracts.Enums;
using Render.Sequencer.Contracts.Interfaces;
using Render.Sequencer.Contracts.Models;

namespace Render.Pages.Transcribe.TranscribeSegmentBackTranslate
{
    public class TranscribeSegmentSelectPageViewModel : WorkflowPageBaseViewModel
    {
        private readonly Dictionary<Guid, string> _segmentsDictionary = new Dictionary<Guid, string>();
        private readonly Dictionary<Guid, Guid> _segmentAudioSegmentBackTranslationDictionary = new Dictionary<Guid, Guid>();
        private readonly Dictionary<Guid, Guid> _segmentBackTranslationPassageDictionary = new Dictionary<Guid, Guid>();

        private SegmentBackTranslation _previouslyTranscribedSegment;
        
        private ActionViewModelBase SequencerActionViewModel { get; set; }

        private bool IsSecondTranscribe
        {
            get => Step?.Role is Roles.Transcribe2;
        }
        
        [Reactive] 
        public ISequencerPlayerViewModel SequencerPlayerViewModel { get; private set; }

        public static async Task<TranscribeSegmentSelectPageViewModel> CreateAsync(
            Step step,
            Section section,
            IViewModelContextProvider viewModelContextProvider,
            Stage stage,
            SegmentBackTranslation previousSegmentBackTranslation = null)
        {
            // WorkflowPageBaseViewModel base constructor initializes SessionStateService and should be invoked at the beginning
            var pageVm = new TranscribeSegmentSelectPageViewModel(
                viewModelContextProvider,
                step,
                section,
                stage,
                previousSegmentBackTranslation);

            pageVm.Initialize();
            
            return pageVm;
        }

        private TranscribeSegmentSelectPageViewModel(
            IViewModelContextProvider viewModelContextProvider,
            Step step,
            Section section,
            Stage stage,
            SegmentBackTranslation previousSegmentBackTranslation = null)
            : base(
                urlPathSegment: "TabletTranscribeSegmentSelect",
                viewModelContextProvider: viewModelContextProvider,
                pageName: AppResources.Transcribe,
                section: section,
                stage: stage,
                step: step,
                secondPageName: AppResources.SegmentSelect)
        {
            DisposeOnNavigationCleared = true;
            TitleBarViewModel.DisposeOnNavigationCleared = true;

            _previouslyTranscribedSegment = previousSegmentBackTranslation;
            
            TitleBarViewModel.PageGlyph = IconExtensions
                .BuildFontImageSource(Icon.Transcribe, ResourceExtensions.GetColor("SecondaryText"))?.Glyph;
            
            ProceedButtonViewModel.SetCommand(NavigateToSelectedBreathPauseSegmentAsync);
            
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
            
            SequencerPlayerViewModel.LoadedCommand = ReactiveCommand.Create(TrySelectNextSegmentToTranslate);
            SequencerPlayerViewModel.IsRightToLeftDirection = FlowDirection is FlowDirection.RightToLeft;
            
            SequencerPlayerViewModel.SetAudio(GetAudios());
            SequencerPlayerViewModel.SetupActivityService(ViewModelContextProvider, Disposables);
            
            SequencerActionViewModel = SequencerPlayerViewModel.CreateActionViewModel(
                required: Step.StepSettings.GetSetting(SettingType.RequireSegmentTranscribeListen),
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

        private void TrySelectNextSegmentToTranslate()
        {
            var orderedBackTranslations = Section.Passages
                .SelectMany(p => p.CurrentDraftAudio.SegmentBackTranslationAudios.OrderBy(s => s.TimeMarkers.StartMarkerTime))
                .ToList();

            Func<SegmentBackTranslation, bool> segmentSelectCondidtion = IsSecondTranscribe ? 
                (segment) => string.IsNullOrEmpty(segment.RetellBackTranslationAudio?.Transcription) : 
                (segment) => string.IsNullOrEmpty(segment.Transcription);

            var indexToSelect = Utilities.Utilities.GetSegmentIndexToSelect(
                backTranslations: orderedBackTranslations,
                previousBackTranslation: _previouslyTranscribedSegment,
                nextSegmentCondition: segmentSelectCondidtion);

            SequencerPlayerViewModel.TrySelectAudio(indexToSelect);
        }

        private PlayerAudioModel[] GetAudios()
        {
            var audioList = new List<PlayerAudioModel>();
            
            var segmentNumber = 1;
            
            foreach (var passage in Section.Passages)
            {
                var orderedSegmentBackTranslations =
                    passage
                        .CurrentDraftAudio
                        .SegmentBackTranslationAudios
                        .OrderBy(x => x.TimeMarkers.StartMarkerTime).ToList();
                
                foreach (var segmentBackTranslation in orderedSegmentBackTranslations)
                {
                    var segmentName = string.Format(AppResources.Segment, segmentNumber);
                    
                    AudioOption option;
                    string endIcon = null;

                    PlayerAudioModel audio = null;
                    
                    if (IsSecondTranscribe is false)
                    {
                        if (segmentBackTranslation.HasAudio)
                        {
                            if (string.IsNullOrEmpty(segmentBackTranslation.Transcription))
                            {
                                option = AudioOption.Required;
                            }
                            else
                            {
                                option = AudioOption.Completed;
                                endIcon = Icon.Checkmark.ToString();
                            }
                            
                            audio = segmentBackTranslation.CreatePlayerAudioModel(
                                audioType: ParentAudioType.SegmentBackTranslation,
                                name: segmentNumber.ToString(),
                                ViewModelContextProvider.GetTempAudioService(segmentBackTranslation).SaveTempAudio(),
                                endIcon: endIcon,
                                option: option,
                                number: segmentNumber.ToString());
                        }
                    }
                    else  // Step.Role == Roles.Transcribe2
                    {
                        if (segmentBackTranslation.RetellBackTranslationAudio != null &&
                            segmentBackTranslation.RetellBackTranslationAudio.HasAudio)
                        {
                            if(string.IsNullOrEmpty(segmentBackTranslation.RetellBackTranslationAudio.Transcription))
                            {
                                option = AudioOption.Required;
                            }
                            else
                            {
                                option = AudioOption.Completed;
                                endIcon = Icon.Checkmark.ToString();
                            }
                            
                            audio = segmentBackTranslation.RetellBackTranslationAudio.CreatePlayerAudioModel(
                                audioType: ParentAudioType.SegmentBackTranslation2,
                                name: segmentNumber.ToString(),
                                ViewModelContextProvider.GetTempAudioService(segmentBackTranslation.RetellBackTranslationAudio).SaveTempAudio(),
                                endIcon: endIcon,
                                option: option,
                                number: segmentNumber.ToString());
                        }
                    }

                    if (audio != null)
                    {
                        audioList.Add(audio);
                        _segmentAudioSegmentBackTranslationDictionary.Add(audio.Key, segmentBackTranslation.Id);
                        _segmentsDictionary.Add(segmentBackTranslation.Id, segmentName);
                        _segmentBackTranslationPassageDictionary.Add(segmentBackTranslation.Id, passage.Id);
                        segmentNumber++;
                    }
                }
            }

			return audioList.ToArray();
		}

        protected async Task<IRoutableViewModel> NavigateToSelectedBreathPauseSegmentAsync()
        {
            try
            {
                var audio = SequencerPlayerViewModel.GetCurrentAudio();

                if (audio is null)
                {
                    return default;
                }

                var segmentBackTranslationId = _segmentAudioSegmentBackTranslationDictionary[audio.Key];

                var passage =  Section.Passages.Single(p =>
                    p.Id == _segmentBackTranslationPassageDictionary[segmentBackTranslationId]);
                
                var segment = passage.CurrentDraftAudio.SegmentBackTranslationAudios.SingleOrDefault(p =>
                        p.Id == segmentBackTranslationId);

                if (segment is null)
                {
                    return default;
                }
                
                //Check if all segments have been transcribed.
                var allSegmentBackTranslationAudios =
                    Section.Passages.SelectMany(x => x.CurrentDraftAudio.SegmentBackTranslationAudios).ToList();
                if (Step.Role != Roles.Transcribe2 && allSegmentBackTranslationAudios.All(x =>
                        x.Transcription != null) ||
                    Step.Role == Roles.Transcribe2 && allSegmentBackTranslationAudios.All(x =>
                        x.RetellBackTranslationAudio?.Transcription != null))
                {
                    await ViewModelContextProvider.GetGrandCentralStation().AdvanceSectionAsync(Section, Step);
                    return await NavigateToHomeOnMainStackAsync();
                }

                var vm = await TranscribeSegmentBackTranslateResolver.GetSegmentTranslatePageViewModel(
                    Section, passage, Step,
                    segment, _segmentsDictionary[segment.Id],
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
			var allSegmentBackTranslationAudios =
				   Section.Passages.SelectMany(x => x.CurrentDraftAudio.SegmentBackTranslationAudios).ToList();
			var areAllSegmentsTransribedForFirstStep = Step.Role != Roles.Transcribe2 && allSegmentBackTranslationAudios.All(x =>
					x.Transcription != null);
			var areAllSegmentsTransribedForSecondStep = Step.Role == Roles.Transcribe2 && allSegmentBackTranslationAudios.All(x =>
					x.RetellBackTranslationAudio?.Transcription != null);
			if (areAllSegmentsTransribedForFirstStep || areAllSegmentsTransribedForSecondStep)
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