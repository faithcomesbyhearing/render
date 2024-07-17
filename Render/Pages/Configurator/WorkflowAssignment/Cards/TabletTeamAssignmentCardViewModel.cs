using System;
using System.Collections.Generic;
using Render.Resources;
using Render.Kernel;
using Render.Models.Users;
using Render.Models.Workflow;
using Render.Pages.Configurator.WorkflowAssignment.Cards;
using Render.Resources.Localization;

namespace Render.Pages.Configurator.WorkflowAssignment;

public class TabletTeamAssignmentCardViewModel : TeamAssignmentCardViewModel
    {
        public TabletTeamAssignmentCardViewModel(Guid stageId, Roles role, List<Team> teamList,
            Action<Team> onDeleteTeam, 
            Action<Guid, Team> onTranslationTeamUpdate,
            RenderWorkflow workflow, 
            IViewModelContextProvider viewModelContextProvider, 
            IUser user = null, string name = null, bool locked = false, Func<Guid, string, bool> checkMultipleTeams = null) : 
            base(stageId, role, teamList,
                onDeleteTeam, 
                onTranslationTeamUpdate, 
                workflow, viewModelContextProvider, user, name, locked, checkMultipleTeams)
        {
        }

        public TabletTeamAssignmentCardViewModel(Guid stageId, Roles role, Team team,
            Action<Team> onDeleteTeam, 
            Action<Guid, Team> onTranslationTeamUpdate,
            RenderWorkflow workflow, IViewModelContextProvider viewModelContextProvider, 
            IUser user = null, string name = null, bool locked = false, Func<Guid, string, bool> checkMultipleTeams = null) :
            base(stageId, role, team,
                onDeleteTeam, 
                onTranslationTeamUpdate, 
                workflow, viewModelContextProvider, user, name, locked, checkMultipleTeams)
        {
        }
        
        public async void OnDrop(IUser user)
        {
            if (Locked)
            {
                return;
            }

            // for drafting stage, a single user cannot be assigned to multiple teams
            // a warning modal will be displayed to alert user
            if (Role == Roles.Drafting && CheckMultipleTeams != null)
            {
                var result = CheckMultipleTeams.Invoke(user.Id, Name);
                if (result)
                {
                    await ViewModelContextProvider.GetModalService().ShowInfoModal
                    (
                          icon: Icon.TypeWarning,
                          title: AppResources.OneUserOneTeam,
                          message: AppResources.UsersCannotBeAssignedToMultipleTeams
                        );

                    return;
                }
            }

            var success = false;

            foreach (var team in TeamList)
            {
                success = Role == Roles.Drafting
                    ? Workflow.AddTranslationAssignmentForTeam(team, user.Id)
                    : Workflow.AddWorkflowAssignmentToTeam(StageId, Role, user, team);

                if (Role == Roles.Drafting && success)
                {
                    OnTranslationTeamUpdate.Invoke(user.Id, team);
                }
            }

            if (!success)
            {
                return;
            }

            UserCardViewModel = new UserCardViewModel(user, ViewModelContextProvider);
        }
    }