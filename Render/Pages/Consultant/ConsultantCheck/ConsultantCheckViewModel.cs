using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;
using DynamicData;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Render.Components.BarPlayer;
using Render.Components.Consultant;
using Render.Kernel;
using Render.Models.Workflow;
using Render.Models.Workflow.Stage;
using Render.Repositories.SectionRepository;
using Render.Resources;
using Render.Components.Consultant.ConsultantCheck;
using Render.Extensions;
using Render.Kernel.WrappersAndExtensions;
using Render.Models.Sections;
using Render.Models.Snapshot;
using Render.Repositories.Extensions;
using Render.Resources.Localization;
using Render.Sequencer.Contracts.Enums;
using Render.Sequencer.Contracts.Interfaces;
using Render.Sequencer.Contracts.Models;
using Render.Sequencer.Contracts.ToolbarItems;
using Render.Components.Revision;

namespace Render.Pages.Consultant.ConsultantCheck
{
    public class ConsultantCheckViewModel : WorkflowPageBaseViewModel
    {
        private readonly ISectionRepository _sectionRepository;
        private List<Passage> _passages;

        private readonly AudioSelector _audioSelector;
        private readonly bool _requireNoteListen;
        private readonly bool _isReviewed;
        private SectionSelectCardViewModel SelectCardViewModel { get; }

        private ConversationService _conversationService;

        #region RevisionsDefinitions
        public MultipleRevisionViewModel RevisionActionViewModel { get; set; }
        #endregion

        #region MenuDefinitions
        public readonly DynamicDataWrapper<MenuButtonViewModel> MenuButtons = new();
        private MenuButtonViewModel SelectedMenuButtonViewModel { get; set; }
        #endregion

        #region SequencerDefinitions
        private IToolbarItem _noteToolbarItemModel;
        private IToolbarItem _transcribeToolbarItemModel;
        private IToolbarItem _referenceToolbarItemModel;
        [Reactive] public ISequencerPlayerViewModel SequencerPlayerViewModel { get; private set; }
        #endregion

        #region ReferencesPanelDefinitions
        private SourceList<IBarPlayerViewModel> SectionReferenceAudioSources { get; } = new();
        private ReadOnlyObservableCollection<IBarPlayerViewModel> _barPlayerViewModels;
        public ReadOnlyObservableCollection<IBarPlayerViewModel> BarPlayerViewModels => _barPlayerViewModels;
        [Reactive] public bool ReferencesPanelIsVisible { get; private set; }
        private ReactiveCommand<Unit, Unit> SetReferencesPanelVisibilityCommand { get; }
        #endregion

        #region TranscriptionPanelDefinitions
        [Reactive] public bool TranscribePanelIsVisible { get; private set; }
        public TranscriptionWindowViewModel TranscriptionWindowViewModel { get; private set; }
        private ReactiveCommand<Unit, Unit> SetTranscribePanelVisibilityCommand { get; }
        #endregion


        public static async Task<ConsultantCheckViewModel> CreateAsync(IViewModelContextProvider
            viewModelContextProvider, SectionSelectCardViewModel sectionSelectCard, Step step, Stage stage)
        {
            var transcriptionWindowViewModel = await TranscriptionWindowViewModel.CreateAsync(
                section: sectionSelectCard.Section,
                viewModelContextProvider: viewModelContextProvider);

            var vm = new ConsultantCheckViewModel(viewModelContextProvider, sectionSelectCard,
                transcriptionWindowViewModel, step, stage);

            await vm.LoadSnapshotsAsync(sectionSelectCard.Section.Id);

            return vm;
        }

