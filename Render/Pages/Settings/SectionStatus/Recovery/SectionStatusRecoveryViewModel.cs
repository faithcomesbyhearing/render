using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Render.Components.MiniWaveformPlayer;
using Render.Components.Modal;
using Render.Components.Modal.ModalComponents;
using Render.Kernel;
using Render.Kernel.WrappersAndExtensions;
using Render.Models.Audio;
using Render.Models.Sections;
using Render.Models.Snapshot;
using Render.Models.Users;
using Render.Models.Workflow;
using Render.Models.Workflow.Stage;
using Render.Pages.Settings.SectionStatus.Processes;
using Render.Repositories.Audio;
using Render.Repositories.SectionRepository;
using Render.Repositories.SnapshotRepository;
using Render.Resources;
using Render.Resources.Localization;
using Render.Services;
using Render.Services.AudioServices;

namespace Render.Pages.Settings.SectionStatus.Recovery;

public class SectionStatusRecoveryViewModel : ViewModelBase
{
    private readonly IAudioRepository<Draft> _draftRepository;
    private readonly ISnapshotRepository _snapshotRepository;
    private readonly ISectionRepository _sectionRepository;
    private readonly IGrandCentralStation _grandCentralStation;

    private SourceList<SectionCardViewModel> SectionCardViewModelSourceList { get; set; } = new();
    private readonly ReadOnlyObservableCollection<SectionCardViewModel> _sectionCardViewModels;
    public ReadOnlyObservableCollection<SectionCardViewModel> SectionCardViewModels => _sectionCardViewModels;

    private SourceList<SnapshotCardViewModel> SnapshotCardViewModelSourceList { get; set; } = new();
    private readonly ReadOnlyObservableCollection<SnapshotCardViewModel> _snapshotCardViewModels;
    public ReadOnlyObservableCollection<SnapshotCardViewModel> SnapshotCardViewModels => _snapshotCardViewModels;

    [Reactive] public IMiniWaveformPlayerViewModel SectionPlayer { get; set; }
    [Reactive] public string SearchString { get; set; } = "";
    [Reactive] public string SectionTitleLabel { get; set; } = string.Format(AppResources.Section, "");
    [Reactive] public SectionCardViewModel SelectedCard { get; set; }
    [Reactive] public bool SectionApproved { get; set; }
    [Reactive] public bool ShowBarPlayer { get; set; }
    [Reactive] public bool ConflictMode { get; set; }
    [Reactive] public bool ApprovalPresent { get; set; }
    [Reactive] public bool ConflictPresent { get; set; }
    [Reactive] public ResetCardViewModel ResetCard { get; set; }
    [Reactive] public string StageName { get; set; }
    private Stage ConflictedStage { get; set; }
    [Reactive] public bool ShowRecoveryView { get; set; }

    public ReactiveCommand<Unit, Unit> SelectSnapshotCommand;
    public ReactiveCommand<Unit, Unit> DeleteSnapshotsCommand;

    public event Action SectionRecovered;

    private bool FirstPass { get; set; }

    public static async Task<SectionStatusRecoveryViewModel> CreateAsync(List<Section> sections, IViewModelContextProvider viewModelContextProvider)
    {
        var vm = new SectionStatusRecoveryViewModel(sections, viewModelContextProvider);
        if (vm.SectionCardViewModelSourceList.Count > 0)
        {
            //Check for snapshot conflict
            var grandCentralStation = viewModelContextProvider.GetGrandCentralStation();
            foreach (var sectionCard in vm._sectionCardViewModels)
            {
                sectionCard.HasConflict = await grandCentralStation.CheckForSnapshotConflicts(sectionCard.Section.Id);
            }

            await vm.SelectSectionCardAsync(vm.SectionCardViewModels.First().Section.Id);
        }

        return vm;
    }

