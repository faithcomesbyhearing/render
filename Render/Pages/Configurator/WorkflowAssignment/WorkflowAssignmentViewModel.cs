using System.Collections.ObjectModel;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Binding;
using Newtonsoft.Json;
using ReactiveUI;
using Render.Components.Scroller;
using Render.Kernel;
using Render.Kernel.WrappersAndExtensions;
using Render.Models.Users;
using Render.Models.Workflow;
using Render.Models.Workflow.Stage;
using Render.Pages.AppStart.Home;
using Render.Pages.Configurator.WorkflowAssignment.Cards;
using Render.Pages.Configurator.WorkflowAssignment.Stages;
using Render.Repositories.WorkflowRepositories;
using Render.Resources;
using Render.Resources.Localization;
using Render.Resources.Styles;
using Render.TempFromVessel.Project;

namespace Render.Pages.Configurator.WorkflowAssignment;

public class WorkflowAssignmentViewModel : WorkflowPageBaseViewModel
{
    private RenderWorkflow Workflow { get; }
    private readonly IWorkflowRepository _workflowPersistence;
    private readonly List<Team> _initialTeamsAssignmentsSnapshot;
    private readonly string _projectName;

    private SourceList<WorkflowStageColumnViewModel> _stageColumnSource =
        new SourceList<WorkflowStageColumnViewModel>();

    private ReadOnlyObservableCollection<WorkflowStageColumnViewModel> _stageColumns;
    public ReadOnlyObservableCollection<WorkflowStageColumnViewModel> StageColumns => _stageColumns;

    private WorkflowDraftStageColumnViewModel _draftStage;

    private List<IUser> _users;
    public DynamicDataWrapper<UserCardViewModel> Users = new DynamicDataWrapper<UserCardViewModel>();
    //private readonly IModalManager _modalManager;

    public ScrollerViewModel ScrollerViewModel { get; private set; }

    public static async Task<WorkflowAssignmentViewModel> CreateAsync(
    IViewModelContextProvider viewModelContextProvider, Guid projectId)
    {
        var projectRepository = viewModelContextProvider.GetPersistence<Project>();
        var project = await projectRepository.GetAsync(projectId);
        var userRepository = viewModelContextProvider.GetUserRepository();
        var usersForProject = await userRepository.GetUsersForProjectAsync(project);
        var workflowRepository = viewModelContextProvider.GetWorkflowRepository();
        var workflow = await workflowRepository.GetWorkflowForProjectIdAsync(projectId);

        var workflowAssignmentViewModel =
            new WorkflowAssignmentViewModel(viewModelContextProvider, project.Name, usersForProject, workflow);
        return workflowAssignmentViewModel;
    }

    private WorkflowAssignmentViewModel(IViewModelContextProvider viewModelContextProvider, string projectName,
            List<IUser> users,
            RenderWorkflow workflow)
            : base(
                "WorkflowAssignment",
                viewModelContextProvider,
                pageName: AppResources.AssignRoles,
                null,
                null,
                secondPageName: projectName
                )
    {
        _projectName = projectName;
        Workflow = workflow;
        _users = users.OrderBy(x => x.Username).ToList();
        _workflowPersistence = viewModelContextProvider.GetWorkflowRepository();

        var color = ResourceExtensions.GetColor("SecondaryText");
        if (color != null)
        {
            TitleBarViewModel.PageGlyph = IconExtensions.BuildFontImageSource(Icon.AssignRoles, color)?.Glyph;
        }

        ProceedButtonViewModel.SetCommand(NavigateHomeAsync);

        Disposables.Add(ProceedButtonViewModel.NavigateToPageCommand.IsExecuting
            .Subscribe(isExecuting =>
            {
                IsLoading = isExecuting;
            }));

        foreach (var user in _users)
        {
            Users.Add(new UserCardViewModel(user, viewModelContextProvider));
        }

        TitleBarViewModel.SetNavigationCondition(AllowNavigation);

        var changeList = _stageColumnSource.Connect().Publish();

        //Creating a copy of initial assignments to check if any changes were made when leaving the page
        var teamsJson = JsonConvert.SerializeObject(workflow.GetTeams());
        _initialTeamsAssignmentsSnapshot = JsonConvert.DeserializeObject<List<Team>>(teamsJson);

        Disposables.Add(changeList
            .ObserveOn(RxApp.MainThreadScheduler)
            .Bind(out _stageColumns)
            .Subscribe());
        Disposables.Add(changeList.Connect());

        CreateStageTypes(_users);

        Disposables.Add(_stageColumns
            .ToObservableChangeSet()
            .MergeMany(item => item.OpenStageSettingsCommand.IsExecuting)
            .Subscribe(isExecuting => { IsLoading = isExecuting; }));

        ScrollerViewModel = new ScrollerViewModel(viewModelContextProvider);
    }

