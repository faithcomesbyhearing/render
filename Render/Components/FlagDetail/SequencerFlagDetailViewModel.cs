using DynamicData;
using ReactiveUI;
using Render.Components.CommunityTestFlagLibraryModal;
using Render.Components.Navigation;
using Render.Kernel;
using Render.Models.Sections;
using Render.Models.Sections.CommunityCheck;
using Render.Models.Workflow.Stage;
using Render.Sequencer.Contracts.Enums;
using Render.Sequencer.Contracts.Interfaces;
using Render.Services;
using Render.Services.SessionStateServices;
using Render.TempFromVessel.Kernel;
using FlagState = Render.Components.NotePlacementPlayer.FlagState;

namespace Render.Components.FlagDetail;

public class SequencerFlagDetailViewModel : IDisposable
{
    private readonly bool _requireCommunityFeedback;

    private List<CommunityTestFlagMarkerViewModel> CommunityTestMarkersViewModels { get; } = new();
    public IReadOnlyList<CommunityTestFlagMarkerViewModel> CommunityTestFlagMarkers => CommunityTestMarkersViewModels.AsReadOnly();
    public CommunityTestForStage CommunityTestForStages { get; private set; }

    private ISequencerViewModel SequencerPlayerViewModel { get; }
    private IViewModelContextProvider ViewModelContextProvider { get; }
    private SourceList<IActionViewModelBase> ActionViewModelBaseSourceList { get; }
    private List<IDisposable> Disposables { get; }
    private readonly ISessionStateService _sessionStateService;

    private Section _section;
    private Passage _passage;
    private Stage _stage;
    private CommunityTest _communityTest;

    public SequencerFlagDetailViewModel(
        Section section,
        Passage passage,
        Stage stage,
        ISequencerViewModel sequencerViewModel,
        IViewModelContextProvider provider,
        SourceList<IActionViewModelBase> actionList,
        List<IDisposable> disposables,
        bool requireCommunityFeedback = false)
    {
        _section = section;
        _passage = passage;
        _stage = stage;
        _communityTest = passage.CurrentDraftAudio.GetCommunityCheck();
        SequencerPlayerViewModel = sequencerViewModel;
        ActionViewModelBaseSourceList = actionList;
        ViewModelContextProvider = provider;
        Disposables = disposables;
        _requireCommunityFeedback = requireCommunityFeedback;

        _sessionStateService = provider.GetSessionStateService();
        var workflowStages = provider.GetGrandCentralStation().ProjectWorkflow.GetAllStages(includeDeactivatedStages:true);
        var communityTestService = provider.GetCommunityTestService();
        CommunityTestForStages = communityTestService.GetCommunityTestForStage(_stage, _communityTest, workflowStages);

        InitializeMarkers();
    }

    private void InitializeMarkers()
    {
        var flagMarkerViewModels = _communityTest.GetFlags(_stage.Id)
            .Select(CreateFlagMarker).ToList();

		if (CommunityTestForStages.Flags.Any() == false && CommunityTestForStages.Retells.Any() || CommunityTestForStages.Responses.Any())
		{
			var flagNearStart = new Flag(0);
			var communityTestFlagMarker = CreateFlagMarker(flagNearStart);
			CommunityTestMarkersViewModels.Add(communityTestFlagMarker);
			ActionViewModelBaseSourceList.Add(communityTestFlagMarker);
			return;
		}

		foreach (var flag in CommunityTestForStages.Flags)
		{
			var existingFlag = flagMarkerViewModels.FirstOrDefault(x => x.Flag.Id == flag.Id);
			if (existingFlag is null)
			{
				var communityTestFlagMarker = CreateFlagMarker(flag);
				flagMarkerViewModels.Add(communityTestFlagMarker);
			}
		}

		CommunityTestMarkersViewModels.AddRange(flagMarkerViewModels.OrderBy(x => x.Flag.TimeMarker));
        ActionViewModelBaseSourceList.AddRange(CommunityTestMarkersViewModels);
    }

