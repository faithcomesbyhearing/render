using System.Collections.ObjectModel;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;
using Render.Components.Modal;
using Render.Components.Modal.ModalComponents;
using Render.Components.Scroller;
using Render.Kernel;
using Render.Kernel.WrappersAndExtensions;
using Render.Models.Workflow;
using Render.Models.Workflow.Stage;
using Render.Pages.AppStart.Home;
using Render.Repositories.WorkflowRepositories;
using Render.Resources;
using Render.Resources.Localization;
using Render.Resources.Styles;
using Render.TempFromVessel.Project;

namespace Render.Pages.Configurator.WorkflowManagement;

public class WorkflowConfigurationViewModel : WorkflowPageBaseViewModel
{
    private StageState? _originalStageState;
    private Stage _stageForDelete;
    private Stage _lastAddedStage;
    private readonly string _projectName;

    private RenderWorkflow Workflow { get; }

    public ScrollerViewModel ScrollerViewModel { get; }

    private readonly SourceList<WorkflowStageCardViewModel> _stagesSource = new ();

    private readonly ReadOnlyObservableCollection<WorkflowStageCardViewModel> _stageCards;
    public ReadOnlyObservableCollection<WorkflowStageCardViewModel> StageCards => _stageCards;

    public DynamicDataWrapper<StageTypeCardViewModel> StagesTypes = new DynamicDataWrapper<StageTypeCardViewModel>();

    private readonly IWorkflowRepository _workflowPersistence;

    public static async Task<WorkflowConfigurationViewModel> CreateAsync(Guid projectId,
        IViewModelContextProvider viewModelContextProvider)
    {
        var workflowRepository = viewModelContextProvider.GetWorkflowRepository();
        var workflow = await workflowRepository.GetWorkflowForProjectIdAsync(projectId);

        var projectRepository = viewModelContextProvider.GetPersistence<Project>();
        var project = await projectRepository.GetAsync(projectId);

        return new WorkflowConfigurationViewModel(workflow, viewModelContextProvider, project.Name);
    }

    private WorkflowConfigurationViewModel(RenderWorkflow workflow,
        IViewModelContextProvider viewModelContextProvider, string projectName) :
        base("WorkflowConfiguration", viewModelContextProvider, AppResources.ConfigureWorkflow, null, null,
            secondPageName: projectName)
    {
        _projectName = projectName;

        var color = (ColorReference)ResourceExtensions.GetResourceValue("SecondaryText");
        if (color != null)
        {
            TitleBarViewModel.PageGlyph = IconExtensions.BuildFontImageSource(Icon.ConfigureWorkflow, color.Color)?.Glyph;
        }

        CreateStageTypes();
        Workflow = workflow;
        _workflowPersistence = viewModelContextProvider.GetWorkflowRepository();
        ProceedButtonViewModel.SetCommand(NavigateHome);
        Disposables.Add(ProceedButtonViewModel.NavigateToPageCommand.IsExecuting
            .Subscribe(isExecuting => { IsLoading = isExecuting; }));
        var changeList = _stagesSource.Connect()
            .Publish();
        Disposables.Add(changeList
            .WhenPropertyChanged(x => x.ShowAddStepAfterCard)
            .Subscribe(s =>
            {
                if (!s.Value)
                {
                    if (StageCards.Count == 2)
                    {
                        StageCards.First().ShowAddStepAfterCard = true;
                    }
                }
            }));
        Disposables.Add(changeList
            .Bind(out _stageCards)
            .Subscribe());
        Disposables.Add(changeList.Connect());
        CreateStageCards();

        Disposables.Add(_stageCards
            .ToObservableChangeSet()
            .MergeMany(item => item.OpenStageSettingsCommand.IsExecuting)
            .Subscribe(isExecuting => { IsLoading = isExecuting; }));

        ScrollerViewModel = new ScrollerViewModel(viewModelContextProvider);

        Disposables.Add(StageCards.WhenAnyValue(x => x.Count)
            .Subscribe(i =>
            {
                if (i <= 2 && i > 0)
                {
                    StageCards.First().ShowAddStepAfterCard = true;
                }
                else if (i > 0)
                {
                    StageCards.First().ShowAddStepAfterCard = false;
                }
            }));
    }

    private void CreateStageCards()
    {
        _stagesSource.Edit(i =>
        {
            foreach (var item in i)
            {
                item.Dispose();
            }

            i.Clear();

            foreach (var stage in Workflow.GetAllStages())
            {
                var locked = stage is DraftingStage || stage is ApprovalStage;
                var endOfWorkflow = stage is ApprovalStage;

                var icon = IconMapper.GetIconForStageType(stage.StageType)?.Glyph;
                var viewmodel = new WorkflowStageCardViewModel(stage, Workflow, locked, endOfWorkflow, icon,
                    UpdateStageCard, ViewModelContextProvider, _projectName);
                i.Add(viewmodel);

                viewmodel.DeleteStageCommand = ReactiveCommand.CreateFromTask(async () => { await DeleteStageAsync(stage); });

                Disposables.Add(viewmodel.DeleteStageCommand.ThrownExceptions.Subscribe(async exception =>
                {
                    await ErrorManager.ShowErrorPopupAsync(ViewModelContextProvider, exception);

                    UndoDeleteStage();
                }));

                viewmodel.AddStageCommand = ReactiveCommand.CreateFromTask<StageTypes>(async stageType => { await AddStageAsync(stageType, stage.Id); });

                Disposables.Add(viewmodel.AddStageCommand.ThrownExceptions
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Subscribe(async exception =>
                    {
                        await ErrorManager.ShowErrorPopupAsync(ViewModelContextProvider, exception);

                        UndoAddStage();
                    }));
            }
        });
    }

