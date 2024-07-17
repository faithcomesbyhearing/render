using Render.Kernel;
using Render.Models.Sections;
using Render.Models.Workflow;
using Render.Models.Workflow.Stage;
using Render.Resources.Localization;
using System.Reactive.Linq;
using DynamicData;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Render.Components.BarPlayer;
using Render.Components.MiniWaveformPlayer;
using Render.Components.NoteDetail;
using Render.Extensions;
using Render.Models.Audio;
using Render.Pages.BackTranslator.RetellBackTranslate;
using Render.Repositories.Audio;
using Render.Repositories.Extensions;
using Render.Resources;
using Render.Sequencer.Contracts.Enums;
using Render.Sequencer.Contracts.Interfaces;
using Render.Pages.BackTranslator.SegmentBackTranslate.SegmentEditing;

namespace Render.Pages.BackTranslator.SegmentBackTranslate
{
    public class TabletSegmentTranslatePageViewModel : WorkflowPageBaseViewModel
    {
        private readonly IAudioRepository<SegmentBackTranslation> _segmentRepository;
        private SegmentBackTranslation _segmentBackTranslation;
        private Passage _passage;
        private readonly string _segmentName;

        private SequencerNoteDetailViewModel SequencerNoteDetailViewModel { get; set; }
        private ActionViewModelBase SequencerActionViewModel { get; set; }

        public IMiniWaveformPlayerViewModel MiniWaveformPlayerViewModel { get; set; }
        public IBarPlayerViewModel BarPlayerViewModel { get; set; }

        [Reactive] public ISequencerRecorderViewModel SequencerRecorderViewModel { get; private set; }
        [Reactive] public bool IsTwoStepBackTranslate { get; set; }

        public static async Task<TabletSegmentTranslatePageViewModel> CreateAsync(
            IViewModelContextProvider viewModelContextProvider,
            Step step,
            Section section,
            Passage passage,
            SegmentBackTranslation segmentBackTranslation,
            string segmentName,
            Stage stage)
        {
            var pageVm = new TabletSegmentTranslatePageViewModel(
                viewModelContextProvider: viewModelContextProvider,
                step: step,
                section: section,
                passage: passage,
                segmentBackTranslation: segmentBackTranslation,
                segmentName: segmentName,
                stage: stage);

            pageVm.Initialize();

            return pageVm;
        }

        private TabletSegmentTranslatePageViewModel(
            IViewModelContextProvider viewModelContextProvider,
            Step step,
            Section section,
            Passage passage,
            SegmentBackTranslation segmentBackTranslation,
            string segmentName,
            Stage stage) :
            base(
                urlPathSegment: "TabletSegmentTranslate",
                viewModelContextProvider: viewModelContextProvider,
                pageName: AppResources.BackTranslate,
                section: section,
                stage: stage,
                step: step,
                passageNumber: passage.PassageNumber,
                nonDraftTranslationId: segmentBackTranslation.Id,
                secondPageName: AppResources.SegmentBackTranslate)
        {
            _segmentBackTranslation = segmentBackTranslation;
            _segmentName = segmentName;
            _passage = passage;

            IsTwoStepBackTranslate = Step.Role == Roles.BackTranslate2;

            _segmentRepository = viewModelContextProvider.GetSegmentBackTranslationRepository();

            DisposeOnNavigationCleared = true;
            TitleBarViewModel.DisposeOnNavigationCleared = true;

            TitleBarViewModel.PageGlyph = IconExtensions
                .BuildFontImageSource(Icon.SegmentBackTranslate,
                    ResourceExtensions.GetColor("SecondaryText"))?.Glyph;

            ProceedButtonViewModel.SetCommand(NavigateForwardAsync);

            Disposables.Add(ProceedButtonViewModel.NavigateToPageCommand.IsExecuting
                .Subscribe(isExecuting => { IsLoading = isExecuting; }));

            SetProceedButtonIcon();
        }

