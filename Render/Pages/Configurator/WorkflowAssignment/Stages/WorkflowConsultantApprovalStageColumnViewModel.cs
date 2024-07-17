using DynamicData;
using Render.Kernel;
using Render.Models.Users;
using Render.Models.Workflow;
using Render.Models.Workflow.Stage;

namespace Render.Pages.Configurator.WorkflowAssignment.Stages;

public class WorkflowConsultantApprovalStageColumnViewModel : WorkflowStageColumnViewModel
    {
        public WorkflowConsultantApprovalStageColumnViewModel(Stage stage, IReadOnlyList<Team> teamList,
            Action<Team> onTeamDeleted,
            Action<Guid, Team> onTranslationTeamUpdate,
            Action<Stage> updateStageColumnCallback,
            IList<IUser> users,
            RenderWorkflow workflow,
            IViewModelContextProvider viewModelContextProvider,
            string projectName) : 
            base(stage, workflow, updateStageColumnCallback, viewModelContextProvider, projectName)
        {
            IUser user = null;
            var team = teamList.FirstOrDefault();
            if (team != null)
            {
                var assignment = team.GetWorkflowAssignmentForStageAndRole(stage.Id, Roles.Approval);
                if (assignment != null)
                {
                    user = users.FirstOrDefault(x => x.Id == assignment.UserId);
                }
                _teamListSource.Add(new TabletTeamAssignmentCardViewModel(stage.Id, Roles.Approval, 
                    new List<Team>(teamList), 
                    onTeamDeleted, onTranslationTeamUpdate, 
                    workflow, viewModelContextProvider, user));
            }
        }

        public override void AddTeamToList(IList<IUser> users, Team team, IUser user,
            Action<Team> onTeamDeleted,
            Action<Guid, Team> onTranslationTeamUpdate)
        {
            if(TeamList.Any())
            {
                TeamList.First().TeamList.Add(team);
            }
        }

        public override void RemoveTeamFromList(Team team)
        {
            if (TeamList.Any())
            {
                TeamList.First().TeamList.Remove(team);
            }
        }
    }