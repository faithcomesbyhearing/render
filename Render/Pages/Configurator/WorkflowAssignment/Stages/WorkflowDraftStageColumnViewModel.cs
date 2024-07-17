using System.Reactive;
using DynamicData;
using DynamicData.Kernel;
using ReactiveUI;
using Render.Kernel;
using Render.Models.Users;
using Render.Models.Workflow;
using Render.Models.Workflow.Stage;
using Render.Resources.Localization;

namespace Render.Pages.Configurator.WorkflowAssignment.Stages;

public class WorkflowDraftStageColumnViewModel : WorkflowStageColumnViewModel
{
    public ReactiveCommand<Unit, Unit> AddTeamCommand { get; }
    private Action _addTeam;

    public WorkflowDraftStageColumnViewModel(Stage stage, IReadOnlyList<Team> teamList,
        Action<Team> onTeamDeleted,
        Action onTeamAdded,
        Action<Guid, Team> onTranslationTeamUpdate,
        Action<Stage> updateStageColumnCallback,
        IList<IUser> users,
        RenderWorkflow workflow,
        IViewModelContextProvider viewModelContextProvider,
        string projectName)
        : base(stage, workflow, updateStageColumnCallback, viewModelContextProvider, projectName)
    {
        _addTeam = onTeamAdded;
        foreach (var team in teamList)
        {
            IUser user = null;
            if (team.TranslatorId != Guid.Empty)
            {
                user = users.FirstOrDefault(x => x.Id == team.TranslatorId);
            }

            _teamListSource.Add(new TabletTeamAssignmentCardViewModel(stage.Id, Roles.Drafting, team, onTeamDeleted,
                    onTranslationTeamUpdate, workflow, ViewModelContextProvider, user,
                    string.Format(AppResources.TeamTitle, team.TeamNumber), checkMultipleTeams: CheckMultipleTeams));
        }

        this.WhenAnyValue(x => x.TeamList.Count)
            .Subscribe(c =>
            {
                if (c <= 1)
                {
                    foreach (var team in TeamList)
                    {
                        team.ShowHideDeleteButton(false);
                    }
                }
                else
                {
                    foreach (var team in TeamList)
                    {
                        team.ShowHideDeleteButton(true);
                    }
                }
            });
        AddTeamCommand = ReactiveCommand.Create(AddTeam);
    }

    private void AddTeam()
    {
        _addTeam.Invoke();
    }

    // make it public for unit test
    public bool CheckMultipleTeams(Guid userId, string teamName)
    {
        //for drafting stage, a single user cannot be assigned to multiple teams
        var otherTeams = TeamList.AsList().Where(x => x.Name != teamName);
        return otherTeams.Any(x => x.UserCardViewModel != null && x.UserCardViewModel.User.Id == userId);
        ;
    }

    public override void AddTeamToList(IList<IUser> users, Team team, IUser user, Action<Team> onTeamDeleted,
        Action<Guid, Team> onTranslationTeamUpdate)
    {
        _teamListSource.Add(new TabletTeamAssignmentCardViewModel(Stage.Id, Roles.Drafting, team, onTeamDeleted,
                onTranslationTeamUpdate, Workflow, ViewModelContextProvider, user,
                string.Format(AppResources.TeamTitle, team.TeamNumber), checkMultipleTeams: CheckMultipleTeams));
    }
}