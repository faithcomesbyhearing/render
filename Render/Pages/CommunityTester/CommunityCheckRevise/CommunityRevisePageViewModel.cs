using System.Reactive;
using System.Reactive.Linq;
using DynamicData;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Render.Components.FlagDetail;
using Render.Extensions;
using Render.Models.Extensions;
using Render.Kernel;
using Render.Models.Sections;
using Render.Models.Snapshot;
using Render.Models.Workflow;
using Render.Models.Workflow.Stage;
using Render.Pages.Translator.DraftingPage;
using Render.Resources;
using Render.Resources.Localization;
using Render.Sequencer.Contracts.Enums;
using Render.Sequencer.Contracts.Interfaces;
using Render.Sequencer.Contracts.Models;
using Render.Sequencer.Contracts.ToolbarItems;
using FlagState = Render.Components.NotePlacementPlayer.FlagState;

namespace Render.Pages.CommunityTester.CommunityCheckRevise
{
    public class CommunityRevisePageViewModel : WorkflowPageBaseViewModel
    {
        [Reactive] public ISequencerPlayerViewModel SequencerPlayerViewModel { get; private set; }
        private ActionViewModelBase SequencerActionViewModel { get; set; }
        private Dictionary<Guid, SequencerFlagDetailViewModel> SequencerFlagDetailViewModels { get; } = new();

        [Reactive] private ReactiveCommand<Unit, IRoutableViewModel> NavigateToDraftingRoutedCommand { get; set; }

        private Snapshot OriginalSnapshot { get; set; }

        public static async Task<CommunityRevisePageViewModel> CreateAsync(
            IViewModelContextProvider viewModelContextProvider,
            Section section,
            Step step,
            Stage stage)
        {
            var vm = new CommunityRevisePageViewModel(
                viewModelContextProvider: viewModelContextProvider,
                section: section,
                step: step,
                stage: stage);

            vm.OriginalSnapshot = await vm.GetOriginalSnapshotForStage(section.Id);
            vm.SetupSequencer();
            return vm;
        }

        private CommunityRevisePageViewModel(
            IViewModelContextProvider viewModelContextProvider,
            Section section,
            Step step,
            Stage stage) : base(
            urlPathSegment: "CommunityRevisePage",
            viewModelContextProvider,
            pageName: GetStepName(step),
            section,
            stage,
            step,
            secondPageName: AppResources.PassageSelect)
        {
            TitleBarViewModel.PageGlyph =
                ResourceExtensions.GetResourceValue<string>(Icon.CommunityRevise.ToString());

            NavigateToDraftingRoutedCommand = ReactiveCommand.CreateFromTask(NavigateToDraftingRoutedAsync);

            ProceedButtonViewModel.SetCommand(NavigateHomeAsync);

            NavigateToDraftingRoutedCommand
                .IsExecuting
                .Subscribe(isExecuting => IsLoading = isExecuting);

            SetProceedButtonIcon();
        }

        protected sealed override void SetProceedButtonIcon()
        {
            ProceedButtonViewModel.IsCheckMarkIcon = true;
        }

        private void SetupSequencer()
        {
            SequencerPlayerViewModel = ViewModelContextProvider.GetSequencerFactory().CreatePlayer(
                playerFactory: ViewModelContextProvider.GetAudioPlayer,
                flagType: FlagType.Marker
            );

            SequencerPlayerViewModel.IsRightToLeftDirection = FlowDirection is FlowDirection.RightToLeft;

            var flagToolbarItemModel = SequencerPlayerViewModel.GetToolbarItem<IFlagToolbarItem>();
            if (flagToolbarItemModel != null) flagToolbarItemModel.IsAvailable = false;

            SequencerPlayerViewModel.AddToolbarItem(
                new ToolbarItemModel(ToolbarItemType.Custom,
                    "ReRecord",
                    NavigateToDraftingRoutedCommand),
                0);

            SequencerPlayerViewModel.SetupActivityService(ViewModelContextProvider, Disposables);

            var passageDraftAudioIds = Section.Passages
                .Where(a => a.CurrentDraftAudio is not null)
                .Select(a => a.CurrentDraftAudio.Id)
                .Merge();
            SequencerActionViewModel = SequencerPlayerViewModel.CreateActionViewModel(
                required: Step.StepSettings.GetSetting(SettingType.RequireSectionListen),
                requirementId: passageDraftAudioIds,
                provider: ViewModelContextProvider,
                disposables: Disposables);
            ActionViewModelBaseSourceList.Add(SequencerActionViewModel);

			//Initialize SequencerFlagDetailVM
			var requiredCommunityFeedBack = Step.StepSettings.GetSetting(SettingType.RequireCommunityFeedback);
            Section.Passages.ForEach(passage => SequencerFlagDetailViewModels.Add(passage.Id,
                new SequencerFlagDetailViewModel(Section, passage, Stage, SequencerPlayerViewModel, ViewModelContextProvider, ActionViewModelBaseSourceList, Disposables,
                    requiredCommunityFeedBack)));

            SequencerPlayerViewModel.SetAudio(Section.Passages.Select(InitializeAudioAndFlags).ToArray());

            Disposables.Add(SequencerPlayerViewModel
                .WhenAnyValue(player => player.State)
                .Where(state => state == SequencerState.Playing)
                .Subscribe(_ => { SequencerActionViewModel.ActionState = ActionState.Optional; }));

            SequencerPlayerViewModel.TapFlagCommand = ReactiveCommand.CreateFromTask(
                async (IFlag flag) => { await ShowFlagAsync(flag); });
        }

