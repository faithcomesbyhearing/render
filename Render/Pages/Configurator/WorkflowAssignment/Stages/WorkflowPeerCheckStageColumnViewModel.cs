using DynamicData;
using Render.Kernel;
using Render.Models.Users;
using Render.Models.Workflow;
using Render.Models.Workflow.Stage;
using Render.Resources.Localization;

namespace Render.Pages.Configurator.WorkflowAssignment.Stages;

public class WorkflowPeerCheckStageColumnViewModel : WorkflowStageColumnViewModel
    {
        public WorkflowPeerCheckStageColumnViewModel(Stage stage, IReadOnlyList<Team> teamList,
            Action<Team> onTeamDeleted,
            Action<Guid, Team> onTranslationTeamUpdate,
            Action<Stage> updateStageColumnCallback,
            IList<IUser> users,
            RenderWorkflow workflow,
            IViewModelContextProvider viewModelContextProvider,
            string projectName) : 
            base(stage, workflow, updateStageColumnCallback, viewModelContextProvider, projectName)
        {
            foreach (var team in teamList)
            {
                IUser user = null;
                var assignment = team.GetWorkflowAssignmentForStageAndRole(stage.Id, Roles.Review);
                if (assignment != null)
                {
                    user = users.FirstOrDefault(x => x.Id == assignment.UserId);
                }
                
                _teamListSource.Add(new TabletTeamAssignmentCardViewModel(stage.Id, Roles.Review, team, onTeamDeleted,
                    onTranslationTeamUpdate, Workflow, ViewModelContextProvider, user, BuildTeamString(team.TeamNumber)));
            }
        }
        
        public override void AddTeamToList(IList<IUser> users, Team team, IUser user, 
            Action<Team> onTeamDeleted, 
            Action<Guid, Team> onTranslationTeamUpdate)
        {
            _teamListSource.Add(new TabletTeamAssignmentCardViewModel(Stage.Id, Roles.Review, team, onTeamDeleted, 
                    onTranslationTeamUpdate, Workflow, ViewModelContextProvider, user, BuildTeamString(team.TeamNumber)));
        }

        private string BuildTeamString(int teamNumber)
        {
            return string.Format(AppResources.TeamTitle, teamNumber);
        }
    }