        private ConsultantCheckViewModel(
            IViewModelContextProvider viewModelContextProvider,
            SectionSelectCardViewModel sectionSelectCard,
            TranscriptionWindowViewModel transcriptionWindowViewModel,
            Step step,
            Stage stage) :
            base(
                urlPathSegment: "ConsultantCheckPage",
                viewModelContextProvider,
                pageName: AppResources.ConsultantCheck,
                sectionSelectCard.Section,
                stage,
                secondPageName: sectionSelectCard.Section.ScriptureReference)
        {
            DisposeOnNavigationCleared = true;
            TitleBarViewModel.DisposeOnNavigationCleared = true;

            SelectCardViewModel = sectionSelectCard;
            Step = step;
            Section = sectionSelectCard.Section;
            _requireNoteListen = step.StepSettings.GetSetting(SettingType.RequireNoteListen);
            _isReviewed = Section.CheckedBy != Guid.Empty;
            _passages = Section.Passages;

            _audioSelector = new AudioSelector(viewModelContextProvider);

            _sectionRepository = ViewModelContextProvider.GetSectionRepository();

            TitleBarViewModel.PageGlyph = IconExtensions.BuildFontImageSource(
                Icon.ConsultantCheckOriginal,
                ResourceExtensions.GetColor("SecondaryText"),
                size: 35)?.Glyph;

            TranscriptionWindowViewModel = transcriptionWindowViewModel;

            SetReferencesPanelVisibilityCommand = ReactiveCommand.Create(SetReferencesPanelVisibility);
            SetTranscribePanelVisibilityCommand = ReactiveCommand.Create(SetTranscribePanelVisibility);
            ProceedButtonViewModel.SetCommand(SetReviewed);

            BuildMenuTabs();

            LoadReferences();

            SetupRevision();

            SetProceedButtonIcon();
        }

        private void SetupListeners()
        {
            Disposables.Add(MenuButtons.Observable
                .WhenPropertyChanged(button => button.IsActive)
                .Subscribe(isActiveProperty =>
                {
                    if (isActiveProperty.Value)
                    {
                        SelectMenuButton(isActiveProperty.Sender);
                    }
                }));

            Disposables.Add(this.WhenAnyValue(vm => vm.RevisionActionViewModel.SelectedRevisionItem)
                .Subscribe(async (revision) => await SelectSnapshot(revision)));

            Disposables.Add(ProceedButtonViewModel.NavigateToPageCommand.IsExecuting
                .Subscribe(isExecuting => { IsLoading = isExecuting; }));
        }

        #region RevisionsImplementation
        private void SetupRevision()
        {
            RevisionActionViewModel = new MultipleRevisionViewModel(ActionState.Optional, ViewModelContextProvider);
            ActionViewModelBaseSourceList.Add(RevisionActionViewModel);
        }

        private async Task LoadSnapshotsAsync(Guid sectionId)
        {
            var snapshots = await RevisionActionViewModel.FillRevisionItems(sectionId, Stage.Id);

            SetupSequencer();

            _conversationService = new ConversationService(
                this,
                Disposables,
                Stage,
                Step,
                SequencerPlayerViewModel);

            _conversationService.TapFlagPostEvent = ProcessStateStatusChange;
            _conversationService.DefineFlagsToDraw(snapshots, Stage.Id);

            SetupButtonStates();
            UpdateSequencer();
            ActionViewModelBaseSourceList.AddRange(MenuButtons.Items);

            SetupListeners();
        }

        private async Task SelectSnapshot(KeyValuePair<Snapshot, string> selectedRevision)
        {
            var snapshot = selectedRevision.Key;

            if (snapshot == null)
            {
                return;
            }

            await RevisionActionViewModel.SelectSnapshot(selectedRevision, getRetellBackTranslations: true, getSegmentBackTranslations: true);

            _passages = RevisionActionViewModel.SelectedSnapshot.Passages;

            MainThread.BeginInvokeOnMainThread(SetupButtonStates);
            UpdateSequencer();
        }
        #endregion

        #region MenuImplementation

