using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;
using DynamicData;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Render.Components.BarPlayer;
using Render.Components.Modal;
using Render.Components.Modal.ModalComponents;
using Render.Extensions;
using Render.Kernel;
using Render.Kernel.WrappersAndExtensions;
using Render.Models.Audio;
using Render.Models.Project;
using Render.Models.Scope;
using Render.Models.Sections;
using Render.Models.Snapshot;
using Render.Models.Users;
using Render.Models.Workflow;
using Render.Models.Workflow.Stage;
using Render.Repositories.Kernel;
using Render.Repositories.SnapshotRepository;
using Render.Resources;
using Render.Resources.Localization;
using Render.Sequencer.Contracts.Interfaces;
using Render.Sequencer.Contracts.Models;
using Render.Services.AudioServices;

namespace Render.Pages.Consultant.ConsultantApproval
{
    public class ApproveSectionViewModel : WorkflowPageBaseViewModel
    {
        public static async Task<ApproveSectionViewModel> CreateAsync(
            IViewModelContextProvider viewModelContextProvider,
            Guid sectionId,
            Action<Section> removeApproveSectionAction)
        {
            var sectionRepository = viewModelContextProvider.GetSectionRepository();
            var section = await sectionRepository.GetSectionWithDraftsAsync(sectionId, true, true);
            var grandCentralStation = viewModelContextProvider.GetGrandCentralStation();
            var step = await grandCentralStation.GetStepToWorkAsync(sectionId, RenderStepTypes.ConsultantApproval);
            var stage = grandCentralStation.ProjectWorkflow.GetStage(step.Id);

            var approveSectionViewModel = new ApproveSectionViewModel(
                viewModelContextProvider: viewModelContextProvider,
                section: section,
                step: step,
                sectionAudio: new AudioPlayback(section.Id, section.Passages.Select(x => x.CurrentDraftAudio)),
                removeApproveSectionAction: removeApproveSectionAction,
                stage: stage);

            approveSectionViewModel.PopulateBackTranslatePlayersAsync(section);
            return approveSectionViewModel;
        }

        private IUser Consultant { get; }
        private ISnapshotRepository SnapshotRepository { get; }
        private readonly IDataPersistence<WorkflowStatus> _workflowStatusRepository;

        private bool IsSecondStep { get; }
        private Action<Section> _removeApproveSectionAction;

        private readonly SourceList<IBarPlayerViewModel> _backTranslateSource = new();
        private readonly ReadOnlyObservableCollection<IBarPlayerViewModel> _backTranslatePlayers;
        public ReadOnlyObservableCollection<IBarPlayerViewModel> BackTranslatePlayers => _backTranslatePlayers;

        [Reactive]
        public ISequencerPlayerViewModel SequencerPlayerViewModel { get; private set; }

        public ReactiveCommand<Unit, Unit> ReturnCommand { get; }
        public ReactiveCommand<Unit, Unit> ApproveCommand { get; }

        private ApproveSectionViewModel(
            IViewModelContextProvider viewModelContextProvider,
            Section section,
            Step step,
            AudioPlayback sectionAudio,
            Action<Section> removeApproveSectionAction,
            Stage stage) :
            base(
                urlPathSegment: "ApproveSection", 
                viewModelContextProvider: viewModelContextProvider, 
                pageName: AppResources.ConsultantApproval,
                section: section,
                stage: stage,
                step: step, 
                secondPageName: section.ScriptureReference)
        {
            DisposeOnNavigationCleared = true;
            TitleBarViewModel.DisposeOnNavigationCleared = true;
            Consultant = viewModelContextProvider.GetLoggedInUser();
            _removeApproveSectionAction = removeApproveSectionAction;
            TitleBarViewModel.PageGlyph = IconExtensions.BuildFontImageSource(Icon.ConsultantApproval)?.Glyph;
            _workflowStatusRepository = ViewModelContextProvider.GetPersistence<WorkflowStatus>();
            SnapshotRepository = ViewModelContextProvider.GetSnapshotRepository();

            SetupSequencer(sectionAudio, section.Number);

            // Check whether second step bt is enabled 
            var gcc = viewModelContextProvider.GetGrandCentralStation();
            var workflow = gcc.ProjectWorkflow;
            var entrySteps = workflow
                .GetAllActiveWorkflowEntrySteps()
                .Where(x => x.RenderStepType == RenderStepTypes.BackTranslate)
                .ToList();

            IsSecondStep = entrySteps.Count > 1;

            var backTranslatePlayerChangeList = _backTranslateSource.Connect().Publish();
            Disposables
                .Add(backTranslatePlayerChangeList
                .Bind(out _backTranslatePlayers)
                .Subscribe());

            Disposables.Add(backTranslatePlayerChangeList.Connect());

            ApproveCommand = ReactiveCommand.CreateFromTask(TryApproveAsync);
            ReturnCommand = ReactiveCommand.CreateFromTask(TryReturnAsync);
        }

