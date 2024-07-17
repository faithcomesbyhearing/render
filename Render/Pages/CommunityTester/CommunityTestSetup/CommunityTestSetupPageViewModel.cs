using System.Reactive.Linq;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Render.Components.CommunityTestFlagLibraryModal;
using Render.Extensions;
using Render.Kernel;
using Render.Kernel.WrappersAndExtensions;
using Render.Models.Sections;
using Render.Models.Sections.CommunityCheck;
using Render.Models.Workflow;
using Render.Models.Workflow.Stage;
using Render.Resources;
using Render.Resources.Localization;
using Render.Sequencer.Contracts.Enums;
using Render.Sequencer.Contracts.Interfaces;
using Render.Sequencer.Contracts.Models;

namespace Render.Pages.CommunityTester.CommunityTestSetup
{
    public class CommunityTestSetupPageViewModel : WorkflowPageBaseViewModel
    {
        private CommunityTest _communityTest;
        private Section _section;
        private Passage _passage;
        private readonly Guid _stepId;
        private readonly Stage _stage;
        [Reactive] public ISequencerPlayerViewModel SequencerPlayerViewModel { get; private set; }
        private DynamicDataWrapper<CommunityTestFlagMarkerViewModel> CommunityTestMarkers { get; set; }

        private readonly ObservableAsPropertyHelper<bool> _aQuestionHasBeenRecorded;
        private bool AQuestionHasBeenRecorded => _aQuestionHasBeenRecorded.Value;

        private readonly List<IMarkerFlag> _markerFlags = new();
        
        public static CommunityTestSetupPageViewModel Create(IViewModelContextProvider viewModelContextProvider,
            Section section, Passage passage, Step step, Stage stage)
        {
            return new CommunityTestSetupPageViewModel(viewModelContextProvider, section, passage, step, stage);
        }

        private CommunityTestSetupPageViewModel(IViewModelContextProvider viewModelContextProvider,
            Section section, Passage passage, Step step, Stage stage)
            : base("CommunityTestSetupPage",
                viewModelContextProvider,
                AppResources.CommunityTestSetup,
                section,
                stage,
                step,
                passage.PassageNumber,
                secondPageName: AppResources.QuestionPlacement)
        {
            DisposeOnNavigationCleared = true;
            TitleBarViewModel.DisposeOnNavigationCleared = true;

            _section = section;
            _passage = passage;
            _stepId = step.Id;
            _stage = stage;
            
            _communityTest = passage.CurrentDraftAudio.GetCommunityCheck();

            var color = ResourceExtensions.GetColor("SecondaryText");
            if (color != null)
            {
                TitleBarViewModel.PageGlyph = IconExtensions.BuildFontImageSource(Icon.CommunityCheckSetup, color)?.Glyph;
            }

            CommunityTestMarkers = new DynamicDataWrapper<CommunityTestFlagMarkerViewModel>(SortExpressionComparer<CommunityTestFlagMarkerViewModel>
                .Ascending(conversation => conversation.Flag.TimeMarker));
            
            ProceedButtonViewModel.SetCommand(NavigateToNextPassageOrHome);
            
            foreach (var flag in _communityTest.GetFlags(Stage.Id))
            {
                var conversationMarker = new CommunityTestFlagMarkerViewModel(ViewModelContextProvider, flag);
                CommunityTestMarkers.Add(conversationMarker);
            }
            
            Disposables.Add(CommunityTestMarkers.Observable.WhenPropertyChanged(x => x.Flag.QuestionCount)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Select(x => x.Sender.Flag)
                .Where(x => x.QuestionCount > 0 && _markerFlags.Any())
                .Subscribe(flag =>
                {
                    var marker = _markerFlags.FirstOrDefault(x => x.Key == flag.Id);
                    if (marker != null)
                    {
                        marker.Symbol = flag.QuestionCount.ToString();   
                    }
                }));
            
            _aQuestionHasBeenRecorded = CommunityTestMarkers.Observable.WhenPropertyChanged(x => x.Flag.QuestionCount)
                .Select(i => CommunityTestMarkers.SourceItems.Any(x => x.Flag.QuestionCount > 0))
                .ToProperty(this, x => x.AQuestionHasBeenRecorded);
            
            Disposables.Add(this.WhenAnyValue(x => x.AQuestionHasBeenRecorded)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(isActive => ProceedButtonViewModel.ProceedActive = isActive));
            
            Disposables.Add(ProceedButtonViewModel.NavigateToPageCommand.IsExecuting
                .Subscribe(isExecuting => { IsLoading = isExecuting; }));

            SetProceedButtonIcon();

            SetupSequencer();
        }

        protected sealed override void SetProceedButtonIcon()
        {
            var nextPassage = _section.GetNextPassage(_passage);
            if (nextPassage == null)
            {
                ProceedButtonViewModel.IsCheckMarkIcon = true;
            }
        }

        private async Task<IRoutableViewModel> NavigateToNextPassageOrHome()
        {
            var nextPassage = _section.GetNextPassage(_passage);

            if (nextPassage == null)
            {
                var gcc = ViewModelContextProvider.GetGrandCentralStation();
                var step = gcc.ProjectWorkflow?.GetStep(_stepId);

                await gcc.AdvanceSectionAsync(_section, step);

                return await NavigateToHomeOnMainStackAsync();
            }

            var nextSetup = Create(ViewModelContextProvider, _section, nextPassage, Step, _stage);

            return await NavigateTo(nextSetup);
        }

