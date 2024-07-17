using DynamicData;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Render.Kernel;
using Render.Models.Users;
using Render.Models.Workflow;
using Render.Models.Workflow.Stage;
using Render.Resources.Localization;

namespace Render.Pages.Configurator.WorkflowAssignment.Stages;

public class WorkflowCommunityCheckStageColumnViewModel : WorkflowStageColumnViewModel
    {
        [Reactive] public bool Locked { get; set; }

        public WorkflowCommunityCheckStageColumnViewModel(Stage stage, IReadOnlyList<Team> teamList,
            Action<Team> onTeamDeleted,
            Action<Guid, Team> onTranslationTeamUpdate,
            Action<Stage> updateStageColumnCallback,
            IList<IUser> users,
            RenderWorkflow workflow,
            IViewModelContextProvider viewModelContextProvider,
            string projectName) :
            base(stage, workflow, updateStageColumnCallback, viewModelContextProvider, projectName)
        {
            Locked = stage.StageSettings.GetSetting(SettingType.AssignToTranslator);
            foreach (var team in teamList)
            {
                IUser user = null;
                var assignment = team.GetWorkflowAssignmentForStageAndRole(stage.Id, Roles.Review);
                if (assignment != null)
                {
                    user = users.FirstOrDefault(x => x.Id == assignment.UserId);
                }

                _teamListSource.Add(new TabletTeamAssignmentCardViewModel(stage.Id, Roles.Review, team,
                    onTeamDeleted, onTranslationTeamUpdate, workflow, viewModelContextProvider, user, 
                    BuildTeamString(team.TeamNumber), Locked));
            }

            var setting =
                stage.StageSettings.Settings.FirstOrDefault(x => x.SettingType == SettingType.AssignToTranslator);
            Disposables.Add(setting.WhenAnyValue(x => x.Value)
                .Subscribe(b =>
                {
                    Locked = b;
                    foreach (var team in TeamList)
                    {
                        team.Locked = b;
                    }
                }));
        }


        public override void AddTeamToList(
            IList<IUser> users,
            Team team, IUser user,
            Action<Team> onTeamDeleted,
            Action<Guid, Team> onTranslationTeamUpdate)
        {
            _teamListSource.Add(new TabletTeamAssignmentCardViewModel(Stage.Id, Roles.Review, team,
                onTeamDeleted, onTranslationTeamUpdate, Workflow, ViewModelContextProvider, user, 
                BuildTeamString(team.TeamNumber), Locked));
        }
        
        private string BuildTeamString(int teamNumber)
        {
            return string.Format(AppResources.TeamTitle, teamNumber);
        }
    }