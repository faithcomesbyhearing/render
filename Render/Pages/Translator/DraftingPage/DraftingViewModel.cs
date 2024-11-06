using System.Reactive.Linq;
using System.Collections.ObjectModel;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Render.Components.BarPlayer;
using Render.Components.Modal;
using Render.Extensions;
using Render.Kernel;
using Render.Kernel.WrappersAndExtensions;
using Render.Models.Audio;
using Render.Models.Sections;
using Render.Models.Workflow;
using Render.Models.Workflow.Stage;
using Render.Pages.Translator.AudioEdit;
using Render.Pages.Translator.DraftSelectPage;
using Render.Repositories.Audio;
using Render.Repositories.SnapshotRepository;
using Render.Resources;
using Render.Resources.Localization;
using Render.Resources.Styles;
using Render.Sequencer.Contracts.Enums;
using Render.Sequencer.Contracts.Interfaces;
using Render.Sequencer.Contracts.Models;
using Render.Sequencer.Contracts.ToolbarItems;
using Render.Services.AudioServices;

namespace Render.Pages.Translator.DraftingPage
{
    public class DraftingViewModel : WorkflowPageBaseViewModel
    {
        private readonly IAudioEncodingService _audioEncodingService;
        private readonly IAudioRepository<Audio> _temporaryAudioRepository;
        private readonly ReadOnlyObservableCollection<IBarPlayerViewModel> _barPlayerViewModels;
        private readonly ReadOnlyObservableCollection<DraftViewModel> _draftViewModels;
        private readonly ISnapshotRepository _snapshotRepository;
        private const int ExpectedDraftCount = 6;

        private Passage _passage;
        private int _draftNumber = 1;

        private readonly SourceList<IBarPlayerViewModel> _sectionReferenceAudioSources = new();
        private readonly SourceCache<DraftViewModel, Guid> _audioDraftSourceList = new(x => x.Audio.Id);

        public ReadOnlyObservableCollection<IBarPlayerViewModel> BarPlayerViewModels => _barPlayerViewModels;
        public ReadOnlyObservableCollection<DraftViewModel> DraftViewModels => _draftViewModels;

        [Reactive] public ISequencerRecorderViewModel SequencerRecorderViewModel { get; private set; }

        private ActionViewModelBase SequencerActionViewModel { get; set; }

        private IToolbarItem EditPassageToolbarItem { get; set; }
        private IToolbarItem DeleteDraftToolBarItem { get; set; }
        private ConversationService _conversationService;
        private Dictionary<Guid, NotableAudio> _sequencerRecorderAudioList = new();

        public static async Task<DraftingViewModel> CreateAsync(
            Section section,
            Passage passage,
            Step step,
            IViewModelContextProvider viewModelContextProvider,
            Stage stage,
            bool needToLoadSessionStageDrafts = true)
        {
            var vm = new DraftingViewModel(section, passage, step, viewModelContextProvider, stage);

            if (!needToLoadSessionStageDrafts)
            {
                var preserveAudioIds = vm._audioDraftSourceList.Items.Where(x => x.HasAudio).Select(x => x.Audio.Id);
                await vm.SessionStateService.RemoveDraftAudios(vm.SessionStateService.AudioIds.Except(preserveAudioIds));
                return vm;
            }

            if (vm.SessionStateService.AudioIds is { Count: > 0 })
            {
                vm.LoadSessionStateDrafts();
            }

            return vm;
        }

