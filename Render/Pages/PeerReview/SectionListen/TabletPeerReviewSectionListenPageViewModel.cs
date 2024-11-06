using DynamicData;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Render.Components.Revision;
using Render.Extensions;
using Render.Kernel;
using Render.Models.Sections;
using Render.Models.Snapshot;
using Render.Models.Workflow;
using Render.Models.Workflow.Stage;
using Render.Pages.PeerReview.PassageListen.Tablet;
using Render.Resources;
using Render.Resources.Localization;
using Render.Sequencer.Contracts.Enums;
using Render.Sequencer.Contracts.Interfaces;
using Render.Sequencer.Contracts.ToolbarItems;
using System.Reactive.Linq;
using Render.Services.SectionMovementService;

namespace Render.Pages.PeerReview.SectionListen
{
    public class TabletPeerReviewSectionListenPageViewModel : WorkflowPageBaseViewModel
    {
        private readonly bool _requireNoteListen;
        private List<Passage> _passages;

        public string Title { get; }

        [Reactive] public ISequencerPlayerViewModel SequencerPlayerViewModel { get; private set; }
        private ActionViewModelBase _sequencerActionViewModel { get; set; }
        public MultipleRevisionViewModel RevisionActionViewModel { get; set; }

        private IToolbarItem _noteToolbarItemModel;

        private ISectionMovementService _sectionMovementService;

        private ConversationService _conversationService;

        public static async Task<TabletPeerReviewSectionListenPageViewModel> CreateAsync(
            IViewModelContextProvider viewModelContextProvider,
            Section section,
            Step step,
            Stage stage)
        {
            var vm = new TabletPeerReviewSectionListenPageViewModel(
                viewModelContextProvider,
                section,
                step,
                stage,
                requireNoteListen: step.StepSettings.GetSetting(SettingType.RequireNoteListen));

            vm.IsLoading = true;
            await Task.Run(async () => await vm.LoadSnapshotsAsync(section.Id));
            vm.IsLoading = false;
            return vm;
        }

        private TabletPeerReviewSectionListenPageViewModel(
            IViewModelContextProvider viewModelContextProvider,
            Section section,
            Step step,
            Stage stage,
            bool requireNoteListen) :
            base(urlPathSegment: "TabletTeamCheckSectionListenPage",
                viewModelContextProvider: viewModelContextProvider,
                pageName: GetStepName(step),
                section: section,
                stage: stage,
                step: step,
                secondPageName: AppResources.SectionListen)
        {
            _requireNoteListen = requireNoteListen;

            TitleBarViewModel.PageGlyph = ((FontImageSource)ResourceExtensions.GetResourceValue("PeerCheckWhite"))?.Glyph;
            Title = string.Format(AppResources.Section, section.Number);

            _sectionMovementService = viewModelContextProvider.GetSectionMovementService();

            ProceedButtonViewModel.SetCommand(NavigateToPassageListenAsync);

            SetupSequencer();
            SetupConversationService(SequencerPlayerViewModel);
            SetupRevision();
            SetProceedButtonIcon();
        }

        protected sealed override void SetProceedButtonIcon()
        {
            if (Step.RenderStepType == RenderStepTypes.PeerCheck)
            {
                if (!Step.StepSettings.GetSetting(SettingType.DoPassageReview))
                {
                    ProceedButtonViewModel.IsCheckMarkIcon = true;
                }
            }
        }

        protected override void OnNavigatingBack()
        {
            base.OnNavigatingBack();
            MainThread.BeginInvokeOnMainThread(UpdateSequencer);
        }

        private void SetupRevision()
        {
            RevisionActionViewModel = new MultipleRevisionViewModel(ActionState.Optional, ViewModelContextProvider);
            ActionViewModelBaseSourceList.Add(RevisionActionViewModel);
        }

        private void SetupSequencer()
        {
            var flagType = FlagType.Note;

            SequencerPlayerViewModel = ViewModelContextProvider
                .GetSequencerFactory()
                .CreatePlayer(ViewModelContextProvider.GetAudioPlayer, flagType);

            SequencerPlayerViewModel.IsRightToLeftDirection = FlowDirection is FlowDirection.RightToLeft;

            _noteToolbarItemModel = SequencerPlayerViewModel.GetToolbarItem<IFlagToolbarItem>();
            SequencerPlayerViewModel.SetupActivityService(ViewModelContextProvider, Disposables);
        }

        private void SetupConversationService(ISequencerPlayerViewModel sequencerPlayer)
        {
            _conversationService = new ConversationService(
                this,
                Disposables,
                Stage,
                Step,
                sequencerPlayer);

            _conversationService.TapFlagPostEvent = ProcessStateStatusChange;
        }

        private async Task LoadSnapshotsAsync(Guid sectionId)
        {
            var snapshots = await RevisionActionViewModel.FillRevisionItems(sectionId, Stage.Id);

            _sequencerActionViewModel = SequencerPlayerViewModel.CreateActionViewModel(
                required: Step.StepSettings.GetSetting(SettingType.RequireSectionReview),
                requirementId: RevisionActionViewModel.SelectedRevisionItem.Key.Id,
                provider: ViewModelContextProvider,
                disposables: Disposables);
            ActionViewModelBaseSourceList.Add(_sequencerActionViewModel);

            _conversationService.DefineFlagsToDraw(snapshots, Stage.Id);

            SetupListeners();
        }

