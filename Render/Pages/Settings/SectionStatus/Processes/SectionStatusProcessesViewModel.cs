using System.Collections.ObjectModel;
using System.Reactive;
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
using Render.Models.Workflow.Stage;
using Render.Components.MiniWaveformPlayer;
using Render.Models.Snapshot;
using Render.Services.SnapshotService;
using Render.Components.SectionInfo;
using Render.Services.StageService;
using Render.Services.WorkflowService;
using Render.Services.InterpretationService;

namespace Render.Pages.Settings.SectionStatus.Processes
{
    public class SectionStatusProcessesViewModel : ViewModelBase
    {
        private bool _isConflictOnDraftStage;
        private Draft _latestDraft;

        private readonly ISectionRepository _sectionRepository;
        private readonly IUserRepository _userRepository;
        private readonly IWorkflowService _workflowService;
        private readonly IStageService _stageService;
        private readonly ISnapshotService _snapshotService;
        private readonly IInterpretationService _interpretationService;

        public readonly List<SectionCardViewModel> SectionToExportList = [];

        [Reactive]
        public ObservableCollection<SectionInfoPlayerViewModel> SectionInfoPlayers { get; private set; } = [];

        [Reactive]
        public ObservableCollection<StageCardViewModel> StageCards { get; private set; }

        [Reactive]
        public SectionCollectionAsStageCardViewModel UnassignedSectionCollectionAsStageCardViewModel { get; set; }

        [Reactive]
        public SectionCollectionAsStageCardViewModel ApprovedSectionCollectionAsStageCardViewModel { get; set; }

        [Reactive]
        public bool ShowSectionInformation { get; set; }

        [Reactive]
        public Section Section { get; private set; }

        [Reactive]
        public string TotalPassages { get; private set; }

        [Reactive]
        public string Reference { get; private set; }

        [Reactive]
        public string TotalVerses { get; private set; }

        [Reactive]
        public string CurrentStep { get; private set; }

        [Reactive]
        public string CurrentUsername { get; private set; }

        [Reactive]
        public string DraftedBy { get; private set; }

        [Reactive]
        public string ApprovedBy { get; private set; }

        [Reactive]
        public bool ShowProcessView { get; set; }

        [Reactive]
        public bool SectionSelected { get; set; }

        [Reactive]
        public bool IsNotAssignedAnything { get; private set; }

        [Reactive]
        public bool HasConflict { get; set; }

        [Reactive]
        public bool AnySectionSelectedToExport { get; private set; }

        public ReactiveCommand<Unit, Unit> CloseInfoCommand { get; set; }

        public static async Task<SectionStatusProcessesViewModel> CreateAsync(
            List<Section> sections,
            IViewModelContextProvider viewModelContextProvider, 
            IScreen screen = null)
        {
            var vm = new SectionStatusProcessesViewModel(viewModelContextProvider);
            await vm.InitializeStageCard(sections, viewModelContextProvider);
            return vm;
        }

        public SectionStatusProcessesViewModel(
            IViewModelContextProvider viewModelContextProvider, 
            IScreen screen = null)
            : base("SectionStatusProcessesView", viewModelContextProvider, screen)
        {
            _sectionRepository = viewModelContextProvider.GetSectionRepository();
            _userRepository = viewModelContextProvider.GetUserRepository();
            _workflowService = viewModelContextProvider.GetWorkflowService();
            _stageService = viewModelContextProvider.GetStageService();
            _snapshotService = viewModelContextProvider.GetSnapshotService();
            _interpretationService = viewModelContextProvider.GetInterpretationService();
        }

        public async Task InitializeStageCard(List<Section> sections, IViewModelContextProvider viewModelContextProvider)
        {
            var data = await GetProcessesData();
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
                                sectionCards.Add(new SectionCardViewModel(
                                    section,
                                    viewModelContextProvider,
                                    SelectSectionCardAsync,
                                    SelectSectionToExport,
                                    stageFrom: stagePair.Key));
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
                            sectionCard.HasConflict = await _snapshotService.CheckForSnapshotConflicts(sectionCard.Section.Id);
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
                new SectionCollectionAsStageCardViewModel(unassignedSections, SelectSectionCardAsync, SelectSectionToExport,
                    Icon.AssignSections,
                    AppResources.Unassigned,
                    viewModelContextProvider);

            ApprovedSectionCollectionAsStageCardViewModel =
                new SectionCollectionAsStageCardViewModel(approvedSections, SelectSectionCardAsync, SelectSectionToExport,
                    Icon.FinishedPassOrSubmit,
                    AppResources.Approved,
                    viewModelContextProvider);

            StageCards = new ObservableCollection<StageCardViewModel>(stageCards);
            CloseInfoCommand = ReactiveCommand.CreateFromTask(CloseInformationAsync);
        }