        private void SetupSequencer(AudioPlayback sectionAudio, int sectionNumber)
        {
            SequencerPlayerViewModel = ViewModelContextProvider
                .GetSequencerFactory()
                .CreatePlayer(ViewModelContextProvider.GetAudioPlayer);

            SequencerPlayerViewModel.IsRightToLeftDirection = FlowDirection is FlowDirection.RightToLeft;

            SequencerPlayerViewModel.SetupActivityService(ViewModelContextProvider, Disposables);
            SequencerPlayerViewModel.SetAudio(new[] { PlayerAudioModel.Create(
                path: ViewModelContextProvider.GetTempAudioService(sectionAudio).SaveTempAudio(),
                name: string.Format(AppResources.Section, sectionNumber), 
                startIcon: Icon.SectionNew.ToString(),
                number: sectionNumber.ToString()) });
        }

        private void PopulateBackTranslatePlayersAsync(Section section)
        {
            var retellBtAudios = IsSecondStep && section.CheckSecondStepRetellAudio()
                ? section.GetSecondStepBackTranslationRetellAudios()?.Cast<Audio>().ToList()
                : section.GetBackTranslationRetellAudios()?.Cast<Audio>().ToList();

            var segmentBtAudios = IsSecondStep && section.CheckSecondStepSegmentAudio()
                ? section.GetSecondStepBackTranslationSegmentAudios()?.Cast<Audio>().ToList()
                : section.GetBackTranslationSegmentAudios()?.Cast<Audio>().ToList();

            if (retellBtAudios != null && retellBtAudios.Any())
            {
                SetBackTranslationBarPlayer(retellBtAudios, string.Format(AppResources.PassageBackTranslate));
            }
            
            if (segmentBtAudios != null && segmentBtAudios.Any())
            {
                SetBackTranslationBarPlayer(segmentBtAudios, string.Format(AppResources.SegmentBackTranslate));
            }
        }

        private void SetBackTranslationBarPlayer(IEnumerable<Audio> audioSequence, string backTranslationName)
        {
            if (audioSequence == null)
            {
                return;
            }

            _backTranslateSource.Add(new BarPlayerViewModel(
                audioPlayback: new AudioPlayback(Guid.NewGuid(), audioSequence),
                viewModelContextProvider: ViewModelContextProvider,
                initialState: ActionState.Optional,
                audioTitle: backTranslationName,
                playerPositionInList: 0));
        }

        private async Task TryApproveAsync()
        {
            var result = await ShowConfirmationModal();
            if (result is not DialogResult.Ok)
            {
                return;
            }

            var modalService = ViewModelContextProvider.GetModalService();
            await modalService.ShowInfoModal(
                icon: Icon.ConsultantApproval,
                title: AppResources.ApprovedDraft,
                message: $"{Section.ScriptureReference} {AppResources.ApprovalSubmitted}");

            await NavigateToSelectApprove();
        }

        private async Task TryReturnAsync()
        {
            var result = await ShowConfirmationModal();
            if (result is not DialogResult.Ok)
            {
                return;
            }

            await ReturnSectionBackAndGoHOme();
        }

        public Task<DialogResult> ShowConfirmationModal()
        {
            var modalService = ViewModelContextProvider.GetModalService();
            var approveSectionComponent = new SectionApproveComponentViewModel(ViewModelContextProvider);

            var confirmationModal = new ModalViewModel(
                ViewModelContextProvider,
                modalService,
                Icon.ExportLayerPassword,
                AppResources.PasswordRequired,
                approveSectionComponent,
                new ModalButtonViewModel(AppResources.Cancel),
                new ModalButtonViewModel(AppResources.Confirm))
            {
                BeforeConfirmCommand = approveSectionComponent.ConfirmCommand
            };

            return modalService.ConfirmationModal(confirmationModal);
        }