    /// <summary>
    /// Dynamic data WorkflowStageColumnViewModel
    /// Subclass specific view models
    /// Use data template selector and feed view model to choose which template to use for the columns
    /// </summary>
    /// <param name="users"></param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    private void CreateStageTypes(IList<IUser> users)
    {
        _stageColumnSource.Clear();

        var stages = Workflow.GetAllStages();
        foreach (var stage in stages)
        {
            var stageType = stage.StageType;

            var teamList = Workflow.GetTeams().OrderBy(x => x.TeamNumber).ToList();
            switch (stageType)
            {
                case StageTypes.Generic:
                    break;
                case StageTypes.Drafting:
                    _draftStage = new WorkflowDraftStageColumnViewModel(stage, teamList,
                        DeleteTeam,
                        AddTeamAsync,
                        UpdateTranslationTeam,
                        UpdateStageColumn,
                        users, Workflow,
                        ViewModelContextProvider,
                        _projectName);
                    _stageColumnSource.Add(_draftStage);
                    break;
                case StageTypes.PeerCheck:
                    var peerCheck = new WorkflowPeerCheckStageColumnViewModel(stage, teamList,
                        DeleteTeam,
                        UpdateTranslationTeam,
                        UpdateStageColumn,
                        users, Workflow,
                        ViewModelContextProvider,
                        _projectName);
                    _stageColumnSource.Add(peerCheck);
                    break;
                case StageTypes.CommunityTest:
                    var communityCheck = new WorkflowCommunityCheckStageColumnViewModel(stage, teamList,
                        DeleteTeam,
                        UpdateTranslationTeam,
                        UpdateStageColumn,
                        users, Workflow,
                        ViewModelContextProvider,
                        _projectName);
                    _stageColumnSource.Add(communityCheck);
                    break;
                case StageTypes.ConsultantCheck:
                    var consultantCheck = new WorkflowConsultantCheckStageColumnViewModel(stage, teamList,
                        DeleteTeam,
                        UpdateTranslationTeam,
                        UpdateStageColumn,
                        users, Workflow,
                        ViewModelContextProvider,
                        _projectName);
                    _stageColumnSource.Add(consultantCheck);
                    break;
                case StageTypes.ConsultantApproval:
                    var consultantApproval = new WorkflowConsultantApprovalStageColumnViewModel(stage, teamList,
                        DeleteTeam,
                        UpdateTranslationTeam,
                        UpdateStageColumn,
                        users, Workflow,
                        ViewModelContextProvider,
                        _projectName);
                    _stageColumnSource.Add(consultantApproval);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(stageType), stageType, null);
            }
        }
    }

    private async Task<bool> AllowNavigation()
    {
        if (CheckIfChangesMade())
        {
            var result = await OpenUnsavedRolesDialog();

            if (result == DialogResult.Ok || result == DialogResult.None)
            {
                return false;
            }
        }

        return true;
    }

    private void UpdateTranslationTeam(Guid userId, Team team)
    {
        var communityStages = StageColumns.Where(x => x.StageType == StageTypes.CommunityTest);

        foreach (var stageColumn in communityStages)
        {
            var teamAssignmentCard = stageColumn.TeamList.First(x => x.TeamList.Contains(team));
            if (stageColumn.Stage.StageSettings.GetSetting(SettingType.AssignToTranslator))
            {
                if (team.TranslatorId == userId)
                {
                    var user = _users.FirstOrDefault(x => x.Id == userId);
                    var userCardViewModel = new UserCardViewModel(user, ViewModelContextProvider);
                    teamAssignmentCard.UserCardViewModel = userCardViewModel;
                }
                else
                {
                    teamAssignmentCard.UserCardViewModel = null;
                }
            }
        }

        LogInfo("Translation Team Assigned", new Dictionary<string, string>
            {
                {"TeamId", team.Id.ToString()},
                {"UserId", userId.ToString()},
            });
    }

    private async Task SaveWorkflowAsync()
    {
        await _workflowPersistence.SaveWorkflowAsync(Workflow);
        ViewModelContextProvider.GetWorkflowService().UpdateWorkflow(Workflow);
    }

    private bool CheckIfChangesMade()
    {
        var currentTeams = Workflow.GetTeams();

        var teamsChanged = currentTeams.Count != _initialTeamsAssignmentsSnapshot.Count
            || currentTeams.Any(t => _initialTeamsAssignmentsSnapshot.All(it => it.Id != t.Id));

        if (teamsChanged)
            return true;

        foreach (var team in currentTeams)
        {
            var initialTeamSnapshot = _initialTeamsAssignmentsSnapshot.Single(t => t.TeamNumber == team.TeamNumber);

            if (team.TranslatorId != initialTeamSnapshot.TranslatorId
                || team.WorkflowAssignments.Count != initialTeamSnapshot.WorkflowAssignments.Count)
            {
                return true;
            }

            var teamAssignmentsChanged = team.WorkflowAssignments
                .Any(a => !initialTeamSnapshot.WorkflowAssignments
                    .Any(w => w.StageType == a.StageType && w.Role == a.Role && w.UserId == a.UserId));

            if (teamAssignmentsChanged)
            {
                return true;
            }
        }

        return false;
    }

    private async Task<DialogResult> OpenUnsavedRolesDialog()
    {
        return await ViewModelContextProvider.GetModalService().ConfirmationModal(
            icon: Icon.TypeWarning,
            title: AppResources.SaveWorkAndProceed,
            message: AppResources.SaveWorkAndProceedMessage,
            cancelText: AppResources.ExitAnyway,
            confirmText: AppResources.OK
            );
    }

    private void DeleteTeam(Team team)
    {
        Workflow.RemoveTeam(team);
        foreach (var stageColumn in StageColumns)
        {
            stageColumn.RemoveTeamFromList(team);
        }

        LogInfo("Translation Team Deleted", new Dictionary<string, string>
            {
                {"TeamId", team.Id.ToString()}
            });
    }

    private void AddTeamAsync()
    {
        var team = Workflow.AddTeam();

        foreach (var stageColumn in StageColumns)
        {
            stageColumn.AddTeamToList(
                _users,
                team,
                null,
                DeleteTeam,
                UpdateTranslationTeam);
        }
    }

    private async Task<IRoutableViewModel> NavigateHomeAsync()
    {
        try
        {
            await SaveWorkflowAsync();

            var viewModel = await HomeViewModel.CreateAsync(Workflow.ProjectId, ViewModelContextProvider);

            return await NavigateToAndReset(viewModel);
        }
        catch (Exception e)
        {
            Logger.LogError(e);
        }

        return null;
    }

    private void UpdateStageColumn(Stage stage)
    {
        var stageColumn = StageColumns.SingleOrDefault(x => x.Stage.Id == stage.Id);
        if (stageColumn != null)
        {
            stageColumn.UpdateSteps();
            if (stage.StageType == StageTypes.CommunityTest)
            {
                var assignToTranslator = stage.StageSettings.GetSetting(SettingType.AssignToTranslator);
                var teams = stageColumn.TeamList;
                foreach (var team in teams)
                {
                    team.UpdateLock(assignToTranslator);
                }
            }
        }
        CreateStageTypes(_users);
    }

    public override void Dispose()
    {
        _stageColumnSource?.Dispose();
        _stageColumnSource = null;
        _stageColumns = null;

        _users = null;
        Users?.Dispose();
        Users = null;

        ScrollerViewModel?.Dispose();
        ScrollerViewModel = null;

        base.Dispose();
    }
}