        private void LoadSessionStateDrafts()
        {
            var activeSession = SessionStateService.ActiveSession;
            if (activeSession.SectionId != Section.Id || !activeSession.PassageNumber.Equals(_passage.PassageNumber))
            {
                return;
            }

            Task.Run(async () =>
            {
                foreach (var draftId in SessionStateService.AudioIds)
                {
                    if (_audioDraftSourceList.Items.Any(d => d.Audio.Id == draftId))
                    {
                        continue;
                    }

                    //If audio is temporarilyDeleted then HasAudio is set to false therefore, we need to use Audio.Data.Length to determine if we have audio or not.
                    var draftVm = _audioDraftSourceList.Items.FirstOrDefault(x => x.Audio.Data.Length == 0);
                    if (draftVm == null)
                    {
                        return;
                    }

                    var audio = await _temporaryAudioRepository.GetByIdAsync(draftId);
                    if (audio?.ParentId != _passage.Id)
                    {
                        continue;
                    }

                    _audioDraftSourceList.RemoveKey(draftVm.Audio.Id);
                    _sequencerRecorderAudioList.Remove(draftVm.Audio.Id);

                    draftVm.SetAudio(audio);
                    _sequencerRecorderAudioList.Add(audio.Id, new NotableAudio(audio.ScopeId, audio.ProjectId, audio.ParentId));

                    if (activeSession.CurrentDraftAudioId != Guid.Empty && activeSession.CurrentDraftAudioId != draftId)
                    {
                        draftVm.Deselect();
                    }
                    else if (activeSession.CurrentDraftAudioId == Guid.Empty && SessionStateService.AudioIds.FirstOrDefault() != draftId)
                    {
                        draftVm.Deselect();
                    }
                    else
                    {
                        draftVm.Select();
                    }

                    _audioDraftSourceList.AddOrUpdate(draftVm);
                }
            });
        }