        private PlayerAudioModel InitializeAudioAndFlags(Passage passage)
        {
            var sequencerFlagDetail = SequencerFlagDetailViewModels[passage.Id];
            var markers = sequencerFlagDetail.CommunityTestFlagMarkers.Select(comTestMarkerItem => new MarkerFlagModel(
                key: comTestMarkerItem.Flag.Id,
                position: comTestMarkerItem.Flag.TimeMarker,
                required: comTestMarkerItem.FlagState is FlagState.Required,
                symbol: comTestMarkerItem.ItemsCount.ToString(),
                read: comTestMarkerItem.FlagState is FlagState.Viewed)).ToList();

            var path = ViewModelContextProvider
                .GetTempAudioService(passage.CurrentDraftAudio)
                .SaveTempAudio();

            var passageEndIcon = GetPassageEndIcon(sequencerFlagDetail, passage);
            return PlayerAudioModel.Create(
                path: path,
                name: string.Format(AppResources.Passage, passage.PassageNumber.PassageNumberString),
                startIcon: Icon.PassageNew.ToString(),
                endIcon: passageEndIcon.Icon,
                option: passageEndIcon.AudioOption,
                key: passage.Id,
                flags: markers,
                number: passage.PassageNumber.PassageNumberString);
        }

        private async Task<Snapshot> GetOriginalSnapshotForStage(Guid sectionId)
        {
            var snapshotRepository = ViewModelContextProvider.GetSnapshotRepository();
            var snapshots = await snapshotRepository.GetSnapshotsForSectionAsync(sectionId);
            //Sort order by date oldest to latest
            var currentSectionSnapshots = snapshotRepository.FilterSnapshotByStageId(snapshots, Stage.Id);

            if (!currentSectionSnapshots.Any() && snapshots.Any())
            {
                // stage was deleted
                return await snapshotRepository.GetPassageDraftsForSnapshot(snapshots[0]);
            }

            return await snapshotRepository.GetPassageDraftsForSnapshot(currentSectionSnapshots[^1]);
        }

        private (string Icon, AudioOption AudioOption) GetPassageEndIcon(SequencerFlagDetailViewModel sequencerFlagDetail, Passage passage)
        {
            var hasRetells = sequencerFlagDetail.CommunityTestForStages.Retells.Any();
            if (hasRetells)
            {
                return (string.Empty, AudioOption.Optional);
            }
			var isOriginalAudio = OriginalSnapshot.Passages.Any(x => x.CurrentDraftAudio.Id == passage.CurrentDraftAudio.Id);
			return isOriginalAudio ? (string.Empty, AudioOption.Optional) : (Icon.ReRecord.ToString(), AudioOption.Completed);
        }

        private async Task<IRoutableViewModel> NavigateHomeAsync()
        {
            await Task.Run(async () =>
            {
                await ViewModelContextProvider.GetSectionMovementService()
                    .AdvanceSectionAfterReviseAsync(Section, Step, ViewModelContextProvider.GetSessionStateService(), GetProjectId(), GetLoggedInUserId());
            });
            return await NavigateToHomeOnMainStackAsync();
        }

        private async Task<IRoutableViewModel> NavigateToDraftingRoutedAsync()
        {
            var draftViewModel = await DraftingViewModel.CreateAsync(Section, GetCurrentPassage(),
                Step, ViewModelContextProvider, Stage);

            return await NavigateTo(draftViewModel);
        }

        private async Task ShowFlagAsync(IFlag flag)
        {
            if (flag.Key == default && GetCurrentPassage() != null)
            {
                await SequencerFlagDetailViewModels[GetCurrentPassage().Id].ShowFlagMarkerAsync(flag);
                return;
            }

            var sequencers = SequencerFlagDetailViewModels.Where(ps => ps.Value.CommunityTestFlagMarkers.Any(c => c.Flag.Id == flag.Key));

            if (!sequencers.Any())
            {
                return;
            }

            await sequencers.First().Value.ShowFlagMarkerAsync(flag);
        }

        private Passage GetCurrentPassage()
        {
            var audio = SequencerPlayerViewModel.GetCurrentAudio();
            var passage = Section.Passages.FirstOrDefault(passage => passage.Id == audio?.Key);
            return passage;
        }


        public override void Dispose()
        {
            SequencerPlayerViewModel?.Dispose();
            SequencerPlayerViewModel = null;

            SequencerActionViewModel?.Dispose();
            SequencerActionViewModel = null;

            SequencerFlagDetailViewModels?.ForEach(x => x.Value.Dispose());
            SequencerFlagDetailViewModels?.Clear();

            OriginalSnapshot = null;

            base.Dispose();
        }
    }
}