using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;
using DynamicData;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Render.Components.BarPlayer;
using Render.Kernel;
using Render.Models.Audio;
using Render.Models.Sections;
using Render.Models.Workflow;
using Render.Resources;
using Render.Repositories.SectionRepository;
using Render.Repositories.UserRepositories;
using Render.Resources.Localization;
using Render.Repositories.Extensions;

namespace Render.Pages.Settings.SectionStatus.Processes
{
    public class SectionStatusProcessesViewModel : ViewModelBase
    {
        [Reactive] public ObservableCollection<StageCardViewModel> StageCards { get; private set; }

        [Reactive]
        public SectionCollectionAsStageCardViewModel UnassignedSectionCollectionAsStageCardViewModel { get; set; }

        [Reactive]
        public SectionCollectionAsStageCardViewModel ApprovedSectionCollectionAsStageCardViewModel { get; set; }

        public ReactiveCommand<Unit, Unit> CloseInfoCommand { get; set; }

        [Reactive] public bool ShowSectionInformation { get; set; }
        [Reactive] public Section Section { get; private set; }
        [Reactive] public string TotalPassages { get; private set; }
        [Reactive] public string Reference { get; private set; }
        [Reactive] public string TotalVerses { get; private set; }
        [Reactive] public string CurrentStep { get; private set; }
        [Reactive] public string CurrentUsername { get; private set; }
        [Reactive] public string DraftedBy { get; private set; }

        [Reactive] public string ApprovedBy { get; private set; }

        [Reactive] public bool ShowProcessView { get; set; }
        [Reactive] public bool SectionSelected { get; set; }

        [Reactive] public bool IsNotAssignedAnything { get; private set; }

        private Draft _latestDraft;

        //Section Audio
        private SourceList<IBarPlayerViewModel> _sectionAudioSourceList { get; } = new
            SourceList<IBarPlayerViewModel>();

        public ReadOnlyObservableCollection<IBarPlayerViewModel> SectionAudioPlayers => _sectionAudioPlayersViewModels;
        private readonly ReadOnlyObservableCollection<IBarPlayerViewModel> _sectionAudioPlayersViewModels;
        private readonly ISectionRepository _sectionRepository;
        private readonly IUserRepository _userRepository;

        public static async Task<SectionStatusProcessesViewModel> CreateAsync(List<Section> sections,
            IViewModelContextProvider viewModelContextProvider, IScreen screen = null)
        {
            var vm = new SectionStatusProcessesViewModel(viewModelContextProvider);
            await vm.InitializeStageCard(sections, viewModelContextProvider);
            return vm;
        }

        public SectionStatusProcessesViewModel(IViewModelContextProvider viewModelContextProvider, IScreen screen = null)
            : base("SectionStatusProcessesView", viewModelContextProvider, screen)
        {
            _sectionRepository = viewModelContextProvider.GetSectionRepository();
            _userRepository = viewModelContextProvider.GetUserRepository();
            //section audio bar players             
            var sectionAudioChangeList = _sectionAudioSourceList.Connect().Publish();
            Disposables.Add(sectionAudioChangeList
				.ObserveOn(RxApp.MainThreadScheduler)
				.Bind(out _sectionAudioPlayersViewModels)
                .Subscribe());
            Disposables.Add(sectionAudioChangeList.Connect());
        }