        private void BuildMenuTabs()
        {
            // Check whether second step bt is enabled
            var gcc = ViewModelContextProvider.GetGrandCentralStation();
            var workflow = gcc.ProjectWorkflow;
            var entrySteps = workflow.GetAllActiveWorkflowEntrySteps()
                .Where(x => x.RenderStepType == RenderStepTypes.BackTranslate).ToList();
            var secondStepIsEnabled = entrySteps.Count > 1;

            var lastBtStep = entrySteps.LastOrDefault();
            var doRetell = lastBtStep != null && lastBtStep.StepSettings.GetSetting(SettingType.DoRetellBackTranslate);
            var doSegment = lastBtStep != null && lastBtStep.StepSettings.GetSetting(SettingType.DoSegmentBackTranslate);

            MenuButtons.Add(
                new MenuButtonViewModel(
                    AppResources.OriginalLanguage,
                    ViewModelContextProvider,
                    new MenuButtonParameters() { AudioType = ParentAudioType.Draft }));
            MenuButtons.Add(
                new MenuButtonViewModel(
                    secondStepIsEnabled ? AppResources.PassageBackTranslate1 : AppResources.PassageBackTranslate,
                    ViewModelContextProvider,
                    new MenuButtonParameters()
                    {
                        IsBackTranslate = true,
                        IsEnabled = doRetell,
                        AudioType = ParentAudioType.PassageBackTranslation
                    }));
            MenuButtons.Add(
                new MenuButtonViewModel(
                    AppResources.PassageBackTranslate2,
                    ViewModelContextProvider,
                    new MenuButtonParameters()
                    {
                        IsSecondStepBackTranslate = true,
                        IsVisible = secondStepIsEnabled,
                        IsEnabled = doRetell,
                        AudioType = ParentAudioType.PassageBackTranslation2
                    }));
            MenuButtons.Add(
                new MenuButtonViewModel(
                    secondStepIsEnabled ? AppResources.SegmentBackTranslate1 : AppResources.SegmentBackTranslate,
                    ViewModelContextProvider,
                    new MenuButtonParameters()
                    {
                        IsSegmentBackTranslate = true,
                        IsEnabled = doSegment,
                        AudioType = ParentAudioType.SegmentBackTranslation
                    }));
            MenuButtons.Add(
                new MenuButtonViewModel(
                    AppResources.SegmentBackTranslate2,
                    ViewModelContextProvider,
                    new MenuButtonParameters()
                    {
                        IsSegmentBackTranslate = true,
                        IsSecondStepBackTranslate = true,
                        IsVisible = secondStepIsEnabled,
                        IsEnabled = doSegment,
                        AudioType = ParentAudioType.SegmentBackTranslation2
                    }));

            MenuButtons.SourceItems.First().IsActive = true;
        }

        private void SelectMenuButton(MenuButtonViewModel viewModel)
        {
            if (SelectedMenuButtonViewModel != null)
            {
                SelectedMenuButtonViewModel.IsActive = false;
            }

            SelectedMenuButtonViewModel = viewModel;


            MainThread.BeginInvokeOnMainThread(UpdatePanels);

            UpdateSequencer();
        }

        private void UpdatePanels()
        {
            TranscriptionWindowViewModel.UpdateTranscriptions(
                SelectedMenuButtonViewModel.IsBackTranslate,
                SelectedMenuButtonViewModel.IsSegmentBackTranslate,
                SelectedMenuButtonViewModel.IsSecondStepBackTranslate);

            if (ReferencesPanelIsVisible)
            {
                SetReferencesPanelVisibility();
            }

            if (TranscribePanelIsVisible)
            {
                SetTranscribePanelVisibility();
            }
        }

        private bool GetRequiredState(IEnumerable<Draft> audios)
        {
            if (!_requireNoteListen)
            {
                return false;
            }

            var loggedInUserId = ViewModelContextProvider.GetLoggedInUser().Id;

            var messages = _conversationService.GetConversations(audios).SelectMany(c => c.Messages);
            foreach (var message in messages)
            {
                if (message.GetSeenStatus(loggedInUserId) == false && message.UserId != loggedInUserId)
                {
                    return true;
                }
            }

            return false;
        }

        private void SetMenuButtonRequiredState(IEnumerable<Draft> audios, MenuButtonViewModel menuButton)
        {
            var isRequired = GetRequiredState(audios);
            menuButton.ActionState = isRequired && menuButton.IsEnabled
                ? ActionState.Required
                : ActionState.Optional;
            menuButton.IsRequired = RevisionActionViewModel.IsCurrentRevision;
        }