        private void Initialize()
        {
            SetupSegmentBackTranslation();
            SetupMiniWaveFormPlayer();
            SetupSequencer();
        }

        private void SetupSegmentBackTranslation()
        {
            if (!IsTwoStepBackTranslate)
            {
                return;
            }

            /* On the second step, we dont further breathpausilize the segment,
            and are therefore creating a retell back translation of the segment
            so in this case we store the audio on the retell back translation field of the segment.*/

            if (_segmentBackTranslation.RetellBackTranslationAudio == null)
            {
                //TODO: Make the to language a real Id
                _segmentBackTranslation.RetellBackTranslationAudio =
                    new RetellBackTranslation(
                        _segmentBackTranslation.Id,
                        Guid.Empty,
                        _segmentBackTranslation.ToLanguageId,
                        _segmentBackTranslation.ProjectId,
                        _segmentBackTranslation.ScopeId);
            }
        }

        private void SetupMiniWaveFormPlayer()
        {
            var requirePassageListen = Step.StepSettings.GetSetting(SettingType.RequireSegmentBTPassageListen);
            var actionState = requirePassageListen ? ActionState.Required : ActionState.Optional;

            if (!IsTwoStepBackTranslate)
            {
                MiniWaveformPlayerViewModel = ViewModelContextProvider.GetMiniWaveformPlayerViewModel(
                    audio: _passage.CurrentDraftAudio,
                    actionState: actionState,
                    title: _segmentName,
                    timeMarkers: _segmentBackTranslation.TimeMarkers,
                    secondaryButtonIcon: (FontImageSource)ResourceExtensions.GetResourceValue("CombineIcon"),
                    secondaryButtonClickCommand: ReactiveCommand.CreateFromTask(NavigateToCombinePageAsync),
                    showSecondaryButton: true
                );

                Disposables.Add(MiniWaveformPlayerViewModel
                    .SecondaryButtonClickCommand
                    .IsExecuting
                    .Subscribe(isExecuting => { IsLoading = isExecuting; }));

                ActionViewModelBaseSourceList.Add(MiniWaveformPlayerViewModel);
            }
            else // Step.Role == Roles.BackTranslate2
            {
                BarPlayerViewModel = ViewModelContextProvider.GetBarPlayerViewModel(
                    _segmentBackTranslation,
                    actionState,
                    _segmentName, 0);

                ActionViewModelBaseSourceList.Add(BarPlayerViewModel);
            }
        }

        private void SetupSequencer()
        {
            //Recorder
            SequencerRecorderViewModel = ViewModelContextProvider
                .GetSequencerFactory()
                .CreateRecorder(
                    playerFactory: ViewModelContextProvider.GetAudioPlayer,
                    recorderFactory: () => ViewModelContextProvider.GetAudioRecorderFactory().Invoke(48000),
                    FlagType.Note);

            SequencerRecorderViewModel.IsRightToLeftDirection = FlowDirection is FlowDirection.RightToLeft;
            SequencerRecorderViewModel.AllowAppendRecordMode = true;

            SequencerRecorderViewModel.SetupActivityService(ViewModelContextProvider, Disposables);
            SequencerRecorderViewModel.SetupRecordPermissionPopup(ViewModelContextProvider, Logger);
            SequencerRecorderViewModel.SetupOnRecordFailedPopup(ViewModelContextProvider, Logger);

            SequencerRecorderViewModel.AddFlagCommand = ReactiveCommand.CreateFromTask<IFlag, bool>(ShowConversationAsync);
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
                if (_segmentBackTranslation.RetellBackTranslationAudio.Data.IsNullOrEmpty())
                {
                    return;
                }

                SequencerRecorderViewModel.SetRecord(_segmentBackTranslation.RetellBackTranslationAudio.CreateRecordAudioModel(
                    path: ViewModelContextProvider.GetTempAudioService(_segmentBackTranslation.RetellBackTranslationAudio).SaveTempAudio(),
                    flagType: FlagType.Note,
                    isTemp: _segmentBackTranslation.RetellBackTranslationAudio.TemporaryDeleted));

                SequencerActionViewModel.ActionState =
                    _segmentBackTranslation.RetellBackTranslationAudio.TemporaryDeleted
                    ? ActionState.Required
                    : ActionState.Optional;
            }
            else
            {
                if (_segmentBackTranslation.Data.IsNullOrEmpty())
                {
                    return;
                }

                SequencerRecorderViewModel.SetRecord(_segmentBackTranslation.CreateRecordAudioModel(
                    path: ViewModelContextProvider.GetTempAudioService(_segmentBackTranslation).SaveTempAudio(),
                    flagType: FlagType.Note,
                    isTemp: _segmentBackTranslation.TemporaryDeleted));

                SequencerActionViewModel.ActionState =
                    _segmentBackTranslation.TemporaryDeleted
                        ? ActionState.Required
                        : ActionState.Optional;
            }
        }

