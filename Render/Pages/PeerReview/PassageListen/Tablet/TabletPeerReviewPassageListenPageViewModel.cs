using System.Collections.ObjectModel;
using System.Reactive.Linq;
using DynamicData;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Render.Components.BarPlayer;
using Render.Extensions;
using Render.Kernel;
using Render.Kernel.WrappersAndExtensions;
using Render.Models.Sections;
using Render.Models.Workflow;
using Render.Models.Workflow.Stage;
using Render.Resources;
using Render.Resources.Localization;
using Render.Sequencer.Contracts.Enums;
using Render.Sequencer.Contracts.Interfaces;

namespace Render.Pages.PeerReview.PassageListen.Tablet
{
    public class TabletPeerReviewPassageListenPageViewModel : WorkflowPageBaseViewModel
    {
        private readonly SourceList<IBarPlayerViewModel> _barPlayers = new();
        private ReadOnlyObservableCollection<IBarPlayerViewModel> _passageReferences;
        public ReadOnlyObservableCollection<IBarPlayerViewModel> PassageReferences => _passageReferences;
        private readonly bool _requireNoteListen;

        [Reactive] public ISequencerPlayerViewModel SequencerPlayerViewModel { get; private set; }
        private ActionViewModelBase SequencerActionViewModel { get; set; }
        private Passage Passage { get; set; }
        
        private ConversationService _conversationService;

        private TabletPeerReviewPassageListenPageViewModel(
            IViewModelContextProvider viewModelContextProvider,
            Section section,
            Passage passage,
            Step step,
            Stage stage)
            : base(urlPathSegment: "TabletPeerReviewPassageList enPage",
                viewModelContextProvider: viewModelContextProvider,
                pageName: AppResources.PeerCheck,
                section: section,
                stage: stage,
                step: step,
                passageNumber: passage.PassageNumber,
                secondPageName: AppResources.PassageListen)
        {
            DisposeOnNavigationCleared = true;
            TitleBarViewModel.DisposeOnNavigationCleared = true;
            Passage = passage;
            _requireNoteListen = step.StepSettings.GetSetting(SettingType.RequireNoteListen);
        }

        public static async Task<TabletPeerReviewPassageListenPageViewModel> CreateAsync(
            IViewModelContextProvider viewModelContextProvider,
            Section section,
            Passage passage,
            Step step,
            Stage stage)
        {
            // WorkflowPageBaseViewModel base constructor initializes SessionStateService and should be invoked at the beginning
            var pageVm = new TabletPeerReviewPassageListenPageViewModel(viewModelContextProvider, section, passage, step, stage);

            await pageVm.Initialize();
            return pageVm;
        }

        private async Task Initialize()
        {
            TitleBarViewModel.PageGlyph = ((FontImageSource)ResourceExtensions.GetResourceValue("PeerCheckWhite"))?.Glyph;

            //Reference bar players 
            var changeList = _barPlayers.Connect().Publish();
            Disposables.Add(changeList
                .Bind(out _passageReferences)
                .Subscribe());
            Disposables.Add(changeList.Connect());

            if (Section.References != null)
            {
                PopulateReferences();
            }
            
            await SetupSequencer();

            ProceedButtonViewModel.SetCommand(NavigateToNextPageAsync);
            Disposables.Add(ProceedButtonViewModel.NavigateToPageCommand.IsExecuting
                .Subscribe(isExecuting => { IsLoading = isExecuting; }));
            SetProceedButtonIcon();
        }

        protected sealed override void SetProceedButtonIcon()
        {
            var nextPassage = Section.GetNextPassage(Passage);
            if (nextPassage == null && Section.Passages.All(x => x.HasAudio))
            {
                ProceedButtonViewModel.IsCheckMarkIcon = true;
            }
        }

