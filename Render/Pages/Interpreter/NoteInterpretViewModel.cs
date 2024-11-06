using System.Reactive.Linq;
using DynamicData;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Render.Components.BarPlayer;
using Render.Extensions;
using Render.Kernel;
using Render.Models.Audio;
using Render.Models.Sections;
using Render.Models.Workflow;
using Render.Models.Workflow.Stage;
using Render.Repositories.Audio;
using Render.Resources;
using Render.Resources.Localization;
using Render.Sequencer.Contracts.Enums;
using Render.Sequencer.Contracts.Interfaces;
using Render.Sequencer.Contracts.Models;
using Render.Sequencer.Contracts.ToolbarItems;

namespace Render.Pages.Interpreter
{
    public class NoteInterpretViewModel : WorkflowPageBaseViewModel
    {
        private readonly IAudioRepository<Audio> _audioRepository;

        private readonly Dictionary<Draft, List<Message>> _messageDictionary;

        public readonly IBarPlayerViewModel OriginalNotePlayer;

        [Reactive] public ISequencerRecorderViewModel SequencerRecorderViewModel { get; private set; }
        private ActionViewModelBase SequencerActionViewModel { get; set; }
        private IToolbarItem DeleteDraftToolBarItem { get; set; }

        private Draft Draft { get; set; }
        private Audio Audio { get; set; }
        private Message Message { get; set; }

        public static NoteInterpretViewModel Create(
            Step step,
            Section section,
            Draft draft,
            Message message,
            Stage stage,
            Dictionary<Draft, List<Message>> messageDictionary,
            IViewModelContextProvider viewModelContextProvider)
        {
            var pageVm = new NoteInterpretViewModel(
                step,
                section,
                draft,
                message,
                stage,
                messageDictionary,
                viewModelContextProvider);

            pageVm.SetupSequencer();
            return pageVm;
        }

        public static (Draft draft, Message message) FindNextMessageNeedingInterpretation(
            Dictionary<Draft, List<Message>> messageDictionary)
        {
            if (messageDictionary.Any())
            {
                var pair = messageDictionary.First();
                var message = pair.Value.First();
                var draft = pair.Key;
                pair.Value.Remove(message);

                if (!pair.Value.Any())
                {
                    messageDictionary.Remove(pair.Key);
                }

                return (draft, message);
            }

            return default;
        }

        // This method is used only for setting the proceed button so that it is readonly
        public static (Draft draft, Message message) FindNextMessageWithoutInterpretation(
            Dictionary<Draft, List<Message>> messageDictionary)
        {
            if (messageDictionary.Any())
            {
                var pair = messageDictionary.First();
                var message = pair.Value.First();
                var draft = pair.Key;
                return (draft, message);
            }

            return default;
        }

        private NoteInterpretViewModel(
            Step step,
            Section section,
            Draft draft,
            Message message,
            Stage stage,
            Dictionary<Draft, List<Message>> messageDictionary,
            IViewModelContextProvider viewModelContextProvider)
            : base(
                urlPathSegment: "NoteInterpret",
                viewModelContextProvider: viewModelContextProvider,
                pageName: GetStepName(step),
                section: section,
                stage: stage,
                step: step,
                nonDraftTranslationId: message.Id,
                secondPageName: AppResources.NoteInterpretScreenTitle)
        {
            var color = ResourceExtensions.GetColor("SecondaryText");
            if (color != null)
            {
                TitleBarViewModel.PageGlyph = IconExtensions.BuildFontImageSource(Icon.NoteTranslate, color, 40)?.Glyph;
            }

            Draft = draft;
            Message = message;
            Audio = message.InterpretationAudio ?? new Audio(section.ScopeId, section.ProjectId, message.Id);
            _audioRepository = viewModelContextProvider.GetAudioRepository();
            _messageDictionary = messageDictionary;
            var requireNoteListen = step.StepSettings.GetSetting(SettingType.RequireNoteListen);
            //TODO add audio title to model
            OriginalNotePlayer = new BarPlayerViewModel(message.Media, viewModelContextProvider,
                requireNoteListen ? ActionState.Required : ActionState.Optional, AppResources.Note);

            ActionViewModelBaseSourceList.Add(OriginalNotePlayer);
            ProceedButtonViewModel.SetCommand(NavigateForwardAsync);
            TitleBarViewModel.NavigateBackCommand = ReactiveCommand.CreateFromTask(NavigateBack);
            Disposables.Add(ProceedButtonViewModel.NavigateToPageCommand.IsExecuting
                .Subscribe(isExecuting => { IsLoading = isExecuting; }));
            SetProceedButtonIcon();
        }