    private SectionStatusRecoveryViewModel(List<Section> sections, IViewModelContextProvider viewModelContextProvider) :
        base("SectionStatusRecoveryView", viewModelContextProvider)
    {
        _draftRepository = ViewModelContextProvider.GetDraftRepository();
        _snapshotRepository = ViewModelContextProvider.GetSnapshotRepository();
        _sectionRepository = ViewModelContextProvider.GetSectionRepository();
        _grandCentralStation = ViewModelContextProvider.GetGrandCentralStation();
        StageName = "";

        FirstPass = true;
        ResetCard = new ResetCardViewModel(
            viewModelContextProvider: viewModelContextProvider,
            section: sections.FirstOrDefault(),
            sectionResetCallback: ShowConfirmation);

        var changeList = SectionCardViewModelSourceList
            .Connect()
            .Publish();

        Disposables.Add(changeList
            .AutoRefreshOnObservable(x => this.WhenAnyValue(s => s.SearchString))
            .Filter(x => x.Section.Number.ToString().Contains(SearchString.ToLowerInvariant()))
            .Sort(SortExpressionComparer<SectionCardViewModel>.Ascending(sectionCard => sectionCard.Section.Number))
            .Bind(out _sectionCardViewModels)
            .Subscribe());

        Disposables.Add(changeList
            .WhenPropertyChanged(x => x.Section)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(async x =>
            {
                if (!FirstPass)
                {
                    SelectedCard = null;

                    if (x.Sender.Section != null)
                    {
                        await SelectSectionCardAsync(x.Sender.Section.Id);
                    }
                }
            }));

        Disposables.Add(changeList
            .WhenPropertyChanged(x => x.HasConflict)
            .Subscribe(x => { ConflictPresent = _sectionCardViewModels.Any(sectionCard => sectionCard.HasConflict); }));

        Disposables.Add(changeList.Connect());

        var snapshotChangeList = SnapshotCardViewModelSourceList
            .Connect()
            .Publish();

        Disposables.Add(snapshotChangeList
            .Bind(out _snapshotCardViewModels)
            .Subscribe());

        Disposables.Add(snapshotChangeList
            .WhenPropertyChanged(x => x.IsSelected)
            .Subscribe(vm =>
            {
                if (!vm.Sender.IsSelected) return;
                foreach (var snapshotCard in _snapshotCardViewModels)
                {
                    if (snapshotCard.Snapshot != null && snapshotCard.Snapshot.Id != vm.Sender.Snapshot.Id)
                    {
                        snapshotCard.IsSelected = false;
                    }
                }
            }));

        Disposables.Add(snapshotChangeList.Connect());

        Disposables.Add(this.WhenAnyValue(x => x.SelectedCard)
            .Subscribe(selectedCard =>
            {
                if (selectedCard != null)
                {
                    ShowBarPlayer = selectedCard.Section.Passages.Any(passage => passage.HasAudio);
                }
            }));

        Disposables.Add(this.WhenAnyValue(x => x.SelectedCard.IsApproved)
            .Subscribe(isApproved => { ApprovalPresent = isApproved; }));

        foreach (var section in sections)
        {
            SectionCardViewModelSourceList.Add(new SectionCardViewModel(section, viewModelContextProvider, SelectSectionCardAsync));
        }
        SelectSnapshotCommand = ReactiveCommand.CreateFromTask(ShowSnapshotSelect);
        DeleteSnapshotsCommand = ReactiveCommand.CreateFromTask(ShowClearSnapshots);
    }

    private async Task<Stage> GetConflictedStage(Guid sectionId) => await _grandCentralStation.GetConflictedStage(sectionId);
    