        public async Task SelectSectionCardAsync(SectionCardViewModel clickedSectionCardViewModel)
        {
            IsLoading = true;

            ViewModelContextProvider.GetAudioActivityService().Stop();

            foreach (var stageCard in StageCards)
            {
                foreach (var stepCard in stageCard.StepCards)
                {
                    foreach (var sectionCard in stepCard.SectionCards)
                    {
                        await SelectSectionCardAsync(clickedSectionCardViewModel, sectionCard, isApproved: false, stageCard, stepCard);
                    }
                }
            }

            foreach (var sectionCard in UnassignedSectionCollectionAsStageCardViewModel.Sections)
            {
                await SelectSectionCardAsync(clickedSectionCardViewModel, sectionCard);
            }

            foreach (var sectionCard in ApprovedSectionCollectionAsStageCardViewModel.Sections)
            {
                await SelectSectionCardAsync(clickedSectionCardViewModel, sectionCard, isApproved: true);
            }

            IsLoading = false;
        }

        public void SelectSectionToExport(SectionCardViewModel clickedSectionCardViewModel)
        {
            foreach (var stageCard in StageCards)
            {
                foreach (var stepCard in stageCard.StepCards)
                {
                    foreach (var sectionCard in stepCard.SectionCards)
                    {
                        SelectSectionCardToExportAsync(clickedSectionCardViewModel, sectionCard);
                    }
                }
            }

            foreach (var sectionCard in UnassignedSectionCollectionAsStageCardViewModel.Sections)
            {
                SelectSectionCardToExportAsync(clickedSectionCardViewModel, sectionCard);
            }

            foreach (var sectionCard in ApprovedSectionCollectionAsStageCardViewModel.Sections)
            {
                SelectSectionCardToExportAsync(clickedSectionCardViewModel, sectionCard);
            }

            AnySectionSelectedToExport = SectionToExportList.Any();
        }

        private void SelectSectionCardToExportAsync(SectionCardViewModel clickedSectionCardViewModel, SectionCardViewModel observableSectionCard)
        {
            if (clickedSectionCardViewModel is null || clickedSectionCardViewModel.ViewModelId != observableSectionCard.ViewModelId)
            {
                return;
            }

            observableSectionCard.SelectedToExport = !observableSectionCard.SelectedToExport;

            if (observableSectionCard.SelectedToExport)
            {
                SectionToExportList.Add(observableSectionCard);
            }
            else
            {
                SectionToExportList.Remove(observableSectionCard);
            }
        }