        private void SetupSequencer()
        {
            var path = ViewModelContextProvider
                .GetTempAudioService(_passage.CurrentDraftAudio)
                .SaveTempAudio();

            SequencerPlayerViewModel = ViewModelContextProvider
                .GetSequencerFactory()
                .CreatePlayer(ViewModelContextProvider.GetAudioPlayer, FlagType.Marker);

            SequencerPlayerViewModel.IsRightToLeftDirection = FlowDirection is FlowDirection.RightToLeft;

            SequencerPlayerViewModel.SetupActivityService(ViewModelContextProvider, Disposables);
            
            SequencerPlayerViewModel.AddFlagCommand = ReactiveCommand.CreateFromTask((IFlag flag) =>ShowConversationAsync((IMarkerFlag)flag));
            SequencerPlayerViewModel.TapFlagCommand = ReactiveCommand.CreateFromTask(
                async (IFlag flag) => { await ShowConversationAsync((IMarkerFlag)flag); });
            
            var markers = CommunityTestMarkers.SourceItems
                .Select(comTestMarker => new MarkerFlagModel(comTestMarker.Flag.Id,
                    comTestMarker.Flag.TimeMarker,
                    false,
                    symbol: comTestMarker.ItemsCount.ToString(),
                    true)).ToList();
            
            var audioPlayer = PlayerAudioModel.Create(
                path: path,
                name: string.Format(AppResources.Passage, _passage.PassageNumber.PassageNumberString),
                startIcon: Icon.PassageNew.ToString(),
                key: _passage.Id,
                flags: markers,
                number: _passage.PassageNumber.PassageNumberString);

            SequencerPlayerViewModel.SetAudio(new[] { audioPlayer });
            
            CommunityTestMarkers.SourceItems.ForEach(markerViewModel =>
            {
                var flag = SequencerPlayerViewModel.GetFlag(markerViewModel.Flag.Id);
                if (flag is null) return;

                _markerFlags.Add((IMarkerFlag)flag);
            });
        }

        private async Task<bool> ShowConversationAsync(IMarkerFlag flag)
        {
            var conversationCreationMode = flag.Key == default;
            
            var flagMarkerVm = conversationCreationMode
                ? AddFlag(flag)
                : CommunityTestMarkers.SourceItems.First(conv => conv.Flag.Id == flag.Key);
            
            if (conversationCreationMode)
            {
                flag.Key = flagMarkerVm.Flag.Id;
            }

            var communityTestLibraryModal = await CommunityTestFlagLibraryModalViewModel.CreateAsync(ViewModelContextProvider, flagMarkerVm,
                _section, _stage.Id,
                _communityTest, RemoveFlagWithoutQuestions);

            await communityTestLibraryModal.ShowPopupAsync();
            
            var marker = CommunityTestMarkers.SourceItems.FirstOrDefault(cm => cm.Flag.Id == flagMarkerVm.Flag.Id);
            if (marker is not null)
            {
                marker.FlagState = Components.NotePlacementPlayer.FlagState.Viewed;
            }
            
            return flagMarkerVm.Flag.Questions.Any();
        }
        
        private CommunityTestFlagMarkerViewModel AddFlag(IFlag flag)
        {
            _markerFlags.Add((IMarkerFlag)flag);
            
            var communityTestFlag = _communityTest.AddFlag(flag.PositionSec);
            var conversationMarker = new CommunityTestFlagMarkerViewModel(ViewModelContextProvider, communityTestFlag);
            CommunityTestMarkers.Add(conversationMarker);
            return conversationMarker;
        }
        
        private void RemoveFlagWithoutQuestions(CommunityTestFlagMarkerViewModel flagMarkerViewModel)
        {
			//We are interested in the flag questions for the current stage
			if (flagMarkerViewModel.Flag.Questions.Any(x => x.StageIds.Contains(_stage.Id)))
			{
				return;
			}

            RemoveNonExistingCustomFlags();
			//Only remove flag when all the questions does not have anymore stages ids attached to them
			if (flagMarkerViewModel.Flag.Questions.All(x => !x.StageIds.Any()))
			{
				_communityTest.RemoveFlag(flagMarkerViewModel.Flag);
			}
		}
        
        private void RemoveNonExistingCustomFlags()
        {
            var existingFlagIds = _communityTest.GetFlags(Stage.Id).Select(f => f.Id).ToList();

            var flagsToRemove = CommunityTestMarkers.SourceItems.Where(f => !existingFlagIds.Contains(f.Flag.Id))
                .ToList();

            foreach (var flag in flagsToRemove)
            {
                CommunityTestMarkers.Remove(flag);
                var sequencerFlag = SequencerPlayerViewModel.GetFlag(flag.Flag.Id);
                if (sequencerFlag is null) continue;
                
                _markerFlags.Remove((IMarkerFlag)sequencerFlag);
                SequencerPlayerViewModel.RemoveFlag(sequencerFlag);
            }
        }
        
        public override void Dispose()
        {
            _section = null;
            _passage = null;
            _communityTest = null;
            
            SequencerPlayerViewModel?.Dispose();
            SequencerPlayerViewModel = null;
            
            CommunityTestMarkers?.Dispose();
            CommunityTestMarkers?.Clear();
            _markerFlags.Clear();

            base.Dispose();
        }
    }
}