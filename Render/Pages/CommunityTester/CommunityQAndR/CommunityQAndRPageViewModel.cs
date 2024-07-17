using System.Reactive.Linq;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Render.Components.BarPlayer;
using Render.Components.MiniWaveformPlayer;
using Render.Kernel;
using Render.Models.Sections;
using Render.Models.Sections.CommunityCheck;
using Render.Models.Workflow;
using Render.Models.Workflow.Stage;
using Render.Repositories.Audio;
using Render.Resources.Localization;
using Render.Resources;
using Render.Resources.Styles;
using Render.Sequencer.Contracts.Interfaces;
using Render.Services.SessionStateServices;
using DynamicData;
using Render.Extensions;
using Render.Models.Audio;
using Render.Sequencer.Contracts.Models;
using Render.Sequencer.Contracts.Enums;
using Render.Pages.CommunityTester.CommunityRetell;

namespace Render.Pages.CommunityTester.CommunityQAndR
{
    public class CommunityQAndRPageViewModel : WorkflowPageBaseViewModel
    {
        private readonly IAudioRepository<Response> _responseRepository;
        private readonly bool _requireResponseRecord;
        private readonly bool _requireContextListen;
        private readonly bool _isSectionAudioListened;
        private readonly bool _doCommunityRetell;
        private readonly bool _requireSectionListen;
        private Step _step;
        private Passage _passage;
        private CommunityTest _communityTest;
        private Flag _currentFlag;
        private Question _currentQuestion;
        private Response _response;

        [Reactive] public IMiniWaveformPlayerViewModel AudioClipPlayer { get; private set; }
        [Reactive] public bool ShowAudioClipPlayer { get; private set; }
		[Reactive] public bool ShowQuestionPlayer { get; private set; }
		[Reactive] public IMiniWaveformPlayerViewModel SectionPlayer { get; private set; }
        [Reactive] public IMiniWaveformPlayerViewModel PassagePlayer { get; private set; }
        [Reactive] public IBarPlayerViewModel QuestionPlayer { get; private set; }
        [Reactive] public ISequencerRecorderViewModel SequencerRecorderViewModel { get; private set; }
        public ActionViewModelBase SequencerActionViewModel { get; private set; }

        public static CommunityQAndRPageViewModel CreateFromSession(ISessionStateService sessionStateService,
            IViewModelContextProvider viewModelContextProvider,
            Section section,
            Passage passage,
            Stage stage,
            Step step)
        {
            // Selects the first question that belongs to the current stage 
            var question = passage.CurrentDraftAudio.GetCommunityCheck().GetQuestions(stage.Id).FirstOrDefault();

            return Create(viewModelContextProvider, section, passage, stage, step, question);
        }

        public static CommunityQAndRPageViewModel Create(IViewModelContextProvider viewModelContextProvider,
            Section section,
            Passage passage,
            Stage stage,
            Step step,
            Question currentQuestion = null,
            bool isSectionAudioListened = false)
        {
            var communityTest = passage.CurrentDraftAudio.GetCommunityCheck();
            var question = currentQuestion ?? communityTest.GetQuestions(stage.Id).FirstOrDefault();

            // WorkflowPageBaseViewModel base constructor initializes SessionStateService and should be invoked at the beginning
            var pageVm = new CommunityQAndRPageViewModel(viewModelContextProvider, section, passage, stage, step, communityTest, question, isSectionAudioListened);

            pageVm.Initialize();

            return pageVm;
        }