        private DraftingViewModel(
            Section section,
            Passage currentPassage,
            Step step,
            IViewModelContextProvider viewModelContextProvider,
            Stage stage)
            : base(
                urlPathSegment: "Drafting",
                viewModelContextProvider: viewModelContextProvider,
                pageName: GetStepName(step),
                section: section,
                stage: stage,
                step: step,
                passageNumber: currentPassage.PassageNumber,
                secondPageName: AppResources.DraftRecord)
        {
            _passage = currentPassage;

            _audioEncodingService = viewModelContextProvider.GetAudioEncodingService();
            _temporaryAudioRepository = viewModelContextProvider.GetTemporaryAudioRepository();
            _snapshotRepository = viewModelContextProvider.GetSnapshotRepository();


            var draftChangeList = _audioDraftSourceList
                .Connect()
                .Publish();

            Disposables.Add(draftChangeList
                .WhenPropertyChanged(x => x.Selected)
                .Select(c => c.Sender)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(async vm =>
                {
                    if (vm.Selected)
                    {
                        await OnDraftSelected(vm);
                    }
                }));

            Disposables.Add(draftChangeList.Sort(SortExpressionComparer<DraftViewModel>.Ascending(vm => vm.Number))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Bind(out _draftViewModels)
                .Subscribe());

            Disposables.Add(draftChangeList.Connect());

            SetupSequencer();

            var canSelectDraft = SequencerRecorderViewModel
                .WhenAnyValue(vm => vm.State)
                .Select(state => state != SequencerState.Recording);

            var hasPreviousDraft = false;
            if (currentPassage.CurrentDraftAudio?.Data?.Any() is true)
            {
                var audioFromPassage = new NotableAudio(section.ScopeId, section.ProjectId, currentPassage.Id);
                audioFromPassage.SetAudio(currentPassage.CurrentDraftAudio.Data);

                audioFromPassage.Conversations =
                    currentPassage.CurrentDraftAudio.Conversations
                        .Union(ConversationService.GetAdditionalNotes(currentPassage.CurrentDraftAudio)).ToList();

                _sequencerRecorderAudioList.Add(currentPassage.CurrentDraftAudio.Id, audioFromPassage);
                // remove the old previous audio id from the audio id list in active session
                if (SessionStateService.AudioIds.Contains(SessionStateService.ActiveSession.PreviousDraftId))
                {
                    SessionStateService.AudioIds.Remove(SessionStateService.ActiveSession.PreviousDraftId);
                }

                var draft = CreateDraftViewModel(viewModelContextProvider, audioFromPassage, canSelectDraft, true, true);
                _audioDraftSourceList.AddOrUpdate(draft);
                _draftNumber++;
                // set the new audio id to the previous draft id in active session
                SessionStateService.ActiveSession.PreviousDraftId = audioFromPassage.Id;
                hasPreviousDraft = true;
            }

            Disposables.Add(draftChangeList
                .ObserveOn(RxApp.MainThreadScheduler)
                .WhenPropertyChanged(x => x.DraftState)
                .Subscribe(_ =>
                {
                    var actionState = DraftViewModels.Any(x => x.HasAudio)
                        ? ActionState.Optional
                        : ActionState.Required;

                    if (SequencerActionViewModel is null)
                    {
                        return;
                    }

                    SequencerActionViewModel.ActionState = actionState;
                }));


            ProceedButtonViewModel.SetCommand(NavigateToDraftSelectAsync);
            Disposables.Add(ProceedButtonViewModel.NavigateToPageCommand.IsExecuting
                .Subscribe(isExecuting =>
                {
                    IsLoading = isExecuting;
                }));

            var color = (ColorReference)ResourceExtensions.GetResourceValue("SecondaryText");
            if (color != null)
            {
                TitleBarViewModel.PageGlyph = ResourceExtensions.GetResourceValue(Icon.Record.ToString()) as string;
            }

            //Reference bar players             
            var referenceChangeList = _sectionReferenceAudioSources.Connect().Publish();
            Disposables.Add(referenceChangeList
                .Bind(out _barPlayerViewModels)
                .Subscribe());

            Disposables.Add(referenceChangeList.Connect());

            var referenceCount = 0;
            var passageNumber = _passage.PassageNumber.Number;
            _sectionReferenceAudioSources.Edit(sectionReferenceAudioSources =>
            {
                foreach (var sectionReferenceAudio in section.References.Where(x => !x.LockedReferenceByPassageNumbersList.Contains(passageNumber)))
                {
                    var passageReference = sectionReferenceAudio.PassageReferences.SingleOrDefault(x =>
                        x.PassageNumber.Equals(currentPassage.PassageNumber));
                    if (passageReference != null)
                    {
                        var vm = ViewModelContextProvider.GetBarPlayerViewModel(sectionReferenceAudio,
                            ActionState.Optional,
                            sectionReferenceAudio.Reference.Name, referenceCount++, passageReference.TimeMarkers);
                        sectionReferenceAudioSources.Add(vm);
                    }
                }
            });

            //populate empty drafts
            var selectDraft = SessionStateService.ActiveSession.CurrentDraftAudioId == Guid.Empty && !hasPreviousDraft;

            _audioDraftSourceList.Edit(audioDraftSourceList =>
            {
                while (_draftNumber <= ExpectedDraftCount)
                {
                    var notableAudio = new NotableAudio(section.ScopeId, section.ProjectId, currentPassage.Id);
                    _sequencerRecorderAudioList.Add(notableAudio.Id, notableAudio);
                    var draft = CreateDraftViewModel(viewModelContextProvider, notableAudio, canSelectDraft, selectDraft);
                    //Only the first one should be selected
                    selectDraft = false;
                    audioDraftSourceList.AddOrUpdate(draft);
                    _draftNumber++;
                }
            });
        }