        private void SetupButtonStates()
        {
            if (_conversationService is null)
            {
                return;
            }

            foreach (var menuButton in MenuButtons.Items)
            {

                if (!menuButton.IsVisible)
                {
                    continue;
                }

                var audios = AudioSelector.SelectDraftAudios(_passages, menuButton.AudioType);

                if (!audios.Any())
                {
                    menuButton.IsEnabled = false;
                }
                else
                {
                    SetMenuButtonRequiredState(audios, menuButton);
                }
            }
        }
        #endregion

        #region SequencerImplementation
        private void SetupSequencer()
        {
            SequencerPlayerViewModel = ViewModelContextProvider
                .GetSequencerFactory()
                .CreatePlayer(ViewModelContextProvider.GetAudioPlayer, FlagType.Note);

            SequencerPlayerViewModel.IsRightToLeftDirection = FlowDirection is FlowDirection.RightToLeft;

            _noteToolbarItemModel = SequencerPlayerViewModel.GetToolbarItem<IFlagToolbarItem>();

            _referenceToolbarItemModel = SequencerPlayerViewModel.AddToolbarItem(new ToolbarItemModel(
                type: ToolbarItemType.Custom,
                icon: "Reference",
                actionCommand: SetReferencesPanelVisibilityCommand), 0);

            _transcribeToolbarItemModel = SequencerPlayerViewModel.AddToolbarItem(new ToolbarItemModel(
                type: ToolbarItemType.Custom,
                icon: "Transcribe",
                actionCommand: SetTranscribePanelVisibilityCommand));

            SequencerPlayerViewModel.SetupActivityService(ViewModelContextProvider, Disposables);
            SequencerPlayerViewModel.HasTimer(false);

            SequencerPlayerViewModel
                .WhenAnyValue(player => player.State)
                .Where(state => state == SequencerState.Loaded && !RevisionActionViewModel.IsCurrentRevision)
                .Subscribe((_) => { _noteToolbarItemModel.State = ToolbarItemState.Disabled; });
        }

        private void UpdateSequencer()
        {
            if (SequencerPlayerViewModel != null)
            {
                UpdateAudios();
                UpdateToolbarItems();
            }
        }

        private void UpdateAudios()
        {
            if (SelectedMenuButtonViewModel == null || _passages.IsNullOrEmpty())
            {
                return;
            }

            var audios = _audioSelector.SelectAudioModels(_passages, SelectedMenuButtonViewModel.AudioType,
                c => _conversationService.AllowedStageIdsForDrawingFlags.Contains(c.StageId));
            SequencerPlayerViewModel.SetAudio(audios);

            _conversationService.SequencerAudios = AudioSelector.SelectDraftAudios(_passages, SelectedMenuButtonViewModel.AudioType);
            _conversationService.ParentAudioType = SelectedMenuButtonViewModel.AudioType;
            _conversationService.InitializeNoteDetail(
                _requireNoteListen && RevisionActionViewModel.IsCurrentRevision,
                RevisionActionViewModel.IsCurrentRevision && !SelectedMenuButtonViewModel.IsBackTranslate && !_isReviewed);
        }

        private void UpdateTranscribeToolbarItem()
        {
            if (_transcribeToolbarItemModel == null)
            {
                return;
            }

            _transcribeToolbarItemModel.IsAvailable = !_isReviewed && SelectedMenuButtonViewModel.IsBackTranslate;

            if (TranscribePanelIsVisible)
            {
                _transcribeToolbarItemModel.State = ToolbarItemState.Toggled;
            }
            else
            {
                _transcribeToolbarItemModel.State =
                    TranscriptionWindowViewModel.Transcriptions.SourceItems.Any() && !ReferencesPanelIsVisible
                        ? ToolbarItemState.Active
                        : ToolbarItemState.Disabled;
            }
        }

        private void UpdateReferenceToolbarItem()
        {
            if (_referenceToolbarItemModel == null)
            {
                return;
            }

            if (ReferencesPanelIsVisible)
            {
                _referenceToolbarItemModel.State = ToolbarItemState.Toggled;
            }
            else
            {
                _referenceToolbarItemModel.State =
                    !TranscribePanelIsVisible ? ToolbarItemState.Active : ToolbarItemState.Disabled;
            }
        }