        private CommunityQAndRPageViewModel(IViewModelContextProvider viewModelContextProvider,
            Section section,
            Passage passage,
            Stage stage,
            Step step,
            CommunityTest communityTest,
            Question question,
            bool isSectionAudioListened)
            : base("CommunityQAndRPage", viewModelContextProvider,
                GetStepName(viewModelContextProvider, RenderStepTypes.CommunityTest, stage.Id),
                section,
                stage,
                step,
                passage.PassageNumber,
                secondPageName: AppResources.RecordResponse,
                nonDraftTranslationId: question?.Id ?? default)
        {
            DisposeOnNavigationCleared = true;
            TitleBarViewModel.DisposeOnNavigationCleared = true;

            _responseRepository = viewModelContextProvider.GetResponseRepository();

            _isSectionAudioListened = isSectionAudioListened;
            _passage = passage;
            _step = step;
            _requireContextListen = step.StepSettings.GetSetting(SettingType.RequireQuestionContextListen);
            _requireResponseRecord = step.StepSettings.GetSetting(SettingType.RequireRecordResponse);
            _doCommunityRetell = step.StepSettings.GetSetting(SettingType.DoCommunityRetell);
            _requireSectionListen = step.StepSettings.GetSetting(SettingType.RequireSectionListen);

            _communityTest = communityTest;
            _currentQuestion = question;
			var communityCheck = passage.CurrentDraftAudio.GetCommunityCheck();
			ShowQuestionPlayer = _currentQuestion is not null;
			if (ShowQuestionPlayer)
            {
				_currentFlag = _communityTest.GetCurrentFlag(_currentQuestion) ?? null;
				_response = _currentQuestion.Responses.FirstOrDefault(x => x.StageId == Stage.Id);

				if (_response == null)
				{
					_response = new Response(Stage.Id, Section.ScopeId, Section.ProjectId, _currentQuestion.Id);
					_currentQuestion.AddResponse(_response);
				}
			}
            else
            {
				_currentQuestion = new Question(Stage.Id);
				var response = passage.CurrentDraftAudio.GetCommunityCheck().GetResponses(stage.Id).FirstOrDefault();
				
				_response = response ?? new Response(Stage.Id, Section.ScopeId, Section.ProjectId, communityCheck.Id);
				_currentQuestion.AddResponse(_response);
			}
        }

        private void Initialize()
        {
            ProceedButtonViewModel.SetCommand(NavigateForwardOrHomeAsync);
            Disposables.Add(ProceedButtonViewModel.NavigateToPageCommand.IsExecuting
                .Subscribe(isExecuting => { IsLoading = isExecuting; }));

            var color = (ColorReference)ResourceExtensions.GetResourceValue("SecondaryText");
            if (color != null)
            {
                TitleBarViewModel.PageGlyph = IconExtensions.BuildFontImageSource(Icon.CommunityCheck,
                    color.Color, 35)?.Glyph;
            }

            SetProceedButtonIcon();

            InitializeSectionAndPassagePlayers();
            if (_currentQuestion is not null && _currentQuestion.QuestionAudio is not null)
            {
				InitializeAudioClipPlayer();
				InitializeQuestionPlayer();
			}

			InitializeAudioRecorderViewModel();
		}

        private void InitializeSectionAndPassagePlayers()
        {
            SectionPlayer = ViewModelContextProvider.GetMiniWaveformPlayerViewModel(
                audio: new AudioPlayback(Section.Id, Section.Passages.Select(x => x.CurrentDraftAudio)),
                title: string.Format(AppResources.Section, Section.Number),
                actionState: _doCommunityRetell is false && _requireSectionListen && _isSectionAudioListened is false ? ActionState.Required : ActionState.Optional,
                glyph: ResourceExtensions.GetResourceValue<string>(Icon.SectionNew.ToString()));
            ActionViewModelBaseSourceList.Add(SectionPlayer);

            PassagePlayer = ViewModelContextProvider.GetMiniWaveformPlayerViewModel(_passage.CurrentDraftAudio,
                ActionState.Optional, string.Format(AppResources.Passage, _passage.PassageNumber.PassageNumberString),
                glyph: ResourceExtensions.GetResourceValue<string>(Icon.PassageNew.ToString()));
        }