        private void PopulateReferences()
        {
            var referenceCount = 0;
            var passageNumber = Passage.PassageNumber.Number;
            foreach (var referenceAudio in Section.References.Where(x => !x.LockedReferenceByPassageNumbersList.Contains(passageNumber)))
            {
                var isRequired = referenceAudio.Reference.Primary && Step.StepSettings.GetSetting(SettingType.RequirePassageReview);
                var timeMarkers = referenceAudio.PassageReferences.FirstOrDefault(x =>
                    x.PassageNumber.Equals(Passage.PassageNumber))?.TimeMarkers;
                var barPlayer = ViewModelContextProvider.GetBarPlayerViewModel(referenceAudio,
                    isRequired ? ActionState.Required : ActionState.Optional,
                    referenceAudio.Reference.Name,
                    referenceCount++,
                    timeMarkers);
                _barPlayers.Add(barPlayer);
                ActionViewModelBaseSourceList.Add(barPlayer);
            }
        }

        private async Task SetupSequencer()
        {
            var path = ViewModelContextProvider
                .GetTempAudioService(Passage.CurrentDraftAudio)
                .SaveTempAudio();

            var flagType = FlagType.Note;

            SequencerPlayerViewModel = ViewModelContextProvider
                .GetSequencerFactory()
                .CreatePlayer(ViewModelContextProvider.GetAudioPlayer, flagType);

            SequencerPlayerViewModel.IsRightToLeftDirection = FlowDirection is FlowDirection.RightToLeft;
            
            var snapshots = await ViewModelContextProvider.GetSnapshotRepository().GetSnapshotsForSectionAsync(Section.Id);
            
            _conversationService = new ConversationService(
                this,
                Disposables,
                Stage,
                Step,
                SequencerPlayerViewModel);
            
            _conversationService.TapFlagPostEvent = ProcessStateStatusChange;
            _conversationService.DefineFlagsToDraw(snapshots, Stage.Id);

            SequencerPlayerViewModel.SetAudio(new[] {
                Passage.CreatePlayerAudioModel(
                _conversationService.GetConversations(Passage.CurrentDraftAudio),
                path: path,
                name: string.Format(AppResources.Passage, Passage.PassageNumber.PassageNumberString),
                startIcon: Icon.PassageNew.ToString(),
                endIcon: null,
                option: AudioOption.Optional,
                flagType: flagType) });
            
            SequencerPlayerViewModel.SetupActivityService(ViewModelContextProvider, Disposables);
            
            _conversationService.SequencerAudios = new List<Draft>() { Passage.CurrentDraftAudio };
            _conversationService.InitializeNoteDetail(_requireNoteListen);

            SequencerActionViewModel = SequencerPlayerViewModel.CreateActionViewModel(
                required: Step.StepSettings.GetSetting(SettingType.RequirePassageReview),
                requirementId: Passage.CurrentDraftAudio.Id,
                provider: ViewModelContextProvider,
                disposables: Disposables);
            ActionViewModelBaseSourceList.Add(SequencerActionViewModel);

            Disposables.Add(SequencerPlayerViewModel.WhenAnyValue(vm => vm.State)
                .Where(state => state == SequencerState.Playing)
                .Subscribe(_ => SequencerActionViewModel.ActionState = ActionState.Optional));
        }

        private async Task<IRoutableViewModel> NavigateToNextPageAsync()
        {
            var nextPassageIndex = Section.Passages.IndexOf(Passage) + 1;
            if (nextPassageIndex >= Section.Passages.Count)
            {
                await Task.Run(async () => { await ViewModelContextProvider.GetGrandCentralStation().AdvanceSectionAfterReviewAsync(Section, Step); });
                return await NavigateToHomeOnMainStackAsync();
            }

            var vm = await Task.Run(async () => await CreateAsync(ViewModelContextProvider,
                Section, Section.Passages[nextPassageIndex], Step, Stage));
            return await NavigateTo(vm);
        }

        public override void Dispose()
        {
            Passage = null;
            _barPlayers.DisposeSourceList();

            SequencerPlayerViewModel?.Dispose();
            SequencerPlayerViewModel = null;
            SequencerActionViewModel?.Dispose();
            SequencerActionViewModel = null;

            _conversationService.Dispose();
            _conversationService = null;

            base.Dispose();
        }
    }
}