using System.Reactive.Linq;
using DynamicData;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Render.Components.BarPlayer;
using Render.Components.MiniWaveformPlayer;
using Render.Components.NoteDetail;
using Render.Extensions;
using Render.Kernel;
using Render.Models.Audio;
using Render.Models.Sections;
using Render.Models.Workflow;
using Render.Models.Workflow.Stage;
using Render.Pages.BackTranslator.SegmentBackTranslate;
using Render.Repositories.Audio;
using Render.Repositories.Extensions;
using Render.Resources;
using Render.Resources.Localization;
using Render.Sequencer.Contracts.Enums;
using Render.Sequencer.Contracts.Interfaces;

namespace Render.Pages.BackTranslator.RetellBackTranslate
{
    public class TabletRetellPassageTranslatePageViewModel : WorkflowPageBaseViewModel
    {
        private readonly IAudioRepository<RetellBackTranslation> _retellRepository;
        private RetellBackTranslation _retellBackTranslation;
        private Passage _passage;
        private readonly string _passageTitle;

        private SequencerNoteDetailViewModel SequencerNoteDetailViewModel { get; set; }
        private ActionViewModelBase SequencerActionViewModel { get; set; }

        public IMiniWaveformPlayerViewModel MiniWaveformPlayerViewModel { get; private set; }
        public IBarPlayerViewModel BarPlayerViewModel { get; set; }
        [Reactive] public ISequencerRecorderViewModel SequencerRecorderViewModel { get; private set; }
        [Reactive] public bool IsTwoStepBackTranslate { get; set; }

        public static async Task<TabletRetellPassageTranslatePageViewModel> CreateAsync(
            IViewModelContextProvider viewModelContextProvider,
            Step step,
            Section section,
            Passage passage,
            RetellBackTranslation retellBackTranslation,
            Stage stage)
        {
            var pageViewModel = new TabletRetellPassageTranslatePageViewModel(
                viewModelContextProvider: viewModelContextProvider,
                step: step,
                section: section,
                passage: passage,
                retellBackTranslation: retellBackTranslation,
                stage: stage);

            pageViewModel.Initialize();

            return pageViewModel;
        }

        private TabletRetellPassageTranslatePageViewModel(
            IViewModelContextProvider viewModelContextProvider,
            Step step,
            Section section,
            Passage passage,
            RetellBackTranslation retellBackTranslation,
            Stage stage) :
            base(urlPathSegment: "TabletRetellBTPassageTranslate",
                viewModelContextProvider: viewModelContextProvider,
                pageName: AppResources.BackTranslate,
                section: section,
                stage: stage,
                step: step,
                passageNumber: passage.PassageNumber,
                nonDraftTranslationId: retellBackTranslation.Id,
                secondPageName: AppResources.PassageBackTranslate)
        {
            _passage = passage;
            _retellBackTranslation = retellBackTranslation;
            _passageTitle = string.Format(AppResources.Passage, passage.PassageNumber.PassageNumberString);

            IsTwoStepBackTranslate = Step.Role == Roles.BackTranslate2;

            _retellRepository = viewModelContextProvider.GetRetellBackTranslationRepository();

            DisposeOnNavigationCleared = true;
            TitleBarViewModel.DisposeOnNavigationCleared = true;

            TitleBarViewModel.PageGlyph = IconExtensions
                .BuildFontImageSource(Icon.RetellBackTranslate,
                    ResourceExtensions.GetColor("SecondaryText"))?.Glyph;

            ProceedButtonViewModel.SetCommand(NavigateForwardAsync);

            Disposables.Add(ProceedButtonViewModel.NavigateToPageCommand.IsExecuting
                .Subscribe(isExecuting => { IsLoading = isExecuting; }));

            SetProceedButtonIcon();
        }

        private void Initialize()
        {
            SetupRetellBackTranslation();

            SetupMiniWaveFormPlayer();

            SetupSequencer();
        }

        private void SetupRetellBackTranslation()
        {
            if (!IsTwoStepBackTranslate)
            {
                return;
            }

            if (_retellBackTranslation.RetellBackTranslationAudio == null)
            {
                // TODO: Make these real language IDs
                _retellBackTranslation.RetellBackTranslationAudio =
                    new RetellBackTranslation(
                        _retellBackTranslation.Id,
                        Guid.Empty,
                        _retellBackTranslation.ToLanguageId,
                        _retellBackTranslation.ProjectId,
                        _retellBackTranslation.ScopeId);
            }
        }