    private CommunityTestFlagMarkerViewModel CreateFlagMarker(Flag flag)
    {
        var communityTestFlagMarker = new CommunityTestFlagMarkerViewModel(ViewModelContextProvider,
            flag, retellsCount: CommunityTestForStages.Retells.Count + CommunityTestForStages.Responses.Count);

        var disposable = communityTestFlagMarker
            .WhenAnyValue(marker => marker.FlagState)
            .Subscribe(state =>
            {
                var sequencerFlag = SequencerPlayerViewModel.GetFlag(flag.Id);
                if (sequencerFlag is null)
                {
                    return;
                }

                switch (state)
                {
                    case FlagState.Optional:
                        sequencerFlag.State = Sequencer.Contracts.Enums.FlagState.Unread;
                        sequencerFlag.Option = ItemOption.Optional;
                        break;
                    case FlagState.Viewed:
                        sequencerFlag.State = Sequencer.Contracts.Enums.FlagState.Read;
                        sequencerFlag.Option = ItemOption.Optional;
                        break;
                    case FlagState.Required:
                        sequencerFlag.State = Sequencer.Contracts.Enums.FlagState.Unread;
                        sequencerFlag.Option = ItemOption.Required;
                        break;
                }
            });

        Disposables.Add(disposable);

        SetRequirementInFlags(communityTestFlagMarker);

        return communityTestFlagMarker;
    }

    private void SetRequirementInFlags(CommunityTestFlagMarkerViewModel flagMarker)
    {
        if (_requireCommunityFeedback)
        {
            var retellRequired = CommunityTestForStages?.Retells.Any(retell => _sessionStateService.RequirementMetInSession(retell.Id) == false) ?? false;
            if (retellRequired)
            {
                SetFlagState(flagMarker, FlagState.Required);
                return;
            }

            var flag = flagMarker.Flag;

            var questionRequired = flag.Questions
                .Any(question => _sessionStateService.RequirementMetInSession(question.QuestionAudio.Id) == false);
            if (questionRequired)
            {
                SetFlagState(flagMarker, FlagState.Required);
                return;
            }

            var responseRequired = flag.Questions
                .SelectMany(question => question.Responses)
                .Any(response => _sessionStateService.RequirementMetInSession(response.Id) == false);
            if (responseRequired)
            {
                SetFlagState(flagMarker, FlagState.Required);
                return;
            }
        }

        SetFlagState(flagMarker, FlagState.Viewed);
    }

    private static void SetFlagState(CommunityTestFlagMarkerViewModel flagMarker, FlagState state)
    {
        flagMarker.FlagState = state;
    }

    public async Task ShowFlagMarkerAsync(IFlag flag)
    {
        var marker = CommunityTestMarkersViewModels.FirstOrDefault(x => x.Flag.Id == flag.Key);
        if (marker is null) return;

        using var flagDetailViewModel = await FlagDetailViewModel.CreateAsync(
            marker.Flag,
            _passage,
            CommunityTestForStages,
            _section.Number,
            _requireCommunityFeedback,
            ViewModelContextProvider);
        
        var navigationViewModel = new ItemDetailNavigationViewModel(
            item: marker.Flag,
            CommunityTestMarkersViewModels,
            onChangeItemCommand: ReactiveCommand.Create<DomainEntity>(_ =>
            {
                CheckMarkerState(flagDetailViewModel.Flag.Id);
            }),
            ViewModelContextProvider);

        flagDetailViewModel.OnCloseCommand = ReactiveCommand.Create(() => { CheckMarkerState(navigationViewModel.CurrentItem.Id); });

        flagDetailViewModel.SetNavigation(navigationViewModel);
        
        await flagDetailViewModel.ShowPopupAsync();
    }

    private void CheckMarkerState(Guid flagId)
    {
        var communityTestMarker = CommunityTestMarkersViewModels.FirstOrDefault(x => x.Flag.Id == flagId);
        if (communityTestMarker is not null)
        {
            SetRequirementInFlags(communityTestMarker);
        }
    }

    public void Dispose()
    {
        _section = null;
        _passage = null;
        _stage = null;
        _communityTest = null;
        CommunityTestForStages = null;

        CommunityTestMarkersViewModels.ForEach(x => x.Dispose());
        CommunityTestMarkersViewModels.Clear();
    }
}