using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Render.Kernel;
using Render.Models.Users;
using Render.Models.Workflow;
using Render.Models.Workflow.Stage;
using Render.Pages.Configurator.WorkflowManagement;
using Render.Resources;
using Render.Resources.Styles;

namespace Render.Pages.Configurator.WorkflowAssignment.Stages;

public abstract class WorkflowStageColumnViewModel : ViewModelBase
{
    private readonly string _projectName;
    public Stage Stage { get; }
    public StageTypes StageType => Stage.StageType;
    public string Name { get; set; }
    public string StageGlyph { get; set; }

    private List<Step> _steps;
    public List<Step> Steps => _steps;
    protected RenderWorkflow Workflow;

    [Reactive] public bool AllWorkAssigned { get; set; } = false;

    public ReadOnlyObservableCollection<TeamAssignmentCardViewModel> TeamList => _teamList;
    protected SourceList<TeamAssignmentCardViewModel> _teamListSource = new SourceList<TeamAssignmentCardViewModel>();
    private readonly ReadOnlyObservableCollection<TeamAssignmentCardViewModel> _teamList;
    private Action<Stage> UpdateStageColumn;
    public readonly ReactiveCommand<Unit, Task<IRoutableViewModel>> OpenStageSettingsCommand;
    public WorkflowStageColumnViewModel(Stage stage, RenderWorkflow workflow,
        Action<Stage> updateStageColumnCallback,
        IViewModelContextProvider viewModelContextProvider, string projectName)
        : base("WorkflowStageColumn", viewModelContextProvider)
    {
        _projectName = projectName;
        Workflow = workflow;
        Stage = stage;
        _steps = stage.GetAllWorkflowEntrySteps();
        Name = Stage.Name;
        StageGlyph = GetStageGlyph();

        Disposables.Add(Stage.WhenAnyValue(x => x.Name)
            .Subscribe(x => Name = x));
        UpdateStageColumn = updateStageColumnCallback;
        OpenStageSettingsCommand = ReactiveCommand.Create(NavigateToStageSettingsPageAsync);
        var changeList = _teamListSource.Connect().Publish();

        Disposables.Add(changeList
            .Sort(SortExpressionComparer<TeamAssignmentCardViewModel>.Ascending(vm => vm.Order))
            .Bind(out _teamList)
            .Subscribe());
        Disposables.Add(changeList
            .WhenPropertyChanged(x => x.UserCardViewModel)
            .Subscribe(vm =>
            {
                AllWorkAssigned = vm.Value != null && TeamList.All(x => x.UserCardViewModel != null);
            }));
        Disposables.Add(changeList.Connect());

    }

    private string GetStageGlyph()
    {
        var color = (ColorReference)ResourceExtensions.GetResourceValue("SecondaryText");
        if (color == null)
        {
            return ((FontImageSource)ResourceExtensions.GetResourceValue("PlaceholderWhite"))?.Glyph;
        }

        var icon = StageType switch
        {
            StageTypes.Drafting => Icon.Record,
            StageTypes.PeerCheck => Icon.PeerReview,
            StageTypes.CommunityTest => Icon.CommunityCheckSetup,
            StageTypes.ConsultantCheck => Icon.ConsultantCheck,
            StageTypes.ConsultantApproval => Icon.ConsultantApproval,
            _ => Icon.Placeholder
        };

        return IconExtensions.BuildFontImageSource(icon, color.Color)?.Glyph;
    }

    public virtual void AddTeamToList(IList<IUser> users, Team team, IUser user,
        Action<Team> onTeamDeleted,
        Action<Guid, Team> onTranslationTeamUpdate)
    {
    }

    public virtual void RemoveTeamFromList(Team team)
    {
        if (TeamList.Any(x => x.TeamList.Contains(team)))
        {
            var teamToRemove = TeamList.First(x => x.TeamList.Contains(team));
            _teamListSource.Remove(teamToRemove);
        }
        AllWorkAssigned = TeamList.All(x => x.UserCardViewModel != null);
    }

    public void UpdateSteps()
    {
        _steps = Stage.GetAllWorkflowEntrySteps();
    }

    private async Task<IRoutableViewModel> NavigateToStageSettingsPageAsync()
    {
        var vm = new WorkflowStageSettingsPageViewModel(Stage, Workflow, ViewModelContextProvider, UpdateStageColumn, _projectName);
        return await NavigateTo(vm);
    }
}