        private async Task SelectSectionCardAsync(SectionCardViewModel clickedSectionCardViewModel, SectionCardViewModel observableSectionCard,
            bool isApproved = false, StageCardViewModel stageCard = null, StepCardViewModel stepCard = null)
        {
            if (clickedSectionCardViewModel is null || clickedSectionCardViewModel.ViewModelId != observableSectionCard.ViewModelId)
            {
                observableSectionCard.IsSelected = false;
                return;
            }

            SectionSelected = true;
            if (observableSectionCard.Section is not null)
            {
                if (observableSectionCard.IsSelected)
                {
                    observableSectionCard.IsSelected = false;
                    ShowSectionInformation = false;
                }
                else
                {
                    observableSectionCard.IsSelected = true;
                    Section = observableSectionCard.Section;
                    TotalPassages = observableSectionCard.Section.Passages.Count.ToString();
                    Reference = observableSectionCard.Section.ScriptureReference;
                    TotalVerses = observableSectionCard.Section.Passages
                        .SelectMany(passage => passage.ScriptureReferences)
                        .Distinct()
                        .Sum(scriptureReference => scriptureReference.EndVerse - scriptureReference.StartVerse + 1)
                        .ToString();

                    HasConflict = await _snapshotService.CheckForSnapshotConflicts(Section.Id);
                    var conflictedStage = await GetConflictedStage(Section.Id);
                    _isConflictOnDraftStage = HasConflict && conflictedStage.StageType == StageTypes.Drafting;

                    await RetrieveAudiosForSection();

                    if (stageCard != null && stepCard != null)
                    {
                        CurrentStep = stepCard.StepName;
                        var workflow = ViewModelContextProvider.GetWorkflowService().ProjectWorkflow;
                        var team = workflow.GetTeams().SingleOrDefault(x =>
                            x.SectionAssignments.Any(y => y.SectionId == observableSectionCard.Section.Id));
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
                            : await GetDraftedBy();
                        ApprovedBy = string.Empty;
                    }
                    else if (isApproved)
                    {
                        CurrentStep = null;
                        CurrentUsername = null;
                        DraftedBy = await GetDraftedBy();
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
                observableSectionCard.IsSelected = false;
            }

            SectionSelected = false;
        }

        private async Task<Stage> GetConflictedStage(Guid sectionId) => await ViewModelContextProvider.GetSnapshotService().GetConflictedStage(sectionId);

        private async Task RetrieveAudiosForSection()
        {
            foreach (var sectionInfoPlayerViewModel in SectionInfoPlayers)
            {
                sectionInfoPlayerViewModel?.Dispose();
            }
            SectionInfoPlayers.Clear();

            var section = await _sectionRepository.GetSectionWithDraftsAsync(Section.Id, true, true);

            if (section != null && section.Passages.Any(x => x.HasAudio) && !HasConflict)
            {
                InitializeBarPlayersWithSectionAudio(section);
            }

            if (HasConflict)
            {
                var lastTwoConflictedSnapshot = await _snapshotService.GetLastConflictedSnapshots(section.Id);

                foreach (var conflict in lastTwoConflictedSnapshot)
                {
                    InitializeBarPlayersWithSnapshotAudio(conflict.Snapshot, conflict.TranslatorName);
                }
            }
        }

        private void InitializeBarPlayersWithSectionAudio(Section section)
        {
            var draftPlayer = ViewModelContextProvider.GetMiniWaveformPlayerViewModel(
                    new AudioPlayback(section.Id, section.Passages.Select(x => x.CurrentDraftAudio)),
                    ActionState.Optional,
                    AppResources.Draft);

            var retellPlayers = GetRetellAudioPlayersFromSection(section);

            var sectionInfoPlayer = new SectionInfoPlayerViewModel(ViewModelContextProvider, draftPlayer, retellPlayers);
            SectionInfoPlayers.Add(sectionInfoPlayer);

            _latestDraft = section.Passages
                .Where(p => p.HasAudio && p.CurrentDraftAudio != null)
                .OrderBy(p => p.CurrentDraftAudio.Revision)
                .Last()
                .CurrentDraftAudio;
        }

        private void InitializeBarPlayersWithSnapshotAudio(Snapshot snapshot, string title)
        {
            var draftPlayer = GetDraftAudioPlayerFromSnapshot(snapshot, title);
            var retells = GetRetellAudioPlayersFromSnapshot(snapshot);

            var sectionInfoPlayer = new SectionInfoPlayerViewModel(ViewModelContextProvider, draftPlayer, retells);
            SectionInfoPlayers.Add(sectionInfoPlayer);
        }

        private List<IBarPlayerViewModel> GetRetellAudioPlayersFromSection(Section section)
        {
            IBarPlayerViewModel playerVM;
            var players = new List<IBarPlayerViewModel>();

            if (section.CheckRetellAudio())
            {
                playerVM = ViewModelContextProvider.GetBarPlayerViewModel(
                    new AudioPlayback(Guid.NewGuid(), section.GetBackTranslationRetellAudios()),
                    ActionState.Optional,
                    AppResources.PassageBackTranslate1,
                    0);
                players.Add(playerVM);
            }
            if (section.CheckSegmentAudio())
            {
                playerVM = ViewModelContextProvider.GetBarPlayerViewModel(
                    new AudioPlayback(Guid.NewGuid(), section.GetBackTranslationSegmentAudios()),
                    ActionState.Optional,
                    AppResources.SegmentBackTranslate1,
                    0);
                players.Add(playerVM);
            }
            if (section.CheckSecondStepRetellAudio())
            {
                playerVM = ViewModelContextProvider.GetBarPlayerViewModel(
                    new AudioPlayback(Guid.NewGuid(), section.GetSecondStepBackTranslationRetellAudios()),
                        ActionState.Optional,
                        AppResources.PassageBackTranslate2,
                        0);
                players.Add(playerVM);
            }

            if (section.CheckSecondStepSegmentAudio())
            {
                playerVM = ViewModelContextProvider.GetBarPlayerViewModel(
                    new AudioPlayback(Guid.NewGuid(), section.GetSecondStepBackTranslationSegmentAudios()),
                    ActionState.Optional,
                    AppResources.SegmentBackTranslate2,
                    0);
                players.Add(playerVM);
            }

            return players;
        }

        private IMiniWaveformPlayerViewModel GetDraftAudioPlayerFromSnapshot(Snapshot snapshot, string title)
        {
            var audio = snapshot.Passages.Select(x => x.CurrentDraftAudio);
            var player = ViewModelContextProvider.GetMiniWaveformPlayerViewModel(
                new AudioPlayback(snapshot.SectionId, audio),
                ActionState.Optional,
                title,
                glyph: IconExtensions.GetIconGlyph(Icon.InvalidInput));

            player.SetGlyphColor(ResourceExtensions.GetColor("Error"));

            return player;
        }

        private List<IBarPlayerViewModel> GetRetellAudioPlayersFromSnapshot(Snapshot conflictedSnapshot)
        {
            IBarPlayerViewModel playerVM;
            var players = new List<IBarPlayerViewModel>();

            if (conflictedSnapshot.CheckRetellAudio())
            {
                playerVM = ViewModelContextProvider.GetBarPlayerViewModel(
                    new AudioPlayback(Guid.NewGuid(), conflictedSnapshot.GetBackTranslationRetellAudios()),
                    ActionState.Optional,
                    AppResources.PassageBackTranslate1,
                    0);
                players.Add(playerVM);
            }
            if (conflictedSnapshot.CheckSegmentAudio())
            {
                playerVM = ViewModelContextProvider.GetBarPlayerViewModel(
                    new AudioPlayback(Guid.NewGuid(), conflictedSnapshot.GetBackTranslationSegmentAudios()),
                    ActionState.Optional,
                    AppResources.SegmentBackTranslate1,
                    0);
                players.Add(playerVM);
            }
            if (conflictedSnapshot.CheckSecondStepRetellAudio())
            {
                playerVM = ViewModelContextProvider.GetBarPlayerViewModel(
                    new AudioPlayback(Guid.NewGuid(), conflictedSnapshot.GetSecondStepBackTranslationRetellAudios()),
                        ActionState.Optional,
                        AppResources.PassageBackTranslate2,
                        0);
                players.Add(playerVM);
            }
            if (conflictedSnapshot.CheckSecondStepSegmentAudio())
            {
                playerVM = ViewModelContextProvider.GetBarPlayerViewModel(
                    new AudioPlayback(Guid.NewGuid(), conflictedSnapshot.GetSecondStepBackTranslationSegmentAudios()),
                    ActionState.Optional,
                    AppResources.SegmentBackTranslate2,
                    0);
                players.Add(playerVM);
            }

            return players;
        }

        private async Task CloseInformationAsync()
        {
            //Deselect all sections
            await SelectSectionCardAsync(null);
            ShowSectionInformation = false;
        }

        private async Task<string> GetUserNameAsync(Guid userId)
        {
            var user = await _userRepository.GetUserAsync(userId);
            if (_isConflictOnDraftStage && user is not null)
            {
                return string.IsNullOrEmpty(user.FullName) ? user.Username : user.FullName;
            }

            return user != null ? user.FullName : string.Empty;
        }

        private async Task<string> GetDraftedBy()
        {
            if (_isConflictOnDraftStage)
            {
                return await GetUserNamesForConflictedDrafts();
            }

            if (_latestDraft == null) return string.Empty;

            var userName = await GetUserNameAsync(_latestDraft.CreatedById);
            return string.IsNullOrEmpty(userName) ? _latestDraft.CreatedByName : userName;
        }

        private async Task<string> GetUserNamesForConflictedDrafts()
        {
            var usersName = new List<string>();
            var snapshots = await ViewModelContextProvider.GetSnapshotRepository().GetPermanentSnapshotsForSectionAsync(Section.Id);
            var snaphotsForDraftStage = snapshots.Where(x => x.StageName.Contains(Stage.DraftingDefaultStageName));

            foreach (var snapshot in snaphotsForDraftStage)
            {
                var userName = await GetUserNameAsync(snapshot.CreatedBy);
                usersName.Add(userName);
            }

            return string.Join(", ", usersName);
        }

        private async Task<Dictionary<RenderWorkflow, Dictionary<Stage, Dictionary<Step, List<Guid>>>>> GetProcessesData()
        {
            var data = new Dictionary<RenderWorkflow, Dictionary<Stage, Dictionary<Step, List<Guid>>>>();
            var sectionIdsFoundInOtherWorkflows = new List<Guid>();
            var enumerator = _workflowService.WorkflowStatusListByWorkflow.GetEnumerator();
            while (enumerator.MoveNext())
            {
                var sectionIdsFoundInThisWorkflow = new List<Guid>();
                var workflow = (RenderWorkflow)enumerator.Key;
                var statusList = (List<WorkflowStatus>)enumerator.Value;
                var stages = _stageService.GetAllStages(workflow);
                var stepsByStage = new Dictionary<Stage, Dictionary<Step, List<Guid>>>();
                foreach (var stage in stages)
                {
                    var sectionsByStep = new Dictionary<Step, List<Guid>>();
                    foreach (var step in stage.GetAllWorkflowEntrySteps(false))
                    {
                        sectionsByStep.Add(step, new List<Guid>());
                    }

                    stepsByStage.Add(stage, sectionsByStep);
                }

                foreach (var status in statusList?.Where(x => !x.IsCompleted))
                {
                    //If the section id is in the list, we already figured out where it's at in another workflow. Move along.
                    if (sectionIdsFoundInOtherWorkflows.Contains(status.ParentSectionId))
                    {
                        continue;
                    }

                    var step = workflow?.GetStep(status.CurrentStepId);
                    if (step is null)
                    {
                        continue;
                    }

                    //If all other status objects for this section are in the holding tank, it's ready to advance
                    if (_workflowService.AreAllWorkflowStatusObjectsForSectionAtHoldingTank(
                            step,
                            status.ParentSectionId,
                            statusList))
                    {
                        //Should only be one next step after a parallel step (by design)
                        var nextSteps = workflow.GetNextSteps(step.Id);
                        var needsInterpretation = await _interpretationService.IsNeedsInterpretation(
                            workflow,
                            status.ParentSectionId,
                            step,
                            nextSteps);
                        step = _stageService.CheckIfNextStepNeedsInterpretation(workflow, nextSteps, step.Id,
                            status.ParentSectionId, needsInterpretation).First();
                    }

                    //Uses the new step if value was re-assigned in previous if
                    if (step.RenderStepType != RenderStepTypes.HoldingTank)
                    {
                        sectionIdsFoundInThisWorkflow.Add(status.ParentSectionId);
                        var stage = stepsByStage
                            .SingleOrDefault(x => x.Key.Id == status.CurrentStageId).Key;
                        if (stage != null && stepsByStage.ContainsKey(stage) &&
                            stepsByStage[stage].ContainsKey(step)
                            && !stepsByStage[stage][step].Contains(status.ParentSectionId))
                        {
                            stepsByStage[stage][step].Add(status.ParentSectionId);
                        }
                    }
                }

                sectionIdsFoundInOtherWorkflows.AddRange(sectionIdsFoundInThisWorkflow);
                if (workflow != null) data.Add(workflow, stepsByStage);
            }

            return data;
        }

        public override void Dispose()
        {
            foreach (var stageCardViewModel in StageCards)
            {
                stageCardViewModel.Dispose();
            }

            StageCards?.Clear();

            foreach (var sectionInfoPlayerViewModel in SectionInfoPlayers)
            {
                sectionInfoPlayerViewModel?.Dispose();
            }
            SectionInfoPlayers.Clear();

            CloseInfoCommand?.Dispose();
            CloseInfoCommand = null;

            UnassignedSectionCollectionAsStageCardViewModel?.Dispose();
            UnassignedSectionCollectionAsStageCardViewModel = null;

            ApprovedSectionCollectionAsStageCardViewModel?.Dispose();
            ApprovedSectionCollectionAsStageCardViewModel = null;

            _sectionRepository.Dispose();

            _snapshotService?.Dispose();

            Section = null;

            base.Dispose();
        }
    }
}