        private void SetupConversations()
        {
            var conversations = IsTwoStepBackTranslate
                ? _segmentBackTranslation.RetellBackTranslationAudio.Conversations
                : _segmentBackTranslation.Conversations;

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

        private async Task<bool> ShowConversationAsync(IFlag flag)
        {
            return await SequencerNoteDetailViewModel.ShowConversationAsync(flag);
        }

        private void ResetBackTranslation()
        {
            if (SequencerRecorderViewModel.AppendRecordMode)
            {
                return;
            }

            if (IsTwoStepBackTranslate)
            {
                if (_segmentBackTranslation?.RetellBackTranslationAudio != null)
                {
                    _segmentBackTranslation.RetellBackTranslationAudio.ClearConversations();
                    _segmentBackTranslation.RetellBackTranslationAudio.TemporaryDeleted = false;
                }
            }
            else
            {
                if (_segmentBackTranslation != null)
                {
                    _segmentBackTranslation.ClearConversations();
                    _segmentBackTranslation.TemporaryDeleted = false;
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
                        _segmentBackTranslation.RetellBackTranslationAudio.SetAudio(audioData);
                        _segmentBackTranslation.RetellBackTranslationAudio.SavedDuration =
                            SequencerRecorderViewModel.TotalDuration;

                        var retellRepository = ViewModelContextProvider.GetRetellBackTranslationRepository();
                        await retellRepository.SaveAsync(_segmentBackTranslation.RetellBackTranslationAudio);
                    }
                    else
                    {
                        _segmentBackTranslation.SetAudio(audioData);
                        _segmentBackTranslation.SavedDuration = SequencerRecorderViewModel.TotalDuration;

                        await _segmentRepository.SaveAsync(_segmentBackTranslation);
                    }

                }
            });