        private void SetupMiniWaveFormPlayer()
        {
            var requirePassageListen = Step.StepSettings.GetSetting(SettingType.RequireRetellBTPassageListen);
            var actionState = requirePassageListen ? ActionState.Required : ActionState.Optional;

            if (!IsTwoStepBackTranslate)
            {
                MiniWaveformPlayerViewModel = ViewModelContextProvider.GetMiniWaveformPlayerViewModel
                (
                    _passage.CurrentDraftAudio,
                    actionState,
                    _passageTitle
                );

                ActionViewModelBaseSourceList.Add(MiniWaveformPlayerViewModel);
            }
            else // Step.Role == Roles.BackTranslate2
            {
                BarPlayerViewModel = ViewModelContextProvider.GetBarPlayerViewModel(
                    _retellBackTranslation,
                    actionState,
                    _passageTitle, 0);

                ActionViewModelBaseSourceList.Add(BarPlayerViewModel);
            }
        }

        private void SetupSequencer()
        {
            //Recorder
            SequencerRecorderViewModel = ViewModelContextProvider.GetSequencerFactory().CreateRecorder(
                playerFactory: () => ViewModelContextProvider.GetAudioPlayer(),
                recorderFactory: () => ViewModelContextProvider.GetAudioRecorderFactory().Invoke(48000),
                FlagType.Note
            );

            SequencerRecorderViewModel.IsRightToLeftDirection = FlowDirection is FlowDirection.RightToLeft;

            SequencerRecorderViewModel.SetupActivityService(ViewModelContextProvider, Disposables);
            SequencerRecorderViewModel.SetupRecordPermissionPopup(ViewModelContextProvider, Logger);
            SequencerRecorderViewModel.SetupOnRecordFailedPopup(ViewModelContextProvider, Logger);

            SequencerRecorderViewModel.AddFlagCommand =
                ReactiveCommand.CreateFromTask<IFlag, bool>(ShowConversationAsync);
            SequencerRecorderViewModel.TapFlagCommand = ReactiveCommand.CreateFromTask(
                async (IFlag flag) => { await ShowConversationAsync(flag); });

            SequencerRecorderViewModel.OnRecordingStartedCommand = ReactiveCommand.Create(ResetBackTranslation);
            SequencerRecorderViewModel.OnRecordingFinishedCommand = ReactiveCommand.CreateFromTask(SaveBackTranslation);
            SequencerRecorderViewModel.OnDeleteRecordCommand = ReactiveCommand.CreateFromTask(DeleteSavedBackTranslation);
            SequencerRecorderViewModel.OnUndoDeleteRecordCommand = ReactiveCommand.CreateFromTask(RestoreDeletedBackTranslation);

            SequencerActionViewModel = SequencerRecorderViewModel
                .CreateActionViewModel(ViewModelContextProvider, Disposables, required: true);
            ActionViewModelBaseSourceList.Add(SequencerActionViewModel);

            //Player
            SetupAudioWithConversations();

            SequencerRecorderViewModel
                .WhenAnyValue(player => player.State)
                .Where(state => state == SequencerState.Recording)
                .Subscribe(_ => { SequencerActionViewModel.ActionState = ActionState.Required; });
        }

        private void SetupAudioWithConversations()
        {
            SetRetellBackTranslationAudio();
            SetupConversations();
        }

        private void SetRetellBackTranslationAudio()
        {
            if (IsTwoStepBackTranslate)
            {
                if (_retellBackTranslation.RetellBackTranslationAudio.Data.IsNullOrEmpty())
                {
                    return;
                }

                SequencerRecorderViewModel.SetRecord(
                    _retellBackTranslation.RetellBackTranslationAudio.CreateRecordAudioModel(
                        path: ViewModelContextProvider
                            .GetTempAudioService(_retellBackTranslation.RetellBackTranslationAudio).SaveTempAudio(),
                        flagType: FlagType.Note,
                        isTemp: _retellBackTranslation.RetellBackTranslationAudio.TemporaryDeleted));

                SequencerActionViewModel.ActionState =
                    _retellBackTranslation.RetellBackTranslationAudio.TemporaryDeleted
                        ? ActionState.Required
                        : ActionState.Optional;
            }
            else
            {
                if (_retellBackTranslation.Data.IsNullOrEmpty())
                {
                    return;
                }
                
                SequencerRecorderViewModel.SetRecord(_retellBackTranslation.CreateRecordAudioModel(
                    path: ViewModelContextProvider.GetTempAudioService(_retellBackTranslation).SaveTempAudio(),
                    flagType: FlagType.Note,
                    isTemp: _retellBackTranslation.TemporaryDeleted));

                SequencerActionViewModel.ActionState =
                    _retellBackTranslation.TemporaryDeleted
                        ? ActionState.Required
                        : ActionState.Optional;
            }
        }