    private async Task SelectSectionCardAsync(Guid clickedSectionId)
    {
        IsLoading = true;

        foreach (var sectionCard in _sectionCardViewModels)
        {
            if (sectionCard.Section.Id == clickedSectionId)
            {
                // Check that no conflicts have come up from syncing since entering the page
                sectionCard.HasConflict =
                    await _grandCentralStation.CheckForSnapshotConflicts(clickedSectionId);
                sectionCard.IsSelected = true;
                var sectionWithDrafts = await _sectionRepository.GetSectionWithDraftsAsync(sectionCard.Section.Id);
                sectionCard.Section.SetPassages(sectionWithDrafts.Passages);
                SelectedCard = sectionCard;
                ConflictMode = sectionCard.HasConflict;
                if (sectionCard.HasConflict)
                {
                    ConflictedStage = await GetConflictedStage(clickedSectionId);
                    StageName = Utilities.Utilities.GetStageName(ConflictedStage);
                }
                
                var snapshotList = await _snapshotRepository.GetPermanentSnapshotsForSectionAsync(clickedSectionId);
                var latestSnapshot = snapshotList.LastOrDefault();
                var workflow = _grandCentralStation.ProjectWorkflow;
                var stageList = workflow.GetAllStages(includeDeactivatedStages: true);

                foreach (var disposable in SnapshotCardViewModelSourceList.Items)
                {
                    disposable.Dispose();
                }

                SnapshotCardViewModelSourceList.Clear();

                ResetCard = new ResetCardViewModel(
                    viewModelContextProvider: ViewModelContextProvider,
                    section: sectionCard.Section,
                    sectionResetCallback: ShowConfirmation,
                    sectionHasSnapshots: snapshotList.Any());

                for (var i = 0; i < snapshotList.Count; i++)
                {
                    var snapshot = snapshotList[i];
                    var stage = stageList.SingleOrDefault(x => x.Id == snapshot.StageId);
                    var currentSnapshot = snapshot != null && snapshot.Equals(latestSnapshot);
                    SnapshotCardViewModelSourceList.Add(new SnapshotCardViewModel(
                        stage: stage,
                        first: i < 0,
                        last: i == snapshotList.Count - 1,
                        snapshot: snapshot,
                        currentSnapshot: currentSnapshot,
                        restoreSnapshotConfirmationCallback: ShowConfirmation,
                        snapshotSelectedCallback: StageCardSelected,
                        viewModelContextProvider: ViewModelContextProvider));
                }

                SectionApproved = sectionCard.Section.ApprovedBy != Guid.Empty;
                
                var sectionPlayer = await Task.Run(() => GetSectionPlayer(
                    passages: sectionCard.Section.Passages,
                    playerTitle: SectionApproved
                        ? string.Format(AppResources.ApprovedSection)
                        : string.Format(AppResources.Section, SelectedCard?.Section.Number)));

                ResetSectionPlayer(sectionPlayer);
            }
            else
            {
                sectionCard.IsSelected = false;
            }
        }

        FirstPass = false;

        IsLoading = false;
    }

    private async Task RevertSnapshotAsync(Guid clickedSnapshotId)
    {
        // Reset section is selected
        if (clickedSnapshotId == Guid.Empty)
        {
            await RevertSectionToDefault(SelectedCard.Section);
            SectionRecovered?.Invoke();
            return;
        }
        
        var snapshotList = await _snapshotRepository.GetSnapshotsForSectionAsync(SelectedCard.Section.Id);
        var selectedSnapshot = _snapshotCardViewModels.First(x => x.Snapshot != null && x.Snapshot.Id == clickedSnapshotId)?.Snapshot;
        var sectionRepo = ViewModelContextProvider.GetSectionRepository();
        selectedSnapshot = await _snapshotRepository.GetPassageDraftsForSnapshot(selectedSnapshot, true, true);
        var newSection = await sectionRepo.RevertSectionToSnapshotAsync(selectedSnapshot, snapshotList, GetLoggedInUserId(), GetProjectId());

        var afterCurrent = false;
        var snapshotsToDelete = new List<Snapshot>();
        foreach (var snapshot in snapshotList)
        {
            if (!afterCurrent)
            {
                snapshot.SetSectionId(newSection.Id);
                await _snapshotRepository.SaveAsync(snapshot);
                if (snapshot.StageId == selectedSnapshot.StageId)
                {
                    afterCurrent = true;
                }
            }
            else
            {
                snapshotsToDelete.Add((Snapshot)snapshot);
            }
        }

        //Delete Note Interpretations if necessary
        var interpretationIdsToDelete = new HashSet<Guid>();
        foreach (var interpretationId in from snapshotToDelete in snapshotsToDelete
                 from interpretationId in snapshotToDelete.NoteInterpretationIds
                 where !selectedSnapshot.NoteInterpretationIds.Contains(interpretationId)
                 select interpretationId)
        {
            interpretationIdsToDelete.Add(interpretationId);
        }

        var audioRepository = ViewModelContextProvider.GetAudioRepository();
        foreach (var id in interpretationIdsToDelete)
        {
            await audioRepository.DeleteAudioByIdAsync(id);
        }

        var workflowRepo = ViewModelContextProvider.GetWorkflowRepository();
        var renderWorkflow = await workflowRepo.GetWorkflowForProjectIdAsync(SelectedCard.Section.ProjectId);
        var step = renderWorkflow.GetStep(selectedSnapshot.StepId);
        var teams = renderWorkflow.GetTeams();
        foreach (var team in teams)
        {
            foreach (var assignment in team.SectionAssignments)
            {
                if (assignment.SectionId == selectedSnapshot.SectionId)
                {
                    team.AddSectionAssignment(newSection.Id, assignment.Priority);
                    team.RemoveSectionAssignment(assignment.SectionId);
                    break;
                }
            }
        }
        
        await _snapshotRepository.BatchSoftDeleteAsync(snapshotsToDelete, newSection);
        await workflowRepo.SaveWorkflowAsync(renderWorkflow);
        await _grandCentralStation.AdvanceSectionAfterRecoveryAsync(newSection, step);
        await _grandCentralStation.FindWorkForUser(_grandCentralStation.CurrentProjectId, GetLoggedInUserId());
        SelectedCard.SetSection(newSection);
        SectionRecovered?.Invoke();
    }