            SequencerActionViewModel.ActionState = ActionState.Optional;
            IsLoading = false;
        }

        private async Task DeleteSavedBackTranslation()
        {
            NotableAudio audio = IsTwoStepBackTranslate
                ? _segmentBackTranslation.RetellBackTranslationAudio
                : _segmentBackTranslation;

            if (!audio.HasAudio)
            {
                return;
            }

            audio.TemporaryDeleted = true;
            audio.SavedDuration = 0;

            if (IsTwoStepBackTranslate)
            {
                var retellRepository = ViewModelContextProvider.GetRetellBackTranslationRepository();
                await retellRepository.SaveAsync(_segmentBackTranslation.RetellBackTranslationAudio);
            }
            else
            {
                await _segmentRepository.SaveAsync(_segmentBackTranslation);
            }

            SequencerActionViewModel.ActionState = ActionState.Required;
        }

        private async Task RestoreDeletedBackTranslation()
        {
            if (IsTwoStepBackTranslate)
            {
                if (_segmentBackTranslation.RetellBackTranslationAudio.Data.IsNullOrEmpty())
                {
                    return;
                }

                _segmentBackTranslation.RetellBackTranslationAudio.TemporaryDeleted = false;
                _segmentBackTranslation.RetellBackTranslationAudio.SavedDuration = SequencerRecorderViewModel.TotalDuration;

                var retellRepository = ViewModelContextProvider.GetRetellBackTranslationRepository();
                await retellRepository.SaveAsync(_segmentBackTranslation.RetellBackTranslationAudio);
            }
            else
            {
                if (_segmentBackTranslation.Data.IsNullOrEmpty())
                {
                    return;
                }

                _segmentBackTranslation.TemporaryDeleted = false;
                _segmentBackTranslation.SavedDuration = SequencerRecorderViewModel.TotalDuration;

                await _segmentRepository.SaveAsync(_segmentBackTranslation);
            }

            SetupAudioWithConversations();
        }

        private async Task<IRoutableViewModel> NavigateForwardAsync()
        {
            try
            {
                if (Step.StepSettings.GetSetting(SettingType.DoSegmentBTPassageReview))
                {
                    var draftingViewModel = await SegmentReviewPageViewModel.CreateAsync(
                        ViewModelContextProvider, Step, Section, _passage, _segmentBackTranslation, _segmentName, Stage);

                    return await NavigateTo(draftingViewModel);
                }

                if (Step.Role != Roles.BackTranslate2 && Section.Passages.All(x =>
                        x.CurrentDraftAudio.SegmentBackTranslationAudios.Count > 0
                        && x.CurrentDraftAudio.SegmentBackTranslationAudios.All(s => s.HasAudio)) ||
                    Step.Role == Roles.BackTranslate2 && Section.Passages.All(x =>
                        x.CurrentDraftAudio.SegmentBackTranslationAudios.Count > 0
                        && x.CurrentDraftAudio.SegmentBackTranslationAudios.All(s =>
                            s.RetellBackTranslationAudio != null && s.RetellBackTranslationAudio.HasAudio)))
                {
                    await ViewModelContextProvider.GetGrandCentralStation().AdvanceSectionAsync(Section, Step);

                    return await NavigateToHomeOnMainStackAsync();
                }

                foreach (var segment in _passage.CurrentDraftAudio.SegmentBackTranslationAudios)
                {
                    if (Step.Role != Roles.BackTranslate2)
                    {
                        if (!segment.HasAudio)
                        {
                            var selectPageViewModel = await SegmentBackTranslateResolver
                                .GetSegmentSelectPageViewModel(Section, Step, _passage, ViewModelContextProvider);

                            return await NavigateToAndReset(selectPageViewModel);
                        }
                    }
                    else
                    {
                        if (segment.RetellBackTranslationAudio == null || !segment.RetellBackTranslationAudio.HasAudio)
                        {
                            var selectPageViewModel = await SegmentBackTranslateResolver
                                .GetSegmentSelectPageViewModel(Section, Step, _passage, ViewModelContextProvider);

                            return await NavigateToAndReset(selectPageViewModel);
                        }
                    }
                }

                Passage nextPassage;

                if (Step.Role == Roles.BackTranslate2)
                {
                    nextPassage = Section.Passages.First(x =>
                        x.CurrentDraftAudio.SegmentBackTranslationAudios.Any(s =>
                            s.RetellBackTranslationAudio == null) ||
                        x.CurrentDraftAudio.SegmentBackTranslationAudios.Any(s =>
                            s.RetellBackTranslationAudio != null &&
                            !s.RetellBackTranslationAudio.HasAudio) ||
                        x.CurrentDraftAudio.SegmentBackTranslationAudios.Count == 0);
                }
                else
                {
                    nextPassage = Section.Passages.First(x =>
                        x.CurrentDraftAudio.SegmentBackTranslationAudios.Any(s => !s.HasAudio) ||
                        x.CurrentDraftAudio.SegmentBackTranslationAudios.Count == 0);
                }

                if (Step.StepSettings.GetSetting(SettingType.DoRetellBackTranslate))
                {
                    if (nextPassage.CurrentDraftAudio.RetellBackTranslationAudio == null)
                    {
                        var newRetellBackTranslation = new RetellBackTranslation(nextPassage.CurrentDraftAudio.Id,
                            Guid.Empty, Guid.Empty, Section.ProjectId, Section.ScopeId);
                        await ViewModelContextProvider.GetRetellBackTranslationRepository()
                            .SaveAsync(newRetellBackTranslation);
                        nextPassage.CurrentDraftAudio.RetellBackTranslationAudio = newRetellBackTranslation;
                    }

                    var viewModel = await RetellBackTranslateResolver.GetRetellPassageSelectViewModelAsync(Section,
                        Step, ViewModelContextProvider);

                    return await NavigateToAndReset(viewModel);
                }

                var selectPage = await SegmentBackTranslateResolver.GetSegmentSelectPageViewModel(Section,
                    Step, nextPassage, ViewModelContextProvider);

                return await NavigateToAndReset(selectPage);
            }
            catch (Exception e)
            {
                LogError(e);
                throw;
            }
        }

        /* We have to check all but the current segment for audio, since the current
                  segment will never have audio at this point */
        protected sealed override void SetProceedButtonIcon()
        {
            if (!Step.StepSettings.GetSetting(SettingType.DoSegmentBTPassageReview) &&
                (Step.Role != Roles.BackTranslate2 && Section.Passages.All(x =>
                     x.CurrentDraftAudio.SegmentBackTranslationAudios.Count > 0
                     && x.CurrentDraftAudio.SegmentBackTranslationAudios
                         .Where(s => s != _segmentBackTranslation)
                         .All(s => s.HasAudio)) ||
                 Step.Role == Roles.BackTranslate2 && Section.Passages.All(x =>
                     x.CurrentDraftAudio.SegmentBackTranslationAudios.Count > 0
                     && x.CurrentDraftAudio.SegmentBackTranslationAudios
                         .Where(s => s != _segmentBackTranslation)
                         .All(s =>
                             s.RetellBackTranslationAudio != null && s.RetellBackTranslationAudio.HasAudio))))
            {
                ProceedButtonViewModel.IsCheckMarkIcon = true;
            }
        }

        private async Task SaveRetellWithNoteAsync(Conversation conversationMarker)
        {
            if (IsTwoStepBackTranslate)
            {
                _segmentBackTranslation.RetellBackTranslationAudio.UpdateOrDeleteConversation(conversationMarker);

                var retellRepository = ViewModelContextProvider.GetRetellBackTranslationRepository();
                await retellRepository.SaveAsync(_segmentBackTranslation.RetellBackTranslationAudio);
            }
            else
            {
                _segmentBackTranslation.UpdateOrDeleteConversation(conversationMarker);
                await _segmentRepository.SaveAsync(_segmentBackTranslation);
            }
        }

        private async Task DeleteMessageAsync(Message message)
        {
            var conversations = IsTwoStepBackTranslate
                ? _segmentBackTranslation.RetellBackTranslationAudio.Conversations
                : _segmentBackTranslation.Conversations;

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


        private async Task<IRoutableViewModel> NavigateToCombinePageAsync()
        {
            await SequencerRecorderViewModel.StopCommand.Execute();

            var vm = await TabletSegmentCombinePageViewModel.CreateAsync(
                step: Step,
                section: Section,
                passage: _passage,
                selectedSegmentBackTranslation: _segmentBackTranslation,
                viewModelContextProvider: ViewModelContextProvider,
                stage: Stage);

            return await NavigateTo(vm);
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

            _passage = null;
            _segmentBackTranslation = null;
            _segmentRepository?.Dispose();

            base.Dispose();
        }
    }
}