    private void CreateStageTypes()
    {
        var peerReviewStages = new StageTypeCardViewModel(StageTypes.PeerCheck, ViewModelContextProvider, Icon.PeerReview);

        var communityCheckStages = new StageTypeCardViewModel(StageTypes.CommunityTest, ViewModelContextProvider,
            Icon.CommunityCheck);

        var consultantReviewStages = new StageTypeCardViewModel(StageTypes.ConsultantCheck, ViewModelContextProvider,
            Icon.ConsultantCheckOriginal);

        StagesTypes.AddRange(new List<StageTypeCardViewModel>{peerReviewStages, communityCheckStages, consultantReviewStages});
    }

    private async Task AddStageAsync(StageTypes stageType, Guid stageIdBeforeNewStage)
    {
        Stage stage;
        switch (stageType)
        {
            case StageTypes.PeerCheck:
                stage = PeerCheckStage.Create();
                break;
            case StageTypes.CommunityTest:
                stage = CommunityTestStage.Create();
                break;
            case StageTypes.ConsultantCheck:
                stage = ConsultantCheckStage.Create();
                break;
            default:
                //This will need to be refactored into the static create paradigm for serialization
                stage = new Stage();
                stage.SetName("Generic");
                break;
        }

        _lastAddedStage = stage;

        Workflow.InsertStage(stage, stageIdBeforeNewStage);
        CreateStageCards();

        await SaveWorkflowAsync().ConfigureAwait(false);

        LogInfo("Stage Added To Workflow", new Dictionary<string, string>
        {
            { "Stage Type", stageType.ToString() },
            { "StageId Before New Stage", stageIdBeforeNewStage.ToString() }
        });
    }
    
    private async Task DeleteStageAsync(Stage stage)
    {
        var modalService = ViewModelContextProvider.GetModalService();
        
        var deleteStageComponent = new StageDeleteConfirmationComponentViewModel(ViewModelContextProvider);
        
        var confirmationModal = new ModalViewModel(
            ViewModelContextProvider, 
            modalService, 
            Icon.DeleteWarning, 
            AppResources.WorkflowDeleteStageTitle,
            deleteStageComponent,
            new ModalButtonViewModel(AppResources.Cancel),
            new ModalButtonViewModel(AppResources.Proceed))
        {
             BeforeConfirmCommand = deleteStageComponent.ContinueCommand
        };

        var result = await modalService.ConfirmationModal(confirmationModal);

        if (result != DialogResult.Ok)
        {
            return;
        }
        
        _originalStageState = stage.State;
        _stageForDelete = stage;

        Workflow.DeactivateStage(stage, deleteStageComponent.StageState);

        CreateStageCards();

        await SaveWorkflowAsync().ConfigureAwait(false);

        LogInfo("Stage Deleted", new Dictionary<string, string>
        {
            { "Stage Id", stage.Id.ToString() }
        });
    }

    private async Task SaveWorkflowAsync()
    {
        await _workflowPersistence.SaveWorkflowAsync(Workflow);
        var grandCentralStation = ViewModelContextProvider.GetGrandCentralStation();
        grandCentralStation.UpdateWorkflow(Workflow);
    }

    private async Task<IRoutableViewModel> NavigateHome()
    {
        try
        {
            var vm = await Task.Run(async () => await HomeViewModel.CreateAsync(Workflow.ProjectId, ViewModelContextProvider));
            return await NavigateToAndReset(vm);
        }
        catch (Exception e)
        {
            Logger.LogError(e);
        }

        return null;
    }

    private void UpdateStageCard(Stage stage)
    {
        //there may be multiple same stages in the workflow such as two community test stages
        //so compare Id instead of Name
        var stageCard = StageCards.SingleOrDefault(x => x.Stage.Id == stage.Id);
        stageCard?.InitializeStepList(stage);
    }

    public void UndoDeleteStage()
    {
        if (!_originalStageState.HasValue)
        {
            return;
        }

        Workflow.DeactivateStage(_stageForDelete, _originalStageState.Value);

        CreateStageCards();

        _originalStageState = null;
        _stageForDelete = null;
    }

    private void UndoAddStage()
    {
        if (_lastAddedStage == null)
        {
            return;
        }

        Workflow.RemoveStage(_lastAddedStage);

        CreateStageCards();

        _lastAddedStage = null;
    }
    
}