    // This method handles choosing a user's snapshots, and deleting all conflicted snapshots.
    // Not picking a snapshot deletes all snapshots at the conflict point and later
    public async Task ChooseSnapshot(Snapshot chosenSnapshot = default)
    {
        await ResolveConflict(chosenSnapshot);

        //Update UI
        await SelectSectionCardAsync(SelectedCard.Section.Id);

        var newMostRecent = _snapshotCardViewModels.LastOrDefault(x => x.Snapshot != null)?.Snapshot;
        if (newMostRecent != null)
        {
            await RevertSnapshotAsync(newMostRecent.Id);
        }
        else
        {
            //All the snapshots were deleted so we need to reset the section
            await RevertSectionToDefault(SelectedCard.Section);
        }
    }

    private async Task ResolveConflict(Snapshot chosenSnapshot)
    {
        var snapshots = await _snapshotRepository.GetPermanentSnapshotsForSectionAsync(SelectedCard.Section.Id);
        var snapshotsToKeep = new List<Guid>();
        //Find all snapshots we want to keep by tracing the chain from the first snapshot through the conflict point
        //(if we chose one) to the end of the chain. If the conflict point is Drafting,
        //we'll use the chosen snapshot (if we chose one) to start the chain
        var previousSnapshot = snapshots.First().StageId == ConflictedStage.Id ? chosenSnapshot : snapshots.First();
        while (previousSnapshot != null)
        {
            snapshotsToKeep.Add(previousSnapshot.Id);
            //If we hit the conflict point, use the chosen snapshot (if we chose one) to continue following the chain
            previousSnapshot = snapshots.FirstOrDefault(s => s.ParentSnapshot == previousSnapshot.Id);
            if (previousSnapshot != null && previousSnapshot.StageId == ConflictedStage.Id)
            {
                previousSnapshot = chosenSnapshot;
            }
        }

        //Delete all other snapshots
        foreach (var snapshot in snapshots.Where(s => !snapshotsToKeep.Contains(s.Id)))
        {
            snapshot.SetDeleted(true);
            await _snapshotRepository.SaveAsync(snapshot);
        }
    }

    public async Task RevertSectionToDefault(Section section)
    {
        var newSection = await _sectionRepository.RevertSectionToDefaultAsync(section, GetLoggedInUserId(), GetProjectId());
        var workflowRepo = ViewModelContextProvider.GetWorkflowRepository();
        var renderWorkflow = await workflowRepo.GetWorkflowForProjectIdAsync(SelectedCard.Section.ProjectId);
        var step = renderWorkflow.DraftingStage.Steps.First();
        //resets workflow status back to drafting
        await _grandCentralStation.ReplaceWorkflowStatus(newSection, step, renderWorkflow);
        var teams = renderWorkflow.GetTeams();
        foreach (var team in teams)
        {
            foreach (var assignment in team.SectionAssignments)
            {
                if (assignment.SectionId == section.Id)
                {
                    team.AddSectionAssignment(newSection.Id, assignment.Priority);
                    team.RemoveSectionAssignment(assignment.SectionId);
                    break;
                }
            }
        }
        
        var snapshotList = await _snapshotRepository.GetSnapshotsForSectionAsync(SelectedCard.Section.Id);
        await _snapshotRepository.BatchSoftDeleteAsync(snapshotList, newSection);
        SelectedCard.SetSection(newSection);
        await workflowRepo.SaveWorkflowAsync(renderWorkflow);
    }

