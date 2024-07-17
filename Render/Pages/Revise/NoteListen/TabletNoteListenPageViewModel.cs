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
using Render.Pages.Translator.DraftingPage;
using Render.Repositories.Extensions;
using Render.Resources;
using Render.Resources.Localization;
using Render.Sequencer.Contracts.Enums;
using Render.Sequencer.Contracts.Interfaces;
using Render.Sequencer.Contracts.Models;
using Render.Sequencer.Contracts.ToolbarItems;
using System.Reactive;
using System.Reactive.Linq;

namespace Render.Pages.Revise.NoteListen
{
    public class TabletNoteListenPageViewModel : WorkflowPageBaseViewModel
    {
        [Reactive] public ISequencerPlayerViewModel SequencerPlayerViewModel { get; private set; }
        private ActionViewModelBase SequencerActionViewModel { get; set; }
        public MultipleRevisionViewModel RevisionActionViewModel { get; set; }

        private IToolbarItem _noteToolbarItemModel;
        private IToolbarItem _reRecordToolbarItemModel;
        private ReactiveCommand<Unit, Unit> NavigateToDraftingCommand { get; set; }

        private List<Passage> _passages;

        private readonly bool _requireNoteListen;
        
        private ConversationService _conversationService;

        public static async Task<TabletNoteListenPageViewModel> CreateAsync(
            IViewModelContextProvider
            viewModelContextProvider,
            Section section,
            Step step,
            Stage stage)
        {
            if (!section.HasReferenceAudio)
            {
                var sectionRepository = viewModelContextProvider.GetSectionRepository();
                section = await sectionRepository.GetSectionWithReferencesAsync(section.Id);
            }

            var vm = new TabletNoteListenPageViewModel(
                viewModelContextProvider,
                section,
                step,
                stage);

            vm.IsLoading = true;
            await Task.Run(async () => await vm.LoadSnapshotsAsync(section.Id));
            vm.IsLoading = false;

            return vm;
        }

        private TabletNoteListenPageViewModel(
            IViewModelContextProvider viewModelContextProvider,
            Section section,
            Step step,
            Stage stage) :
            base(urlPathSegment: "TabletSectionReviewPage",
                viewModelContextProvider: viewModelContextProvider,
                pageName: GetStepName(viewModelContextProvider, step.RenderStepType, stage.Id),
                section: section,
                stage: stage,
                step: step,
                secondPageName: AppResources.PassageSelect)
        {
            DisposeOnNavigationCleared = true;
            TitleBarViewModel.DisposeOnNavigationCleared = true;

            var icon = step.RenderStepType == RenderStepTypes.ConsultantRevise ? Icon.ConsultantRevise :
                step.RenderStepType == RenderStepTypes.PeerRevise ? Icon.PeerRevise : Icon.CommunityRevise;
            TitleBarViewModel.PageGlyph = IconExtensions.BuildFontImageSource(icon)?.Glyph;

            _requireNoteListen = Step.StepSettings.GetSetting(SettingType.RequireNoteListen);

			ProceedButtonViewModel.SetCommand(NavigateForwardAsync);
            Disposables.Add(ProceedButtonViewModel.NavigateToPageCommand.IsExecuting
                .Subscribe(isExecuting => { IsLoading = isExecuting; }));
            
            NavigateToDraftingCommand = ReactiveCommand.CreateFromTask(NavigateToDraftingAsync);
            NavigateToDraftingCommand.IsExecuting
                .Subscribe(isExecuting => IsLoading = isExecuting);
            
            SetupSequencer();
            
            _conversationService = new ConversationService(
                this,
                Disposables,
                Stage,
                Step,
                SequencerPlayerViewModel,
                appendNotesForChildAudios: Step.RenderStepType == RenderStepTypes.ConsultantRevise);

            _conversationService.TapFlagPostEvent = ProcessStateStatusChange;

            SetupRevision();

            Disposables.Add(this.WhenAnyValue(vm => vm.RevisionActionViewModel.SelectedRevisionItem)
                .Subscribe(SelectSnapshot));

            SetProceedButtonIcon();
        }

        private void SetupSequencer()
        {
            SequencerPlayerViewModel = ViewModelContextProvider
                .GetSequencerFactory()
                .CreatePlayer(
                    playerFactory: ViewModelContextProvider.GetAudioPlayer,
                    flagType: FlagType.Note
            );

            SequencerPlayerViewModel.IsRightToLeftDirection = FlowDirection is FlowDirection.RightToLeft;

            _noteToolbarItemModel = SequencerPlayerViewModel.GetToolbarItem<IFlagToolbarItem>();
            _reRecordToolbarItemModel = SequencerPlayerViewModel.AddToolbarItem(new ToolbarItemModel(ToolbarItemType.Custom, "ReRecord", NavigateToDraftingCommand), 0);

            SequencerPlayerViewModel.SetupActivityService(ViewModelContextProvider, Disposables);

            SequencerActionViewModel = SequencerPlayerViewModel.CreateActionViewModel(
                provider: ViewModelContextProvider,
                disposables: Disposables);
            ActionViewModelBaseSourceList.Add(SequencerActionViewModel);

            SequencerPlayerViewModel
                .WhenAnyValue(player => player.State)
                .Where(state => state == SequencerState.Playing && RevisionActionViewModel.IsCurrentRevision)
                .Subscribe((_) => { SequencerActionViewModel.ActionState = ActionState.Optional; });

            SequencerPlayerViewModel
                .WhenAnyValue(player => player.State)
                .Where(state => state == SequencerState.Loaded && !RevisionActionViewModel.IsCurrentRevision)
                .Subscribe((_) => { _noteToolbarItemModel.State = ToolbarItemState.Disabled; });
        }