        private void SetupSequencer()
        {
            var flagType = Stage.StageType switch
            {
                StageTypes.CommunityTest => FlagType.None,
                _ => FlagType.Note
            };

            SequencerRecorderViewModel = ViewModelContextProvider.GetSequencerFactory().CreateRecorder(
                playerFactory: ViewModelContextProvider.GetAudioPlayer,
                recorderFactory: () => ViewModelContextProvider.GetAudioRecorderFactory().Invoke(48000),
                flagType: flagType);

            SequencerRecorderViewModel.IsRightToLeftDirection = FlowDirection is FlowDirection.RightToLeft;

            var flagItem = SequencerRecorderViewModel.GetToolbarItem<IFlagToolbarItem>();
            if (flagItem != null)
            {
                SequencerRecorderViewModel.RemoveToolbarItem(flagItem);
            }

            SequencerRecorderViewModel.SetupActivityService(ViewModelContextProvider, Disposables);
            SequencerRecorderViewModel.SetupRecordPermissionPopup(ViewModelContextProvider, Logger);
            SequencerRecorderViewModel.SetupOnRecordFailedPopup(ViewModelContextProvider, Logger);

            DeleteDraftToolBarItem = SequencerRecorderViewModel.GetToolbarItem<IDeleteToolbarItem>();

            if (Step.StepSettings.GetSetting(SettingType.AllowEditing))
            {
                EditPassageToolbarItem = SequencerRecorderViewModel.AddToolbarItem(
                    item: new ToolbarItemModel(ToolbarItemType.Custom,
                        icon: "Edit",
                        actionCommand: ReactiveCommand.CreateFromTask(NavigateToAudioEditPageAsync),
                        automationId: "EditButton"), 0);
            }

            SequencerRecorderViewModel.OnRecordingStartedCommand = ReactiveCommand.Create(OnDraftRecordingStarted);
            SequencerRecorderViewModel.OnRecordingFinishedCommand = ReactiveCommand.CreateFromTask(OnDraftRecordingFinished);
            SequencerRecorderViewModel.OnDeleteRecordCommand = ReactiveCommand.CreateFromTask(OnDraftDeleted);
            SequencerRecorderViewModel.OnUndoDeleteRecordCommand = ReactiveCommand.CreateFromTask(OnUndoDraftDeletion);

            SequencerActionViewModel = SequencerRecorderViewModel.CreateActionViewModel(ViewModelContextProvider, Disposables);
            ActionViewModelBaseSourceList.Add(SequencerActionViewModel);
        }

        private async Task SetupConversationService(Guid sectionId)
        {
            var snapshots = await _snapshotRepository.GetSnapshotsForSectionAsync(sectionId);

            _conversationService = new ConversationService(
                this,
                Disposables,
                Stage,
                Step,
                SequencerRecorderViewModel,
                appendNotesForChildAudios: Step.RenderStepType == RenderStepTypes.ConsultantRevise);

            _conversationService.TapFlagPostEvent = ProcessStateStatusChange;
            _conversationService.DefineFlagsToDraw(snapshots, Stage.Id);
        }

        private DraftViewModel CreateDraftViewModel(
            IViewModelContextProvider viewModelContextProvider,
            Audio audio,
            IObservable<bool> canSelectDraft,
            bool selectDraft,
            bool isPreviousDraft = false)
        {
            var draft = new DraftViewModel(_draftNumber, viewModelContextProvider, canSelectDraft, isPreviousDraft);
            draft.SetAudio(audio);

            if (selectDraft)
            {
                draft.Select();
            }

            return draft;
        }

        protected override async Task NavigatingAwayAsync()
        {
            if (SequencerRecorderViewModel.State is not SequencerState.Recording)
            {
                return;
            }

            await SequencerRecorderViewModel.StopCommand.Execute();
        }

        private void OnDraftRecordingStarted()
        {
            SequencerActionViewModel.ActionState = ActionState.Required;
        }