    private async Task ShowSnapshotSelect()
    {
        var section = SelectedCard.Section;
        var snapshots = await _snapshotRepository.GetSnapshotsForSectionAsync(section.Id);
        var userRepository = ViewModelContextProvider.GetUserRepository();
        var currentWorkflow = _grandCentralStation.ProjectWorkflow;
        var pairs = new List<(IUser, Snapshot, Team)>();
        //Get users for conflicted snapshots
        foreach (var snapshot in snapshots)
        {
            foreach (var otherSnapshot in snapshots.Where(otherSnapshot => snapshot.StageId == otherSnapshot.StageId && !snapshot.Equals(otherSnapshot)))
            {
                var user = await userRepository.GetUserAsync(snapshot.CreatedBy);
                var team = currentWorkflow.GetTeamForTranslatorId(user.Id);
                pairs.Add(((IUser, Snapshot, Team))(user, snapshot, team));
                var user2 = await userRepository.GetUserAsync(otherSnapshot.CreatedBy);
                var otherPairTeam = currentWorkflow.GetTeamForTranslatorId(user2.Id);
                pairs.Add(((IUser, Snapshot, Team))(user2, otherSnapshot, otherPairTeam));
            }
        }

        var stageName = Utilities.Utilities.GetStageName(ConflictedStage);

        var snapshotSelectModalViewModel = new SnapshotSelectComponentViewModel(pairs, SelectedCard.Section, stageName, ViewModelContextProvider);

        var modalService = ViewModelContextProvider.GetModalService();

        var selectSnapshotsModal = new ModalViewModel(
            ViewModelContextProvider,
            modalService,
            Icon.SelectASnapshot,
            AppResources.SelectSnapshot,
            snapshotSelectModalViewModel,
            new ModalButtonViewModel(AppResources.No), new ModalButtonViewModel(AppResources.Yes))
        {
            FooterIsVisible = false
        };

        var snapshotSelectModalViewModelResult = await modalService.ConfirmationModal(selectSnapshotsModal);

        if (snapshotSelectModalViewModelResult != DialogResult.Ok) return;

        var selectedSnapshotMessage = new SelectedSnapshotConfirmationMessageViewModel(snapshotSelectModalViewModel.SelectedPair, section, ViewModelContextProvider);
        
        var selectASnapshotsModal = new ModalViewModel(
            ViewModelContextProvider,
            modalService,
            Icon.SelectASnapshot,
            AppResources.YouHaveChosen,
            selectedSnapshotMessage,
            new ModalButtonViewModel(AppResources.No), new ModalButtonViewModel(AppResources.Yes))
        {
            AfterConfirmCommand = ReactiveCommand.CreateFromTask(async () => { await ChooseSnapshot(snapshotSelectModalViewModel.SelectedPair.Snapshot); })
        };

        var selectASnapshotsModalResult = await modalService.ConfirmationModal(selectASnapshotsModal);

        if (selectASnapshotsModalResult != DialogResult.Ok) return;

        var teamWasChosenTitle = string.Format(AppResources.TeamWasChosen, selectedSnapshotMessage.TeamNumber, selectedSnapshotMessage.SnapshotUpdatedData);
        var teamWasChosenMessage = string.Format(AppResources.TeamWasChosenForSection,
            snapshotSelectModalViewModel.SelectedPair.User.FullName,
            selectedSnapshotMessage.TeamNumber.ToLower(),
            selectedSnapshotMessage.SnapshotUpdatedData,
            SelectedCard.Section.ScriptureReference);

        await modalService.ShowInfoModal(Icon.SelectASnapshot, teamWasChosenTitle, teamWasChosenMessage);

        selectASnapshotsModal.Dispose();
    }

    private async Task ShowClearSnapshots()
    {
        var modalService = ViewModelContextProvider.GetModalService();

        var clearSnapshotsModal = new ModalViewModel(
            ViewModelContextProvider,
            modalService,
            Icon.ClearBothSnapshots,
            string.Format(AppResources.DeleteSnapshotsFor, SelectedCard.Section.ScriptureReference),
            string.Format(AppResources.DeleteSnapshotsConfirmation, SelectedCard.Section.ScriptureReference),
            new ModalButtonViewModel(AppResources.No), new ModalButtonViewModel(AppResources.Yes))
        {
            AfterConfirmCommand = ReactiveCommand.CreateFromTask(async () => await ChooseSnapshot())
        };

        var clearSnapshotsModalResult = await modalService.ConfirmationModal(clearSnapshotsModal);

        if (clearSnapshotsModalResult != DialogResult.Ok) return;

        await modalService.ShowInfoModal(Icon.ClearBothSnapshots, AppResources.BothSnapshotsDeleted, AppResources.SectionReset);
    }