        private void SetupRevision()
        {
            RevisionActionViewModel = new MultipleRevisionViewModel(ActionState.Optional, ViewModelContextProvider);
            ActionViewModelBaseSourceList.Add(RevisionActionViewModel);
        }

        private void UpdateAudios()
        {
            if (_passages.IsNullOrEmpty())
            {
                return;
            }

            _conversationService.SequencerAudios = _passages.Select(passage => passage.CurrentDraftAudio);
            _conversationService.InitializeNoteDetail(
                isRequired: _requireNoteListen,
                allowEditing: RevisionActionViewModel.IsCurrentRevision);
            
            var audios = _passages.Select(passage =>
            {
                var isOriginalRecord = !RevisionActionViewModel.IsCurrentRevision || RevisionActionViewModel.SelectedRevisionItem.Key.Passages.First(x => x.Id == passage.Id)
                        .CurrentDraftAudio.Id.Equals(passage.CurrentDraftAudio.Id);

                return passage.CreatePlayerAudioModel(
                    _conversationService.GetConversations(passage.CurrentDraftAudio),
                    path: ViewModelContextProvider.GetTempAudioService(passage.CurrentDraftAudio).SaveTempAudio(),
                    name: string.Format(AppResources.Passage, passage.PassageNumber.PassageNumberString),
                    startIcon: Icon.PassageNew.ToString(),
                    endIcon: isOriginalRecord ? string.Empty : Icon.ReRecord.ToString(),
                    option: isOriginalRecord ? AudioOption.Optional : AudioOption.Completed,
                    flagType: FlagType.Note);
            }).ToArray();

            SequencerPlayerViewModel.SetAudio(audios);
        }

        protected sealed override void SetProceedButtonIcon()
        {
            switch (Step.RenderStepType)
            {
                case RenderStepTypes.PeerRevise:
                case RenderStepTypes.ConsultantRevise:
                case RenderStepTypes.CommunityRevise:
                    ProceedButtonViewModel.IsCheckMarkIcon = true;
                    break;
            }
        }

        protected override void OnNavigatingBack()
        {
            base.OnNavigatingBack();
            MainThread.BeginInvokeOnMainThread(UpdateSequencer);
        }

        private async Task<IRoutableViewModel> NavigateForwardAsync()
        {
            await Task.Run(async () =>
            {
                var grandCentralStation = ViewModelContextProvider.GetGrandCentralStation();
                await grandCentralStation.CreateTemporarySnapshotAfterReRecording(Section, Step);
                await grandCentralStation.AdvanceSectionAfterReviseAsync(Section, Step, ViewModelContextProvider.GetSessionStateService());
            });
            return await NavigateToHomeOnMainStackAsync();
        }

        private async Task LoadSnapshotsAsync(Guid sectionId)
        {
            var snapshots = await RevisionActionViewModel.FillRevisionItems(sectionId, Stage.Id);
            _conversationService.DefineFlagsToDraw(snapshots, Stage.Id);

            MainThread.BeginInvokeOnMainThread(UpdateSequencer);
        }

        private async void SelectSnapshot(KeyValuePair<Snapshot, string> selectedRevision)
        {
            var snapshot = selectedRevision.Key;
            
            if (snapshot == null)
            {
                return;
            }

            await RevisionActionViewModel.SelectSnapshot(selectedRevision, getRetellBackTranslations: true, getSegmentBackTranslations: true);

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
            if (noteItem == null)
            {
                var playItem = SequencerPlayerViewModel?.GetToolbarItem<IPlayToolbarItem>();

                if (playItem != null)
                {
                    SequencerPlayerViewModel.AddToolbarItemAfter(playItem, _noteToolbarItemModel);
                }
            }

			_noteToolbarItemModel.State = RevisionActionViewModel.IsCurrentRevision ? ToolbarItemState.Active
				: ToolbarItemState.Disabled;

			_reRecordToolbarItemModel.State = _noteToolbarItemModel.State;
		}

        private async Task NavigateToDraftingAsync()
        {
            if (RevisionActionViewModel.IsCurrentRevision is false) return;

            IsLoading = true;

            var viewModel = await Task.Run(async () =>
            {
                var audio = SequencerPlayerViewModel.GetCurrentAudio();
                if (audio is null) return null;

                var passage = _passages.FirstOrDefault(passage => passage.CurrentDraftAudio.Id == audio.Key);
                if (passage is null) return null;

                return await DraftingViewModel.CreateAsync(Section, passage, Step, ViewModelContextProvider, Stage);
            });


            if (viewModel != null)
            {
                await NavigateTo(viewModel);
            }

            IsLoading = false;
        }

        public override void Dispose()
        {
            _passages = null;

            SequencerPlayerViewModel?.Dispose();
            SequencerPlayerViewModel = null;
            SequencerActionViewModel?.Dispose();
            SequencerActionViewModel = null;

            RevisionActionViewModel?.Dispose();
            RevisionActionViewModel = null;

            _conversationService.Dispose();
            _conversationService = null;

            base.Dispose();
        }
    }
}