        private void SetupListeners()
        {
            Disposables.Add(ProceedButtonViewModel.NavigateToPageCommand.IsExecuting
                .Subscribe(isExecuting => { IsLoading = isExecuting; }));

            Disposables.Add(this.WhenAnyValue(vm => vm.RevisionActionViewModel.SelectedRevisionItem)
                .Where(revision => revision.Key is not null)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(SelectSnapshot));

            Disposables.Add(SequencerPlayerViewModel
                .WhenAnyValue(player => player.State)
                .Where(state => state == SequencerState.Playing && RevisionActionViewModel.IsCurrentRevision)
                .Subscribe((_) => { _sequencerActionViewModel.ActionState = ActionState.Optional; }));

            Disposables.Add(SequencerPlayerViewModel
                .WhenAnyValue(player => player.State)
                .Where(state => state == SequencerState.Loaded && !RevisionActionViewModel.IsCurrentRevision)
                .Subscribe((_) => { _noteToolbarItemModel.State = ToolbarItemState.Disabled; }));
        }

        private async void SelectSnapshot(KeyValuePair<Snapshot, string> selectedRevision)
        {
            var snapshot = selectedRevision.Key;

            if (snapshot == null)
            {
                return;
            }

            await RevisionActionViewModel.SelectSnapshot(selectedRevision);

            _passages = RevisionActionViewModel.IsCurrentRevision ? Section.Passages : RevisionActionViewModel.SelectedSnapshot.Passages;

            MainThread.BeginInvokeOnMainThread(UpdateSequencer);
        }

        private void UpdateSequencer()
        {
            if (SequencerPlayerViewModel != null)
            {
                UpdateAudios();
                UpdateToolBarState();
            }
        }

        private void UpdateToolBarState()
        {
            var noteItem = SequencerPlayerViewModel?.GetToolbarItem<IFlagToolbarItem>();
            var playItem = SequencerPlayerViewModel?.GetToolbarItem<IPlayToolbarItem>();
            if (playItem != null && noteItem == null)
            {
                SequencerPlayerViewModel.AddToolbarItemAfter(playItem, _noteToolbarItemModel);
            }

            _noteToolbarItemModel.State = RevisionActionViewModel.IsCurrentRevision ? ToolbarItemState.Active
                : ToolbarItemState.Disabled;

            if (playItem != null)
            {
                var option = _sequencerActionViewModel.ActionState == ActionState.Optional ? ItemOption.Optional : ItemOption.Required;
                playItem.Option = RevisionActionViewModel.IsCurrentRevision ? option : ItemOption.Optional;
            }
        }

        private void UpdateAudios()
        {
            var audios = _passages.Select(passage => passage.CreatePlayerAudioModel(
                _conversationService.GetConversations(passage.CurrentDraftAudio),
                path: ViewModelContextProvider.GetTempAudioService(passage.CurrentDraftAudio).SaveTempAudio(),
                name: string.Format(AppResources.Passage, passage.PassageNumber.PassageNumberString),
                startIcon: Icon.PassageNew.ToString(),
                endIcon: null,
                option: AudioOption.Optional,
                flagType: FlagType.Note,
                userId: ViewModelContextProvider.GetLoggedInUser().Id,
                requireNoteListen: _requireNoteListen)).ToArray();

            SequencerPlayerViewModel.SetAudio(audios);

            _conversationService.SequencerAudios = _passages.Select(p => p.CurrentDraftAudio);
            _conversationService.InitializeNoteDetail(
                _requireNoteListen,
                RevisionActionViewModel.IsCurrentRevision);
        }

        private async Task<IRoutableViewModel> NavigateToPassageListenAsync()
        {
            if (Step.StepSettings.GetSetting(SettingType.DoPassageReview))
            {
                var vm = await Task.Run(async () => await TabletPeerReviewPassageListenPageViewModel.CreateAsync(
                    ViewModelContextProvider, Section, Section.Passages.First(), Step, Stage));
                return await NavigateTo(vm);
            }

            await Task.Run(async () => { await _sectionMovementService.AdvanceSectionAfterReviewAsync(Section, Step, GetProjectId(), GetLoggedInUserId()); });
            return await NavigateToHomeOnMainStackAsync();
        }

        public override void Dispose()
        {
            _passages = null;

            SequencerPlayerViewModel?.Dispose();
            SequencerPlayerViewModel = null;
            _sequencerActionViewModel?.Dispose();
            _sequencerActionViewModel = null;

            RevisionActionViewModel?.Dispose();
            RevisionActionViewModel = null;

            _conversationService.Dispose();
            _conversationService = null;
            _sectionMovementService.Dispose();
            _sectionMovementService = null;

            base.Dispose();
        }
    }
}