        public async Task InitializeStageCard(List<Section> sections, IViewModelContextProvider viewModelContextProvider)
        {
            var data = viewModelContextProvider.GetGrandCentralStation().GetProcessesData();
            var stageCards = new List<StageCardViewModel>();
            var assignedSections = new List<Section>();

            foreach (var workflowPair in data)
            {
                var assignments = workflowPair.Key.AllSectionAssignments;
                foreach (var stagePair in workflowPair.Value)
                {
                    var stepCards = new List<StepCardViewModel>();
                    var movedToNextStepSectionsIds = new List<Guid>();
                    foreach (var stepPair in stagePair.Value)
                    {
                        var isCurrentStepNotActive = !stepPair.Key.StepSettings.GetSetting(SettingType.IsActive);
						//If the step is off, don't add a view model for it
						if (isCurrentStepNotActive)
                        {
							//If the step is off and there are sections in it, move sections
							if (stepPair.Value.Any())
                            {
								movedToNextStepSectionsIds.AddRange(stepPair.Value);
							}

							continue;
						}

                        if (!movedToNextStepSectionsIds.IsNullOrEmpty())
                        {
                            stepPair.Value.AddRange(movedToNextStepSectionsIds);
                            movedToNextStepSectionsIds.Clear();
                        }

						var sectionCards = new List<SectionCardViewModel>();
                        var sectionIds = assignments.Where(x => stepPair.Value.Contains(x.SectionId))
                            .OrderBy(x => x.Priority).Select(x => x.SectionId).ToList();
                        foreach (var sectionId in sectionIds)
                        {
                            var section = sections.SingleOrDefault(x => x.Id == sectionId);
                            if (section != null)
                            {
                                assignedSections.Add(section);
                                sectionCards.Add(new SectionCardViewModel(section, viewModelContextProvider,
                                    SelectSectionCardAsync));
                            }
                        }

                        //If step does not have sections add section card without section for the empty section 
                        if (!sectionCards.Any())
                        {
                            sectionCards.Add(new SectionCardViewModel(null, viewModelContextProvider,
                                SelectSectionCardAsync));
                        }

                        stepCards.Add(new StepCardViewModel(stepPair.Key, sectionCards, viewModelContextProvider,
                            stagePair.Key.Id));

                        var lastSection = sectionCards.LastOrDefault();
                        if (lastSection != null) lastSection.IsLastSectionCard = true;

                        foreach (var sectionCard in sectionCards)
                        {
                            if (sectionCard.Section is null) continue;
                            sectionCard.HasConflict = await ViewModelContextProvider.GetGrandCentralStation()
                                .CheckForSnapshotConflicts(sectionCard.Section.Id);
                        }
                    }

                    var lastStepCard = stepCards.LastOrDefault();
                    if (lastStepCard != null) lastStepCard.LastStepCard = true;
                    stageCards.Add(new StageCardViewModel(stagePair.Key, stepCards, viewModelContextProvider));
                }
            }

            var anySectionAssignedToTeam = data.Keys.Any(workflow => workflow.AllSectionAssignments.Any());
            IsNotAssignedAnything = anySectionAssignedToTeam is false;
            var unassignedSections = sections.Except(assignedSections).ToList();
            var approvedSections = unassignedSections.Where(section => section.ApprovedBy != Guid.Empty).ToList();
            unassignedSections = unassignedSections.Except(approvedSections).ToList();

            UnassignedSectionCollectionAsStageCardViewModel =
                new SectionCollectionAsStageCardViewModel(unassignedSections, SelectSectionCardAsync,
                    Icon.AssignSections,
                    AppResources.Unassigned,
                    viewModelContextProvider);

            ApprovedSectionCollectionAsStageCardViewModel =
                new SectionCollectionAsStageCardViewModel(approvedSections, SelectSectionCardAsync,
                    Icon.FinishedPassOrSubmit,
                    AppResources.Approved,
                    viewModelContextProvider);

            StageCards = new ObservableCollection<StageCardViewModel>(stageCards);
            CloseInfoCommand = ReactiveCommand.CreateFromTask(CloseInformationAsync);
        }

        public async Task SelectSectionCardAsync(Guid clickedSectionId)
        {
            IsLoading = true;

            ViewModelContextProvider.GetAudioActivityService().Stop();
            
            foreach (var stageCard in StageCards)
            {
                foreach (var stepCard in stageCard.StepCards)
                {
                    foreach (var sectionCard in stepCard.SectionCards)
                    {
                        await SelectSectionCardAsync(clickedSectionId, stageCard, stepCard, sectionCard);
                    }
                }
            }

            foreach (var sectionCard in UnassignedSectionCollectionAsStageCardViewModel.Sections)
            {
                await SelectSectionCardAsync(clickedSectionId, null, null, sectionCard);
            }

            foreach (var sectionCard in ApprovedSectionCollectionAsStageCardViewModel.Sections)
            {
                await SelectSectionCardAsync(clickedSectionId, null, null, sectionCard, true);
            }

            IsLoading = false;
        }

        private async Task SelectSectionCardAsync(Guid clickedSectionId, StageCardViewModel stageCard,
            StepCardViewModel stepCard, SectionCardViewModel sectionCard, bool isApproved = false)
        {
            SectionSelected = true;
            if (sectionCard.Section != null && sectionCard.Section.Id == clickedSectionId)
            {
                if (sectionCard.IsSelected)
                {
                    sectionCard.IsSelected = false;
                    ShowSectionInformation = false;
                }
                else
                {
                    sectionCard.IsSelected = true;
                    Section = sectionCard.Section;
                    TotalPassages = sectionCard.Section.Passages.Count.ToString();
                    Reference = sectionCard.Section.ScriptureReference;
                    TotalVerses = sectionCard.Section.Passages
                        .SelectMany(passage => passage.ScriptureReferences)
                        .Distinct()
                        .Sum(scriptureReference => scriptureReference.EndVerse - scriptureReference.StartVerse + 1)
                        .ToString();

                    await Task.Run(RetrieveAudiosForSection);

                    if (stageCard != null && stepCard != null)
                    {
                        CurrentStep = stepCard.StepName;
                        var workflow = ViewModelContextProvider.GetGrandCentralStation().ProjectWorkflow;
                        var team = workflow.GetTeams().SingleOrDefault(x =>
                            x.SectionAssignments.Any(y => y.SectionId == clickedSectionId));
                        var teamAssignment = team?.WorkflowAssignments.SingleOrDefault(y =>
                            y.StageId == stageCard.Stage.Id && y.Role == stepCard.Step.Role);
                        if (stageCard.Stage.StageType == StageTypes.Drafting || stepCard.Step.Role == Roles.Drafting)
                        {
                            CurrentUsername = team == null || team.TranslatorId == Guid.Empty
                                ? string.Empty
                                : (await GetUserNameAsync(team.TranslatorId));
                        }
                        else
                        {
                            CurrentUsername = teamAssignment == null
                                ? string.Empty
                                : (await GetUserNameAsync(teamAssignment.UserId));
                        }

                        DraftedBy = stageCard.Stage.StageType == StageTypes.Drafting
                            ? string.Empty
                            : await GetDraftedBy(team);
                        ApprovedBy = string.Empty;
                    }
                    else if (isApproved)
                    {
                        CurrentStep = null;
                        CurrentUsername = null;
                        var workflow = ViewModelContextProvider.GetGrandCentralStation().ProjectWorkflow;
                        var team = workflow.GetTeams().SingleOrDefault(x =>
                            x.SectionAssignments.Any(y => y.SectionId == clickedSectionId));
                        DraftedBy = await GetDraftedBy(team);
                        ApprovedBy = await GetUserNameAsync(Section.ApprovedBy);
                    }
                    else
                    {
                        //not specified for unassigned section
                        CurrentStep = null;
                        CurrentUsername = null;
                        DraftedBy = null;
                        ApprovedBy = null;
                    }

                    ShowSectionInformation = true;
                }
            }
            else
            {
                sectionCard.IsSelected = false;
            }

            SectionSelected = false;
        }

