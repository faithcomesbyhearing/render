using DynamicData;
using ReactiveUI;
using Render.Components.BarPlayer;
using Render.Kernel;
using Render.Kernel.WrappersAndExtensions;
using Render.Models.Sections;
using Render.Models.Workflow;
using Render.Models.Workflow.Stage;
using Render.Resources.Localization;
using System.Reactive;
using System.Reactive.Linq;
using ReactiveUI.Fody.Helpers;
using Render.Extensions;
using Render.Sequencer.Contracts.Enums;
using Render.Sequencer.Contracts.Interfaces;
using Render.Sequencer.Contracts.Models;
using Render.Sequencer.Contracts.ToolbarItems;
using Render.Repositories.SnapshotRepository;

namespace Render.Pages.Translator.PassageReview
{
    public class TabletPassageReviewPageViewModel : PassageReviewPageBaseViewModel
    {
        private readonly ISnapshotRepository _snapshotRepository;
        public DynamicDataWrapper<IBarPlayerViewModel> References { get; private set; }

        [Reactive]
        public ISequencerPlayerViewModel SequencerPlayerViewModel { get; private set; }

        private ActionViewModelBase SequencerActionViewModel { get; set; }
        private ReactiveCommand<Unit, Unit> NavigateToDraftingCommand { get; set; }

        private ConversationService _conversationService;

        public static TabletPassageReviewPageViewModel Create(
            IViewModelContextProvider viewModelContextProvider,
            Section section,
            Passage passage,
            Step step,
            Stage stage)
        {
            var pageViewModel = new TabletPassageReviewPageViewModel(
                viewModelContextProvider: viewModelContextProvider,
                pageName: GetStepName(step),
                secondPageName: AppResources.PassageReview,
                section: section,
                passage: passage,
                step: step,
                stage: stage);

            pageViewModel.Initialize();
            return pageViewModel;
        }

        private TabletPassageReviewPageViewModel(
            IViewModelContextProvider viewModelContextProvider,
            string pageName,
            string secondPageName,
            Section section,
            Passage passage,
            Step step,
            Stage stage)
            : base(
                viewModelContextProvider: viewModelContextProvider,
                pageName: pageName,
                secondPageName: secondPageName,
                section: section,
                passage: passage,
                step: step,
                urlPathSegment: "TabletPassageReviewPage",
                stage: stage)
        {
            References = new DynamicDataWrapper<IBarPlayerViewModel>();
            _snapshotRepository = viewModelContextProvider.GetSnapshotRepository();
        }

        public async void Refresh()
        {
            SetupSequencer();
            await SetupConversationService();
            UpdateAudios();
        }

        private void Initialize()
        {
            // Toolbar
            NavigateToDraftingCommand = ReactiveCommand.CreateFromTask(NavigateToReRecord);
            NavigateToDraftingCommand
                .IsExecuting
                .Subscribe(isExecuting => IsLoading = isExecuting);

            PopulateReferences();
        }

        private void PopulateReferences()
        {
            var passageReviewRequired = Step.StepSettings.GetSetting(SettingType.RequirePassageReview);
            var referenceCount = 0;
            var passageNumber = Passage.PassageNumber.Number;
            foreach (var referenceAudio in Section.References.Where(x => !x.LockedReferenceByPassageNumbersList.Contains(passageNumber)))
            {
                var barPlayer = PopulateReference(referenceAudio, Passage, passageReviewRequired, referenceCount++);
                References.Add(barPlayer);
                ActionViewModelBaseSourceList.Add(barPlayer);
            }
        }

        private void SetupSequencer()
        {
            _conversationService?.Dispose();
            SequencerActionViewModel?.Dispose();
            SequencerPlayerViewModel?.Dispose();

            var flagType = Stage.StageType switch
            {
                StageTypes.CommunityTest => FlagType.None,
                _ => FlagType.Note
            };

            SequencerPlayerViewModel = ViewModelContextProvider
                .GetSequencerFactory()
                .CreatePlayer(ViewModelContextProvider.GetAudioPlayer, flagType);

            SequencerPlayerViewModel.IsRightToLeftDirection = FlowDirection is FlowDirection.RightToLeft;

            SequencerPlayerViewModel.AddToolbarItem(new ToolbarItemModel(ToolbarItemType.Custom, "ReRecord", NavigateToDraftingCommand), 0);
            SequencerPlayerViewModel.SetupActivityService(ViewModelContextProvider, Disposables);

            SequencerActionViewModel = SequencerPlayerViewModel.CreateActionViewModel(
                required: Step.StepSettings.GetSetting(SettingType.RequirePassageReview),
                requirementId: Passage.CurrentDraftAudio?.Id,
                provider: ViewModelContextProvider,
                disposables: Disposables);
            ActionViewModelBaseSourceList.Add(SequencerActionViewModel);

            var addFlagCommand = SequencerPlayerViewModel.GetToolbarItem<IFlagToolbarItem>();
            if (Step.RenderStepType == RenderStepTypes.CommunityRevise)
            {
                if (addFlagCommand != null) addFlagCommand.IsAvailable = false;
            }

            Disposables.Add(SequencerPlayerViewModel.WhenAnyValue(vm => vm.State)
                .Where(state => state == SequencerState.Playing)
                .Subscribe(_ => SequencerActionViewModel.ActionState = ActionState.Optional));
        }

        private async Task SetupConversationService()
        {
            var snapshots = await _snapshotRepository.GetSnapshotsForSectionAsync(Section.Id);

            _conversationService = new ConversationService(
                this,
                Disposables,
                Stage,
                Step,
                SequencerPlayerViewModel,
                appendNotesForChildAudios: Step.RenderStepType == RenderStepTypes.ConsultantRevise);

            _conversationService.TapFlagPostEvent = ProcessStateStatusChange;
            _conversationService.DefineFlagsToDraw(snapshots, Stage.Id);
        }

        private void UpdateAudios()
        {
            var flagType = Stage.StageType switch
            {
                StageTypes.CommunityTest => FlagType.None,
                _ => FlagType.Note
            };

            var audio = Passage.CreatePlayerAudioModel(
                _conversationService.GetConversations(Passage.CurrentDraftAudio),
                path: ViewModelContextProvider.GetTempAudioService(Passage.CurrentDraftAudio).SaveTempAudio(),
                name: string.Format(AppResources.Passage, Passage.PassageNumber.PassageNumberString),
                startIcon: null,
                endIcon: null,
                option: AudioOption.Optional,
                flagType: flagType,
                userId: ViewModelContextProvider.GetLoggedInUser().Id);

            SequencerPlayerViewModel.SetAudio([audio]);

            _conversationService.SequencerAudios = [Passage.CurrentDraftAudio];
            _conversationService.InitializeNoteDetail(default);
        }

        public override void Dispose()
        {
            References.Dispose();

            SequencerPlayerViewModel?.Dispose();
            SequencerPlayerViewModel = null;
            SequencerActionViewModel?.Dispose();
            SequencerActionViewModel = null;
            _conversationService?.Dispose();
            _conversationService = null;

            ActionViewModelBaseSourceList?.Dispose();

            base.Dispose();
        }
    }
}