        public async Task NavigateToSelectApprove()
        {
            Section.SetApprovedBy(Consultant.Id, DateTimeOffset.Now);
            await ViewModelContextProvider.GetSectionRepository().SaveSectionAsync(Section);

            var grandCentralStation = ViewModelContextProvider.GetGrandCentralStation();
            
            await grandCentralStation.AdvanceSectionAsync(Section, Step);
            // remove Holding Tank workflow statuses
            await grandCentralStation.RemoveHoldingTankSectionStatusesAfterApproval(Section.Id);

            // update Project Statistics
            var statisticsPersistence = ViewModelContextProvider.GetPersistence<RenderProjectStatistics>();
            var projectStatistics = (await statisticsPersistence.QueryOnFieldAsync("ProjectId", Section.ProjectId.ToString(), 1, false)).FirstOrDefault();
            if (projectStatistics != null && projectStatistics.FirstScriptureApprovedDate.Year == 1)
            {
                projectStatistics.SetFirstScriptureApprovedDate(DateTimeOffset.Now);
                await statisticsPersistence.UpsertAsync(projectStatistics.Id, projectStatistics);
            }

            //Update Scope class with first section drafted date
            var scopePersistence = ViewModelContextProvider.GetPersistence<Scope>();
            var scope = await scopePersistence.GetAsync(Section.ScopeId);
            var sectionRepository = ViewModelContextProvider.GetPersistence<Section>();
            var sectionsForScope = await sectionRepository.QueryOnFieldAsync("ScopeId", scope.Id.ToString(), 0);
            if (sectionsForScope.All(x => x.ApprovedBy != Guid.Empty && scope.FinalSectionApprovedDate.Year == 1))
            {
                scope.SetFinalSectionApprovedDate(DateTimeOffset.Now);
                await scopePersistence.UpsertAsync(scope.Id, scope);
            }

            LogInfo("Section Approved", new Dictionary<string, string>
            {
                {"SectionId", Section.Id.ToString()},
                {"Consultant Name", Consultant.Username}
            });

            _removeApproveSectionAction?.Invoke(Section);
        }

        public async Task ReturnSectionBackAndGoHOme()
        {
            //mark current workflow status as complete and create a new one pointing to the revise step of the previous stage
            var sectionWorkflowStatusObjects = await _workflowStatusRepository.QueryOnFieldAsync(
                searchField: "ParentSectionId",
                value: Section.Id.ToString(),
                limit: 0) ?? new List<WorkflowStatus>();

            var currentWorkflowStatuses = sectionWorkflowStatusObjects.Where(x => !x.IsCompleted && x.CurrentStageId == Stage.Id);
            if (currentWorkflowStatuses.Count() < 1)
            {
                _removeApproveSectionAction?.Invoke(Section);
            }

            foreach (var currentWorkflowStatus in currentWorkflowStatuses)
            {
                currentWorkflowStatus.MarkAsCompleted();
                await _workflowStatusRepository.UpsertAsync(currentWorkflowStatus.Id, currentWorkflowStatus);
            }

            var workflow = ViewModelContextProvider.GetGrandCentralStation().ProjectWorkflow;
            var allStages = workflow.GetAllStages().ToList();

            Step previousStep;
            // each project has at least two stages: draft and consultant approval
            // since we are at the consultant approval stage which is the last stage, so
            // the previous stage is the second from the last of the list
            Stage previousStage = allStages[allStages.Count - 2];
            // since Draft stage does not have a revise step, we return the section back to the draft step of the draft stage
            if (previousStage.Type.Contains("DraftingStage"))
            {
                //DraftingStage overrides the Steps property so the DraftingStage contains only one step
                previousStep = ((DraftingStage)previousStage).Steps.First();
            }
            else
            {
                previousStep = previousStage.ReviseStep;
            }

            var newWorkflowStatus = new WorkflowStatus(
                Section.Id,
                workflow.Id,
                Section.ProjectId,
                previousStep.Id,
                Section.ScopeId,
                previousStage.Id,
                previousStep.RenderStepType);

            await _workflowStatusRepository.UpsertAsync(newWorkflowStatus.Id, newWorkflowStatus);

            var snapshots = await SnapshotRepository.GetSnapshotsForSectionAsync(Section.Id);
            var previousSnapshot = snapshots.SingleOrDefault(x => !x.Temporary && x.StageId == previousStage.Id);
            if (previousSnapshot == null)
            {
                _removeApproveSectionAction?.Invoke(Section);
                return;
            }

            var list = new List<Snapshot> { previousSnapshot };
            await SnapshotRepository.BatchSoftDeleteAsync(list, Section);

            LogInfo("Section Returned", new Dictionary<string, string>
            {
                {"SectionId", Section.Id.ToString()},
                {"Consultant Name", Consultant.Username}
            });

            _removeApproveSectionAction?.Invoke(Section);
        }

        public override void Dispose()
        {
            SequencerPlayerViewModel?.Dispose();
            SequencerPlayerViewModel = null;

            _removeApproveSectionAction = null;
            SnapshotRepository.Dispose();
            _workflowStatusRepository.Dispose();

            base.Dispose();
        }
    }
}