        private async Task RetrieveAudiosForSection()
        {
            foreach (var player in _sectionAudioSourceList.Items)
            {
                player.Dispose();
            }
            _sectionAudioSourceList.Clear();
            
            var section = await _sectionRepository.GetSectionWithDraftsAsync(Section.Id, true, true);
            IBarPlayerViewModel playerVM;
            
            if (section != null && section.Passages.Any(x => x.HasAudio))
            {
                playerVM = ViewModelContextProvider.GetBarPlayerViewModel(
                    new AudioPlayback(section.Id, section.Passages.Select(x => x.CurrentDraftAudio)),
                    ActionState.Optional,
                    AppResources.Draft,
                    0);
                _sectionAudioSourceList.Add(playerVM);
                _latestDraft = section.Passages.Last().CurrentDraftAudio;
            }
            if (section.CheckSegmentAudio())
            {
                playerVM = ViewModelContextProvider.GetBarPlayerViewModel(
                    new AudioPlayback(Guid.NewGuid(), section.GetBackTranslationSegmentAudios()),
                    ActionState.Optional,
                    AppResources.SegmentBackTranslate1,
                    0);
                _sectionAudioSourceList.Add(playerVM);
            }
            if (section.CheckSecondStepSegmentAudio())
            {
                playerVM = ViewModelContextProvider.GetBarPlayerViewModel(
                    new AudioPlayback(Guid.NewGuid(), section.GetSecondStepBackTranslationSegmentAudios()),
                    ActionState.Optional,
                    AppResources.SegmentBackTranslate2,
                    0);
                _sectionAudioSourceList.Add(playerVM);
            }
            if (section.CheckRetellAudio())
            {
                playerVM = ViewModelContextProvider.GetBarPlayerViewModel(
                    new AudioPlayback(Guid.NewGuid(), section.GetBackTranslationRetellAudios()),
                    ActionState.Optional,
                    AppResources.PassageBackTranslate1,
                    0);
                _sectionAudioSourceList.Add(playerVM);
            }
            if (section.CheckSecondStepRetellAudio())
            {
                playerVM = ViewModelContextProvider.GetBarPlayerViewModel(
                    new AudioPlayback(Guid.NewGuid(), section.GetSecondStepBackTranslationRetellAudios()),
                        ActionState.Optional,
                        AppResources.PassageBackTranslate2,
                        0);
                _sectionAudioSourceList.Add(playerVM);
            }
        }

        private async Task CloseInformationAsync()
        {
            //Deselect all sections
            await SelectSectionCardAsync(Guid.Empty);
            ShowSectionInformation = false;
        }

        private async Task<string> GetUserNameAsync(Guid userId)
        {
            var user = await _userRepository.GetUserAsync(userId);
            return user != null ? user.FullName : string.Empty;
        }

        private async Task<string> GetDraftedBy(Team team)
        {
            if (team == null || _latestDraft == null) return string.Empty;

            var userName = await GetUserNameAsync(_latestDraft.CreatedById);
            return string.IsNullOrEmpty(userName) ? _latestDraft.CreatedByName : userName;
        }

        public override void Dispose()
        {
            foreach (var stageCardViewModel in StageCards)
            {
                stageCardViewModel.Dispose();
            }
            StageCards?.Clear();

            foreach (var player in _sectionAudioSourceList.Items)
            {
                player.Dispose();
            }
            _sectionAudioSourceList.Dispose();

            // _sectionAudioPlayersViewModels bound to Disposables

            CloseInfoCommand?.Dispose();
            CloseInfoCommand = null;

            UnassignedSectionCollectionAsStageCardViewModel?.Dispose();
            UnassignedSectionCollectionAsStageCardViewModel = null;

            ApprovedSectionCollectionAsStageCardViewModel?.Dispose();
            ApprovedSectionCollectionAsStageCardViewModel = null;

            _sectionRepository.Dispose();

            Section = null;

            base.Dispose();
        }
    }
}