        private async Task OnDraftRecordingFinished()
        {
            IsLoading = true;

            await Task.Run(async () =>
            {
                var draft = DraftViewModels.First(vm => vm.Selected);
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

                var audioData = _audioEncodingService.ConvertWavToOpus(
                    wavStream: audioStream,
                    sampleRate: audioDetails.SampleRate,
                    channelCount: audioDetails.ChannelCount);

                await audioStream.DisposeAsync();

                var audio = draft.Audio ?? new Audio(Guid.Empty, Guid.Empty, Guid.Empty);
                audio.TemporaryDeleted = false;
                audio.SetAudio(audioData);
                audio.SavedDuration = SequencerRecorderViewModel.TotalDuration;
                audio.PreviewSamples = ViewModelContextProvider
                    .GetWaveFormService()
                    .InterpolateBars(record.Samples);

                if (draft.Audio is null)
                {
                    draft.SetAudio(audio, record.Path);
                }

                await _temporaryAudioRepository.SaveAsync(audio);
                SessionStateService.AddDraftAudio(audio);
            });

            SequencerActionViewModel.ActionState = ActionState.Optional;
            IsLoading = false;
        }

        private async Task OnDraftSelected(DraftViewModel vm)
        {
            var drafts = DraftViewModels.ToList();

            foreach (var draft in drafts)
            {
                if (draft.Title != vm.Title)
                {
                    draft.Deselect();
                }
            }

            if (vm.Audio?.Data?.Length > 0)
            {
                await SetupConversationService(Section.Id);

                SequencerRecorderViewModel.SetRecord(vm.Audio.CreateRecordAudioModel(
                    _conversationService.GetConversations(vm.Audio),
                    path: vm.GetAudioPath(),
                    isTemp: vm.Audio.TemporaryDeleted,
                    canDelete: vm.IsPreviousDraft is false,
                    flagType: FlagType.Note,
                    userId: ViewModelContextProvider.GetLoggedInUser().Id));

                _conversationService.SequencerAudios = new List<Passage>() { _passage }.Select(p => p.CurrentDraftAudio);
                _conversationService.SequencerRecords = _sequencerRecorderAudioList;
                _conversationService.InitializeNoteDetailForRecord(default);
            }
            else
            {
                SequencerRecorderViewModel.SetRecord(RecordAudioModel.Empty());
            }

            if (EditPassageToolbarItem is not null)
            {
                EditPassageToolbarItem.IsAvailable = vm.IsPreviousDraft;
            }
        }

        private async Task OnDraftDeleted()
        {
            var selectedDraft = _draftViewModels.SingleOrDefault(x => x.Selected);
            selectedDraft.Audio.TemporaryDeleted = true;
            selectedDraft.IsPreviousDraft = false;

            if (EditPassageToolbarItem is not null)
            {
                EditPassageToolbarItem.IsAvailable = false;
            }

            var recentAudio = DraftViewModels.LastOrDefault(x => x.Audio.HasAudio && SessionStateService.AudioIds.Contains(x.Audio.Id));
            SessionStateService.SetCurrentDraftId(recentAudio != null ? recentAudio.Audio.Id : Guid.Empty);

            if (selectedDraft?.Audio == null || selectedDraft.Audio is Models.Sections.Draft)
            {
                return;
            }

            selectedDraft.TriggerUpdate();
            await _temporaryAudioRepository.SaveAsync(selectedDraft.Audio);
        }

        private async Task OnUndoDraftDeletion()
        {
            var selectedDraft = _draftViewModels.SingleOrDefault(x => x.Selected);
            selectedDraft.Audio.TemporaryDeleted = false;

            var recentAudio = DraftViewModels.LastOrDefault(x => x.Audio.HasAudio && SessionStateService.AudioIds.Contains(x.Audio.Id));
            SessionStateService.SetCurrentDraftId(recentAudio != null ? recentAudio.Audio.Id : Guid.Empty);

            if (selectedDraft?.Audio == null || selectedDraft.Audio is Models.Sections.Draft)
            {
                return;
            }

            selectedDraft.TriggerUpdate();
            await _temporaryAudioRepository.SaveAsync(selectedDraft.Audio);
        }