        private void SetupConversations()
        {
            var conversations = IsTwoStepBackTranslate
                ? _retellBackTranslation.RetellBackTranslationAudio.Conversations
                : _retellBackTranslation.Conversations;
            
            SequencerNoteDetailViewModel?.ConversationMarkers.Clear();
            SequencerNoteDetailViewModel?.Dispose();
            
            //Note detail
            SequencerNoteDetailViewModel = new SequencerNoteDetailViewModel(
                conversations,
                Section,
                Stage,
                SequencerRecorderViewModel,
                ViewModelContextProvider,
                ActionViewModelBaseSourceList,
                Disposables);

            SequencerNoteDetailViewModel.SaveCommand = ReactiveCommand.CreateFromTask<Conversation>(SaveRetellWithNoteAsync);
            SequencerNoteDetailViewModel.DeleteMessageCommand = ReactiveCommand.CreateFromTask<Message>(DeleteMessageAsync);
        }

        // We have to check all but the current passage for audio, since the current passage will never have audio at this point
        protected sealed override void SetProceedButtonIcon()
        {
            if (!Step.StepSettings.GetSetting(SettingType.DoRetellBTPassageReview) &&
                !Step.StepSettings.GetSetting(SettingType.DoSegmentBackTranslate) &&
                (Step.Role != Roles.BackTranslate2 && Section.Passages
                     .Where(x => !x.PassageNumber.Equals(_passage.PassageNumber)).All(x =>
                         x.CurrentDraftAudio.RetellBackTranslationAudio != null) ||
                 Step.Role == Roles.BackTranslate2 && Section.Passages
                     .Where(x => !x.PassageNumber.Equals(_passage.PassageNumber)).All(x =>
                         x.CurrentDraftAudio.RetellBackTranslationAudio?.RetellBackTranslationAudio != null)))
            {
                ProceedButtonViewModel.IsCheckMarkIcon = true;
            }
        }

        private void ResetBackTranslation()
        {
            if (IsTwoStepBackTranslate)
            {
                if (_retellBackTranslation?.RetellBackTranslationAudio != null)
                {
                    _retellBackTranslation.RetellBackTranslationAudio.ClearConversations();
                    _retellBackTranslation.RetellBackTranslationAudio.TemporaryDeleted = false;
                }
            }
            else
            {
                if (_retellBackTranslation != null)
                {
                    _retellBackTranslation.ClearConversations();
                    _retellBackTranslation.TemporaryDeleted = false;
                }
            }
            
            SequencerNoteDetailViewModel?.ConversationMarkers.Clear();
        }

        private async Task SaveBackTranslation()
        {
            IsLoading = true;

            await Task.Run(async () =>
            {
                var record = SequencerRecorderViewModel.GetRecord();

                if (record == null)
                {
                    return;
                }

                var audioDetails = SequencerRecorderViewModel.AudioDetails;

                Stream audioStream;

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

                    if (IsTwoStepBackTranslate)
                    {
                        _retellBackTranslation.RetellBackTranslationAudio.SetAudio(audioData);
                        _retellBackTranslation.RetellBackTranslationAudio.SavedDuration =
                            SequencerRecorderViewModel.TotalDuration;

                        await _retellRepository.SaveAsync(_retellBackTranslation.RetellBackTranslationAudio);
                    }
                    else
                    {
                        _retellBackTranslation.SetAudio(audioData);
                        _retellBackTranslation.SavedDuration = SequencerRecorderViewModel.TotalDuration;

                        await _retellRepository.SaveAsync(_retellBackTranslation);
                    }

                }
            });
            
            SequencerActionViewModel.ActionState = ActionState.Optional;
            IsLoading = false;
        }

        private async Task DeleteSavedBackTranslation()
        {
            NotableAudio audio = IsTwoStepBackTranslate
                ? _retellBackTranslation.RetellBackTranslationAudio
                : _retellBackTranslation;
            
            if (!audio.HasAudio)
            {
                return;
            }
            
            audio.TemporaryDeleted = true;
            audio.SavedDuration = 0;
            
            await _retellRepository.SaveAsync(IsTwoStepBackTranslate
                ? _retellBackTranslation.RetellBackTranslationAudio
                : _retellBackTranslation);
            
            SequencerActionViewModel.ActionState = ActionState.Required;
        }