        private void InitializeAudioClipPlayer()
        {
            if (_currentFlag?.TimeMarker > 0)
            {
                var priorFlag = _communityTest.GetFlags(Stage.Id).LastOrDefault(x => x.TimeMarker < _currentFlag.TimeMarker);
                var startTimeInMilliseconds = 1000 * (priorFlag?.TimeMarker ?? 0);
                var endTimeInMilliseconds = 1000 * _currentFlag.TimeMarker;

                AudioClipPlayer = ViewModelContextProvider.GetMiniWaveformPlayerViewModel(
                    audio: _passage.CurrentDraftAudio,
                    actionState: _requireContextListen ? ActionState.Required : ActionState.Optional,
                    title: AppResources.AudioClip,
                    timeMarkers: new TimeMarkers((int)startTimeInMilliseconds, (int)endTimeInMilliseconds));

                ActionViewModelBaseSourceList.Add(AudioClipPlayer);
            }

            ShowAudioClipPlayer = AudioClipPlayer != null;
        }

        private void InitializeQuestionPlayer()
        {
            var allQuestions = _communityTest.GetFlags(Stage.Id).SelectMany(x => x.Questions)
                .Where(x => x.StageIds.Contains(Stage.Id)).ToList();
            var questionNumber = allQuestions.IndexOf(_currentQuestion) + 1;

            QuestionPlayer = ViewModelContextProvider.GetBarPlayerViewModel(
                audio: _currentQuestion.QuestionAudio,
                actionState: _requireResponseRecord ? ActionState.Required : ActionState.Optional,
                title: string.Format(AppResources.QuestionNumberAndTotalNumberFormatted, questionNumber, allQuestions.Count),
                barPlayerPosition: 0,
                glyph: ResourceExtensions.GetResourceValue<string>(Icon.Flag.ToString()));

            ActionViewModelBaseSourceList.Add(QuestionPlayer);
        }

        private void InitializeAudioRecorderViewModel()
        {
            SequencerRecorderViewModel = ViewModelContextProvider.GetSequencerFactory().CreateRecorder(
                playerFactory: ViewModelContextProvider.GetAudioPlayer,
                recorderFactory: () => ViewModelContextProvider.GetAudioRecorderFactory().Invoke(48000));

            SequencerRecorderViewModel.IsRightToLeftDirection = FlowDirection is FlowDirection.RightToLeft;
            SequencerRecorderViewModel.AllowAppendRecordMode = true;

            SequencerRecorderViewModel.SetupActivityService(ViewModelContextProvider, Disposables);
            SequencerRecorderViewModel.SetupRecordPermissionPopup(ViewModelContextProvider, Logger);
            SequencerRecorderViewModel.SetupOnRecordFailedPopup(ViewModelContextProvider, Logger);

            SequencerRecorderViewModel.OnDeleteRecordCommand = ReactiveCommand.Create(DeleteResponseAudio);
            SequencerRecorderViewModel.OnUndoDeleteRecordCommand = ReactiveCommand.Create(SaveResponseAudio);
            SequencerRecorderViewModel.OnRecordingFinishedCommand = ReactiveCommand.Create(SaveResponseAudio);

            SequencerActionViewModel = SequencerRecorderViewModel.CreateActionViewModel(ViewModelContextProvider, Disposables, required: _requireResponseRecord);

            if (_response is not null && _response.Data.Length is not 0)
            {
                SequencerRecorderViewModel.SetRecord(RecordAudioModel.Create(
                    path: ViewModelContextProvider.GetTempAudioService(_response).SaveTempAudio(),
                    name: string.Empty));
                SequencerActionViewModel.ActionState = ActionState.Optional;
            }

            SequencerRecorderViewModel
                .WhenAnyValue(s => s.State)
                .Subscribe(observer =>
                {
                    if (observer == SequencerState.Recording)
                    {
                        SequencerActionViewModel.ActionState = ActionState.Required;
                    }
                });

            Disposables.Add(_response
                .WhenAnyValue(r => r.Data)
                .Subscribe(_ =>
                {
                    var actionState = !(_response.Data.Length > 0) && _requireResponseRecord
                        ? ActionState.Required
                        : ActionState.Optional;

                    SequencerActionViewModel.ActionState = actionState;
                }));


            ActionViewModelBaseSourceList.Add(SequencerActionViewModel);
        }