        private void SetupSequencer()
        {
            SequencerRecorderViewModel = ViewModelContextProvider.GetSequencerFactory().CreateRecorder(
                playerFactory: ViewModelContextProvider.GetAudioPlayer,
                recorderFactory: () => ViewModelContextProvider.GetAudioRecorderFactory().Invoke(48000));

            SequencerRecorderViewModel.IsRightToLeftDirection = FlowDirection is FlowDirection.RightToLeft;
            SequencerRecorderViewModel.AllowAppendRecordMode = true;
            SequencerRecorderViewModel.SetupActivityService(ViewModelContextProvider, Disposables);
            SequencerRecorderViewModel.SetupRecordPermissionPopup(ViewModelContextProvider, Logger);
            SequencerRecorderViewModel.SetupOnRecordFailedPopup(ViewModelContextProvider, Logger);

            DeleteDraftToolBarItem = SequencerRecorderViewModel.GetToolbarItem<IDeleteToolbarItem>();

            SequencerRecorderViewModel.OnDeleteRecordCommand = ReactiveCommand.Create(DeleteInterpretation);
            SequencerRecorderViewModel.OnRecordingFinishedCommand = ReactiveCommand.CreateFromTask(SaveInterpretation);
            SequencerRecorderViewModel.OnUndoDeleteRecordCommand =
                ReactiveCommand.Create(() => { SequencerActionViewModel.ActionState = ActionState.Optional; });

            SequencerActionViewModel =
                SequencerRecorderViewModel.CreateActionViewModel(ViewModelContextProvider, Disposables, required: true);
            ActionViewModelBaseSourceList.Add(SequencerActionViewModel);

            if (Audio.Data.Length is not 0)
            {
                SequencerRecorderViewModel.SetRecord(RecordAudioModel.Create(
                    path: ViewModelContextProvider.GetTempAudioService(Audio).SaveTempAudio(),
                    name: string.Empty));
                SequencerActionViewModel.ActionState = ActionState.Optional;
            }

            SequencerRecorderViewModel
               .WhenAnyValue(player => player.State)
               .Where(state => state == SequencerState.Recording)
               .Subscribe(_ => { SequencerActionViewModel.ActionState = ActionState.Required; });
        }

        protected override async Task NavigatingAwayAsync()
        {
            if (SequencerRecorderViewModel.State is not SequencerState.Recording)
            {
                return;
            }

            await SequencerRecorderViewModel.StopCommand.Execute();
        }

        protected sealed override void SetProceedButtonIcon()
        {
            var tuple = FindNextMessageWithoutInterpretation(_messageDictionary);
            if (!Step.StepSettings.GetSetting(SettingType.DoNoteReview) &&
                tuple == default)
            {
                ProceedButtonViewModel.IsCheckMarkIcon = true;
            }
        }

        private async Task SaveInterpretation()
        {
            IsLoading = true;

            await Task.Run(async () =>
            {
                var loggedInUserId = GetLoggedInUserId();

                if (SequencerRecorderViewModel?.GetRecord() != null)
                {
                    //SequencerRecorderViewModel.AppendAudio();
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

                        Audio.SetAudio(audioData);
                        await _audioRepository.SaveAsync(Audio);
                        Message.InterpretationAudio = Audio;

                        SequencerActionViewModel.ActionState = ActionState.Optional;
                    }
                }

                Message.SetInterpreterUserId(loggedInUserId); //set interpret user id for interpret note
            });

            IsLoading = false;
        }

        private void DeleteInterpretation()
        {
            Task.Run(async () =>
            {
                await _audioRepository.SaveAsync(Audio);
                Message.InterpretationAudio = null;
                Message.SetInterpreterUserId(Guid.Empty);
                SequencerActionViewModel.ActionState = ActionState.Required;
            });
        }

        private async Task<IRoutableViewModel> NavigateForwardAsync()
        {
            if (Step.StepSettings.GetSetting(SettingType.DoNoteReview))
            {
                if (Message.InterpretationAudio == null)
                {
                    Message.InterpretationAudio = Audio;

                    Message.SetInterpreterUserId(GetLoggedInUserId());
                }

                var noteReviewViewModel = await Task.Run(() => Task.FromResult(NoteReviewViewModel.Create(Step, Section,
                    Draft, Message, Stage,
                    _messageDictionary, ViewModelContextProvider)));
                return await NavigateTo(noteReviewViewModel);
            }

            await Task.Run(async () =>
            {
                await NoteReviewViewModel.SaveDraftAsync(Draft, Message, ViewModelContextProvider);
            });

            //Find next note to interpret
            var tuple = FindNextMessageNeedingInterpretation(_messageDictionary);
            if (tuple != default)
            {
                var viewModel = await Task.Run(() => Task.FromResult(Create(Step, Section, tuple.draft, tuple.message,
                    Stage,
                    _messageDictionary, ViewModelContextProvider)));
                return await NavigateTo(viewModel);
            }

            await Task.Run(async () =>
            {
                var sectionMovementService = ViewModelContextProvider.GetSectionMovementService();
                await sectionMovementService.AdvanceSectionAsync(Section, Step, GetProjectId(), GetLoggedInUserId()); 
            });

            return await NavigateToHomeOnMainStackAsync();
        }

        private new async Task<IRoutableViewModel> NavigateBack()
        {
            try
            {
                return await FinishCurrentStackAndNavigateHome();
            }
            catch (Exception e)
            {
                LogError(e);
                throw;
            }
        }

        public override void Dispose()
        {
            Audio = null;
            Draft = null;
            Message = null;

            OriginalNotePlayer?.Dispose();

            SequencerRecorderViewModel?.Dispose();
            SequencerRecorderViewModel = null;
            SequencerActionViewModel?.Dispose();
            SequencerActionViewModel = null;

            _audioRepository?.Dispose();

            base.Dispose();
        }
    }
}