        private void UpdateNoteToolbarItem()
        {
            if (_noteToolbarItemModel == null)
            {
                return;
            }

            _noteToolbarItemModel.IsAvailable = !SelectedMenuButtonViewModel.IsBackTranslate && !_isReviewed;
            _noteToolbarItemModel.State = RevisionActionViewModel.IsCurrentRevision ? ToolbarItemState.Active : ToolbarItemState.Disabled;
        }

        private void UpdateToolbarItems()
        {
            if (SelectedMenuButtonViewModel == null || RevisionActionViewModel.SelectedRevisionItem.Key == null)
            {
                return;
            }

            UpdateNoteToolbarItem();
            UpdateReferenceToolbarItem();
            UpdateTranscribeToolbarItem();
        }
        #endregion

        #region ReferencesPanelImplementation
        private void LoadReferences()
        {
            var referenceCount = 0;

            foreach (var sectionReferenceAudio in Section.References)
            {
                var vm = ViewModelContextProvider.GetBarPlayerViewModel(sectionReferenceAudio,
                    ActionState.Optional,
                    sectionReferenceAudio.Reference.Name, referenceCount++,
                    passageMarkers: sectionReferenceAudio.PassageReferences.Select(x => x.TimeMarkers).ToList());
                SectionReferenceAudioSources.Add(vm);
            }

            var referenceChangeList = SectionReferenceAudioSources.Connect().Publish();
            Disposables.Add(referenceChangeList
                .Bind(out _barPlayerViewModels)
                .Subscribe());
            Disposables.Add(referenceChangeList.Connect());
        }

        private void SetReferencesPanelVisibility()
        {
            ReferencesPanelIsVisible = !ReferencesPanelIsVisible;

            UpdateToolbarItems();
        }
        #endregion

        #region TranscribePanelImplementation
        private void SetTranscribePanelVisibility()
        {
            TranscribePanelIsVisible = !TranscribePanelIsVisible;

            UpdateToolbarItems();
        }
        #endregion

        #region ProceedButtonImplementation

        protected sealed override void SetProceedButtonIcon()
        {
            ProceedButtonViewModel.IsCheckMarkIcon = true;
        }

        protected override void ProcessStateStatusChange()
        {
            if (SelectedMenuButtonViewModel != null && _conversationService?.SequencerAudios is not null)
            {
                SetMenuButtonRequiredState(_conversationService.SequencerAudios, SelectedMenuButtonViewModel);
            }
        }

        private async Task<IRoutableViewModel> SetReviewed()
        {
            var viewModel = await Task.Run(async () =>
            {
                if (Section.CheckedBy == Guid.Empty)
                {
                    SelectCardViewModel.Section.SetCheckedBy(ViewModelContextProvider.GetLoggedInUser().Id);
                    await _sectionRepository.SaveSectionAsync(SelectCardViewModel.Section);
                    await ViewModelContextProvider.GetGrandCentralStation()
                        .AdvanceSectionAfterReviewAsync(Section, Step);
                }

                return await ConsultantCheckSectionSelectViewModel.CreateAsync(Section.ProjectId,
                    ViewModelContextProvider,
                    Stage, Step);
            });

            return await NavigateToAndReset(viewModel);
        }
        #endregion

        public override void Dispose()
        {
            Step = null;
            Section = null;

            _sectionRepository?.Dispose();

            _barPlayerViewModels = null;
            SectionReferenceAudioSources.DisposeSourceList();
            BarPlayerViewModels.DisposeCollection();

            SequencerPlayerViewModel.Dispose();

            RevisionActionViewModel?.Dispose();
            RevisionActionViewModel = null;

            MenuButtons?.Dispose();
            SelectedMenuButtonViewModel = null;

            TranscriptionWindowViewModel?.Dispose();
            TranscriptionWindowViewModel = null;

            _conversationService.Dispose();
            _conversationService = null;

            base.Dispose();
        }
    }
}