        protected sealed override void SetProceedButtonIcon()
        {
            var nextQuestion = _currentQuestion is not null ? _communityTest.GetNextQuestion(Stage.Id, _currentQuestion) : null;

            var nextPassage = Section.GetNextPassage(_passage);
            if (nextQuestion == null && nextPassage == null)
            {
                ProceedButtonViewModel.IsCheckMarkIcon = true;
            }
        }

        private async void SaveResponseAudio()
        {
            IsLoading = true;

            await Task.Run(async () =>
            {
                var record = SequencerRecorderViewModel.GetRecord();
                var audioDetails = SequencerRecorderViewModel.AudioDetails;
                var audioStream = (Stream)null;
                if (record.HasData)
                {
                    audioStream = new MemoryStream(record.Data);
                }
                else if (record.IsFileExists)
                {
                    audioStream = new FileStream(record.Path, FileMode.Open, FileAccess.Read);
                }
                else
                {
                    //TODO: Handle empty audio, show error to user.
                    throw new InvalidOperationException("Audio does not contain data or temporary file path");
                }

                await using (audioStream)
                {
                    var audioData = ViewModelContextProvider.GetAudioEncodingService().ConvertWavToOpus(
                        wavStream: audioStream,
                        sampleRate: audioDetails.SampleRate,
                        channelCount: audioDetails.ChannelCount);
                    _response.SetAudio(audioData);
                    await _responseRepository.SaveAsync(_response);
                }
            });

            IsLoading = false;
        }

        private void DeleteResponseAudio()
        {
            Task.Factory.StartNew(async () =>
            {
                await _responseRepository.DeleteAudioByIdAsync(_response.Id);
            });
			SequencerActionViewModel.ActionState = _requireResponseRecord ? ActionState.Required : ActionState.Optional;
		}

        private async Task<IRoutableViewModel> NavigateForwardOrHomeAsync()
        {
            var nextQuestion = _communityTest.GetNextQuestion(Stage.Id, _currentQuestion);
            if (nextQuestion != null)
            {
                var vm = await Task.Run(() => Create(
                    viewModelContextProvider: ViewModelContextProvider,
                    section: Section,
                    passage: _passage,
                    stage: Stage,
                    step: _step,
                    currentQuestion: nextQuestion,
                    isSectionAudioListened: true));
                return await NavigateTo(vm);
            }

            var nextPassage = Section.GetNextPassage(_passage);
            if (nextPassage != null)
            {
                if (_doCommunityRetell)
                {
                    var vm = await Task.Run(() => new CommunityRetellPageViewModel(
                        viewModelContextProvider: ViewModelContextProvider,
                        section: Section,
                        passage: nextPassage,
                        stage: Stage,
                        step: _step,
                        isSectionAudioListened: true));
                    return await NavigateTo(vm);
                }
                else
                {
                    var vm = await Task.Run(() => Create(
                        viewModelContextProvider: ViewModelContextProvider,
                        section: Section,
                        passage: nextPassage,
                        stage: Stage,
                        step: _step,
                        isSectionAudioListened: true));
                    return await NavigateTo(vm);
                }
            }

            await Task.Run(async () =>
            {
                var gcs = ViewModelContextProvider.GetGrandCentralStation();
                await gcs.AdvanceSectionAsync(Section, _step);
            });
            return await NavigateToHomeOnMainStackAsync();
        }

        public override void Dispose()
        {
            _currentFlag = null;
            _passage = null;
            _communityTest = null;
            _response = null;
            _currentQuestion = null;
            _step = null;

            AudioClipPlayer?.Dispose();
            SectionPlayer?.Dispose();
            PassagePlayer?.Dispose();
            QuestionPlayer?.Dispose();
            SequencerRecorderViewModel?.Dispose();
            SequencerRecorderViewModel = null;
            SequencerActionViewModel?.Dispose();
            SequencerActionViewModel = null;

            _responseRepository?.Dispose();

            base.Dispose();
        }
    }
}