        private async Task<IRoutableViewModel> NavigateToDraftSelectAsync()
        {
            var drafts = new List<DraftViewModel>(DraftViewModels.Where(x => x.HasAudio));
            var draftSelectViewModel =
                await Task.Run(() => Task.FromResult(new DraftSelectViewModel(Section, drafts, ViewModelContextProvider,
                    _passage, Step, Stage)));
            var draftAudioIds = drafts.Select(x => x.Audio.Id).ToList();
            var audioIdsToRemove = new List<Guid>();
            foreach (var audioId in SessionStateService.AudioIds)
            {
                if (!draftAudioIds.Contains(audioId))
                {
                    audioIdsToRemove.Add(audioId);
                }
            }

            foreach (var draft in drafts)
            {
                if (!SessionStateService.AudioIds.Contains(draft.Audio.Id))
                {
                    draft.Audio.SavedDuration = SequencerRecorderViewModel.TotalDuration;
                    await _temporaryAudioRepository.SaveAsync(draft.Audio);
                    SessionStateService.AddDraftAudio(draft.Audio);
                }
            }

            await SessionStateService.RemoveDraftAudios(audioIdsToRemove);
            return await NavigateTo(draftSelectViewModel);
        }

        private async Task<IRoutableViewModel> NavigateToAudioEditPageAsync()
        {
            try
            {
                // To avoid animation glitches it's better to run view model creation and initialization
                // in separate thread 
                var vm = await Task.Run(() => AudioEditingPageViewModel.Create(
                    viewModelContextProvider: ViewModelContextProvider,
                    section: Section,
                    passage: _passage,
                    stage: Stage,
                    step: Step));

                return await NavigateTo(vm);
            }
            catch (Exception e)
            {
                LogError(e);
                throw;
            }
        }

        public async Task<bool> LoadEditedAudioAsync(Audio newAudio, string audioPath = null)
        {
            var draftVm = _draftViewModels.FirstOrDefault(x => !x.HasAudio || x.Audio.TemporaryDeleted);

            if (draftVm is null)
            {
                var modalService = ViewModelContextProvider.GetModalService();
                using var component = new DraftReplacementComponentViewModel(_draftViewModels, ViewModelContextProvider);
                using var confirmationModal = new ModalViewModel(viewModelContextProvider: ViewModelContextProvider,
                    modalService: modalService,
                    icon: null,
                    title: AppResources.DraftReplacementModalTitle,
                    contentContentViewModel: component,
                    cancelButtonViewModel: new ModalButtonViewModel(AppResources.Cancel),
                    confirmButtonViewModel: new ModalButtonViewModel(AppResources.Replace))
                {
                    BeforeConfirmCommand = component.ReplaceAudioCommand
                };

                var result = await modalService.ConfirmationModal(confirmationModal);

                if (result == DialogResult.Ok && component.SelectedDraft != null)
                {
                    draftVm = component.SelectedDraft;
                }
            }

            if (draftVm == null)
            {
                return false;
            }

            var newAudioData = newAudio.Data;

            var audio = draftVm.Audio ?? new Audio(Guid.Empty, Guid.Empty, Guid.Empty);
            audio.TemporaryDeleted = false;
            audio.SetAudio(newAudioData);
            audio.SavedDuration = newAudio.SavedDuration;

            if (draftVm.Audio is null)
            {
                draftVm.SetAudio(audio, audioPath);
            }

            await _temporaryAudioRepository.SaveAsync(audio);
            SessionStateService.AddDraftAudio(audio);

            draftVm.Select();

            return true;
        }

        public override void Dispose()
        {
            _sectionReferenceAudioSources.DisposeSourceList();
            _audioDraftSourceList?.DisposeSourceCache();

            _temporaryAudioRepository?.Dispose();

            SequencerRecorderViewModel?.Dispose();
            SequencerRecorderViewModel = null;
            SequencerActionViewModel?.Dispose();
            SequencerActionViewModel = null;

            _conversationService?.Dispose();
            _conversationService = null;

            _passage = null;
            base.Dispose();
        }
    }
}