    private async Task ShowConfirmation(Guid snapshotId)
    {
        var modalService = ViewModelContextProvider.GetModalService();
        var approveSectionComponent = new SectionApproveComponentViewModel(ViewModelContextProvider);

        var lossDataWarningConfirmationModal = new ModalViewModel(
            ViewModelContextProvider,
            modalService,
            Icon.PopUpWarning,
            AppResources.DraftWillBeLost,
            AppResources.ResetSectionWarning,
            new ModalButtonViewModel(AppResources.Cancel),
            new ModalButtonViewModel(AppResources.ResetSection));

        var result = await modalService.ConfirmationModal(lossDataWarningConfirmationModal);
        if (result != DialogResult.Ok) return;

        var resetSectionConfirmationModal = new ModalViewModel(
            ViewModelContextProvider,
            modalService,
            Icon.ExportLayerPassword,
            AppResources.PasswordRequired,
            approveSectionComponent,
            new ModalButtonViewModel(AppResources.Cancel),
            new ModalButtonViewModel(AppResources.Confirm))
        {
            BeforeConfirmCommand = approveSectionComponent.ConfirmCommand,
            AfterConfirmCommand = ReactiveCommand.CreateFromTask(() => RevertSnapshotAsync(snapshotId))
        };

        await modalService.ConfirmationModal(resetSectionConfirmationModal);
    }

    private async Task StageCardSelected(Snapshot selectedSnapshot)
    {
        if (SelectedCard is null) return;

        IsLoading = true;

        IMiniWaveformPlayerViewModel sectionPlayer;
        if (selectedSnapshot is null)
        {
            var sectionWithAudio = await _sectionRepository.GetSectionWithDraftsAsync(SelectedCard.Section.Id);
            sectionPlayer = await Task.Run(() => GetSectionPlayer(
                passages: sectionWithAudio.Passages,
                playerTitle: string.Format(AppResources.Section, SelectedCard.Section.Number)));
        }
        else
        {
            var snapshotWithAudio = await _snapshotRepository.GetPassageDraftsForSnapshot(selectedSnapshot);   
            sectionPlayer = await Task.Run(() => GetSectionPlayer(
                passages:  snapshotWithAudio.Passages,
                playerTitle: selectedSnapshot.StageName));
        }
        
        ResetSectionPlayer(sectionPlayer);

        IsLoading = false;
    }

    private IMiniWaveformPlayerViewModel GetSectionPlayer(IList<Passage> passages, string playerTitle)
    {
        return ViewModelContextProvider.GetMiniWaveformPlayerViewModel(
            audio: new AudioPlayback(Guid.NewGuid(), passages.Select(passage => passage.CurrentDraftAudio)),
            actionState: ActionState.Optional,
            title: playerTitle);
    }

    private void ResetSectionPlayer(IMiniWaveformPlayerViewModel sectionPlayer)
    {
        SectionPlayer?.Dispose();
        SectionPlayer = sectionPlayer;
    }

    public override void Dispose()
    {
        _draftRepository?.Dispose();
        _sectionRepository?.Dispose();
        _snapshotRepository?.Dispose();

        SectionRecovered = null;

        foreach (var disposable in SectionCardViewModelSourceList.Items)
        {
            disposable.Dispose();
        }

        foreach (var disposable in SnapshotCardViewModelSourceList.Items)
        {
            disposable.Dispose();
        }

        SectionCardViewModelSourceList?.Dispose();
        SectionCardViewModelSourceList = null;

        SnapshotCardViewModelSourceList?.Dispose();
        SnapshotCardViewModelSourceList = null;

        SectionPlayer?.Dispose();
        SectionPlayer = null;

        SelectedCard?.Dispose();
        SelectedCard = null;

        ResetCard?.Dispose();
        ResetCard = null;

        SelectSnapshotCommand?.Dispose();
        SelectSnapshotCommand = null;
        DeleteSnapshotsCommand?.Dispose();
        DeleteSnapshotsCommand = null;

        base.Dispose();
    }
}