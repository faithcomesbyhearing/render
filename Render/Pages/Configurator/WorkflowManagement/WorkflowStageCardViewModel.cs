using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;
using DynamicData;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Render.Kernel;
using Render.Models.Workflow;
using Render.Models.Workflow.Stage;

namespace Render.Pages.Configurator.WorkflowManagement;

public class WorkflowStageCardViewModel : ViewModelBase
{
    private readonly string _projectName;

    public Stage Stage { get; }
    public RenderWorkflow Workflow { get; set; }

    public bool Locked { get; private set; }

    public bool EndOfWorkflow { get; private set; }

    [Reactive] public string Name { get; set; }

    [Reactive] public bool ShowAddStepAfterCard { get; set; }

    public bool ShowSettingsIcon { get; set; } = true;

    public string StageIcon { get; }

    private readonly SourceList<Step> _stepListSource = new();
    private readonly ReadOnlyObservableCollection<StepLabelViewModel> _stepList;
    public ReadOnlyObservableCollection<StepLabelViewModel> StepList => _stepList;

    public readonly ReactiveCommand<Unit, Unit> OnDragOverCommand;
    public readonly ReactiveCommand<Unit, Unit> OnDragLeaveCommand;
    public ReactiveCommand<Unit, Unit> DeleteStageCommand;
    public ReactiveCommand<StageTypes, Unit> AddStageCommand;
    public readonly ReactiveCommand<Unit, IRoutableViewModel> OpenStageSettingsCommand;

    private readonly Action<Stage> _updateStageColumn;

    public WorkflowStageCardViewModel(
        Stage stage,
        RenderWorkflow workflow,
        bool locked,
        bool endOfWorkflow,
        string stageIcon,
        Action<Stage> openStageSettingsCallback,
        IViewModelContextProvider viewModelContextProvider,
        string projectName)
        : base(
            urlPathSegment: "WorkflowStageCard",
            viewModelContextProvider: viewModelContextProvider)
    {
        _projectName = projectName;
        Stage = stage;
        Locked = locked;
        EndOfWorkflow = endOfWorkflow;
        Name = Stage.Name;
        StageIcon = stageIcon;
        OnDragOverCommand = ReactiveCommand.Create(OnDragOver);
        OnDragLeaveCommand = ReactiveCommand.Create(OnDragLeave);
        _updateStageColumn = openStageSettingsCallback;
        Workflow = workflow;
        OpenStageSettingsCommand = ReactiveCommand.CreateFromTask(NavigateToStageSettingsPageAsync);

        Disposables.Add(_stepListSource.Connect()
            .Transform(step =>
                new StepLabelViewModel(step, viewModelContextProvider, stage))
            .Bind(out _stepList)
            .Subscribe());

        Disposables.Add(Stage.WhenAnyValue(x => x.Name)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(x => Name = x));

        InitializeStepList(Stage);

        Disposables.Add(StepList.WhenAnyValue(x => x.Count)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Where(count => count > 0)
            .Subscribe(_ =>
            {
                StepList.Last().ShowSeparator = false;
            }));
    }

    public void InitializeStepList(Stage stage)
    {
        var steps = stage.GetAllWorkflowEntrySteps();
        var distinctSteps = new List<Step>();

        distinctSteps.AddRange(steps.Where(step => distinctSteps.All(x => x.RenderStepType != step.RenderStepType)));

        ViewModelContextProvider.GetEssentials().InvokeOnMainThread(() =>
        {
            _stepListSource.Clear();
            _stepListSource.AddRange(distinctSteps);
        });
    }

    private void OnDragLeave()
    {
        ShowAddStepAfterCard = false;
    }

    private void OnDragOver()
    {
        ShowAddStepAfterCard = true;
    }

    private async Task<IRoutableViewModel> NavigateToStageSettingsPageAsync()
    {
        var workflowStageSettingsPageViewModel = new WorkflowStageSettingsPageViewModel(Stage, Workflow, ViewModelContextProvider,
            _updateStageColumn, _projectName);
        return await NavigateTo(workflowStageSettingsPageViewModel);
    }
}