        private async Task RestoreDeletedBackTranslation()
        {
            if (IsTwoStepBackTranslate)
            {
                if (_retellBackTranslation.RetellBackTranslationAudio.Data.IsNullOrEmpty())
                {
                    return;
                }

                _retellBackTranslation.RetellBackTranslationAudio.TemporaryDeleted = false;
                _retellBackTranslation.RetellBackTranslationAudio.SavedDuration = SequencerRecorderViewModel.TotalDuration;

                await _retellRepository.SaveAsync(_retellBackTranslation.RetellBackTranslationAudio);
            }
            else
            {
                if (_retellBackTranslation.Data.IsNullOrEmpty())
                {
                    return;
                }
                
                _retellBackTranslation.TemporaryDeleted = false;
                _retellBackTranslation.SavedDuration = SequencerRecorderViewModel.TotalDuration;
                
                await _retellRepository.SaveAsync(_retellBackTranslation);
            }

            SetupAudioWithConversations();
        } 
        
        private async Task DeleteMessageAsync(Message message)
        {
            var conversations = IsTwoStepBackTranslate
                ? _retellBackTranslation.RetellBackTranslationAudio.Conversations
                : _retellBackTranslation.Conversations;
            
            foreach (var conversation in conversations)
            {
                var removed = conversation.Messages.Remove(message);
                if (removed)
                {
                    await SaveRetellWithNoteAsync(conversation);
                    break;
                }
            }
        }

        private async Task SaveRetellWithNoteAsync(Conversation conversationMarker)
        {
            if (IsTwoStepBackTranslate)
            {
                _retellBackTranslation.RetellBackTranslationAudio.UpdateOrDeleteConversation(conversationMarker);
                await _retellRepository.SaveAsync(_retellBackTranslation.RetellBackTranslationAudio);
            }
            else
            {
                _retellBackTranslation.UpdateOrDeleteConversation(conversationMarker);
                await _retellRepository.SaveAsync(_retellBackTranslation);
            }
        }

        private async Task<bool> ShowConversationAsync(IFlag flag)
        {
            return await SequencerNoteDetailViewModel.ShowConversationAsync(flag);
        }

        private async Task<IRoutableViewModel> NavigateForwardAsync()
        {
            try
            {
                if (Step.StepSettings.GetSetting(SettingType.DoRetellBTPassageReview))
                {
                    var vm = await RetellPassageReviewPageViewModel.CreateAsync(
                        ViewModelContextProvider, 
                        Step, 
                        Section,
                        _passage, 
                        _retellBackTranslation, 
                        _passageTitle, 
                        Stage);
                    
                    return await NavigateTo(vm);
                }

                if (Step.StepSettings.GetSetting(SettingType.DoSegmentBackTranslate))
                {
                    var vm= await SegmentBackTranslateResolver.GetSegmentSelectPageViewModel(
                        Section,
                        Step, 
                        _passage, 
                        ViewModelContextProvider);
                    
                    return await NavigateTo(vm);
                }

                if (Step.Role != Roles.BackTranslate2 && Section.Passages.All(x =>
                        x.CurrentDraftAudio.RetellBackTranslationAudio != null) ||
                    Step.Role == Roles.BackTranslate2 && Section.Passages.All(x =>
                        x.CurrentDraftAudio.RetellBackTranslationAudio?.RetellBackTranslationAudio != null))
                {
                    await ViewModelContextProvider.GetGrandCentralStation().AdvanceSectionAsync(Section, Step);
                    
                    return await NavigateToHomeOnMainStackAsync();
                }

                var selectPage = await RetellBackTranslateResolver.GetRetellPassageSelectViewModelAsync(
                    Section, Step, ViewModelContextProvider);
                   
                return await NavigateToAndReset(selectPage);
            }
            catch (Exception e)
            {
                LogError(e);
                throw;
            }
        }

        public override void Dispose()
        {
            SequencerRecorderViewModel?.Dispose();
            SequencerRecorderViewModel = null;

            SequencerNoteDetailViewModel?.Dispose();
            SequencerNoteDetailViewModel = null;

            SequencerActionViewModel?.Dispose();
            SequencerActionViewModel = null;

            MiniWaveformPlayerViewModel?.Dispose();
            BarPlayerViewModel?.Dispose();
            ProceedButtonViewModel?.Dispose();

            _retellBackTranslation = null;
            _passage = null;

            base.Dispose();
        }
    }
}