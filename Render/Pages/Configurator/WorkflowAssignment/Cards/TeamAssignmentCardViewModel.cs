using System.Reactive;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Render.Kernel;
using Render.Models.Users;
using Render.Models.Workflow;
using Render.Pages.Configurator.WorkflowAssignment.Cards;

namespace Render.Pages.Configurator.WorkflowAssignment;

public abstract class TeamAssignmentCardViewModel : ViewModelBase
    {
        public List<Team> TeamList { get; }
        public string Name { get; }
        
        public int Order { get; }

        public Guid StageId { get; }

        public Roles Role { get; }

        [Reactive] public bool ShowDeleteButton { get; private set; }

        private readonly Action<Team> _onDeleteTeam;
        protected readonly Action<Guid, Team> OnTranslationTeamUpdate;
        protected Func<Guid, string, bool> CheckMultipleTeams;

        [Reactive] public UserCardViewModel UserCardViewModel { get; set; }

        [Reactive] public bool ShowUserCard { get; set; }

        public ReactiveCommand<Unit, Unit> RemoveAssignmentCommand { get; }
        public ReactiveCommand<Unit, Unit> RemoveTeamCommand { get; }

        protected readonly RenderWorkflow Workflow;

        [Reactive] public bool Locked { get;  set; }

        protected TeamAssignmentCardViewModel(Guid stageId,
            Roles role,
            List<Team> teamList,
            Action<Team> onDeleteTeam,
            Action<Guid, Team> onTranslationTeamUpdate,
            RenderWorkflow workflow,
            IViewModelContextProvider viewModelContextProvider,
            IUser user = null, string name = null, bool locked = false,
            Func<Guid, string, bool> checkMultipleTeams = null) :
            base("TeamAssignmentCard", viewModelContextProvider)
        {
            Workflow = workflow;
            Locked = locked;
            StageId = stageId;
            Role = role;
            TeamList = teamList;
            Name = name ?? "";

            if (int.TryParse(Name.Split(' ').LastOrDefault(), out int order))
            {
                Order = order;
            }
            
            if (user != null)
            {
                UserCardViewModel = new UserCardViewModel(user, viewModelContextProvider);
            }

            _onDeleteTeam = onDeleteTeam;
            OnTranslationTeamUpdate = onTranslationTeamUpdate;
            CheckMultipleTeams = checkMultipleTeams;
            this.WhenAnyValue(x => x.UserCardViewModel).Subscribe(s =>
            {
                ShowUserCard = s != null;
            });
            RemoveAssignmentCommand = ReactiveCommand.Create(RemoveAssignment);
            RemoveTeamCommand = ReactiveCommand.Create(DeleteTeam);
        }

        protected TeamAssignmentCardViewModel(Guid stageId, Roles role, Team team,
            Action<Team> onDeleteTeam,
            Action<Guid, Team> onTranslationTeamUpdate,
            RenderWorkflow workflow,
            IViewModelContextProvider viewModelContextProvider, IUser user = null, string name = null,
            bool locked = false, Func<Guid, string, bool> checkMultipleTeams = null) :
            this(stageId, role, new List<Team> { team }, onDeleteTeam, onTranslationTeamUpdate,
                workflow,
                viewModelContextProvider, user, name, locked)
        {
            CheckMultipleTeams = checkMultipleTeams;
        }

        public void UpdateLock(bool isLocked)
        {
            Locked = isLocked;
        }
        
        public virtual void ShowHideDeleteButton(bool show)
        {
            if (Role != Roles.Drafting)
            {
                ShowDeleteButton = false;
                return;
            }

            ShowDeleteButton = show;
        }

        private void RemoveAssignment()
        {
            if (Locked)
            { 
                return; 
            }

            foreach (var team in TeamList)
            {
                if (Role == Roles.Drafting)
                {
                    var userId = team.TranslatorId;
                    Workflow.RemoveTranslationTeamAssignment(team);
                    OnTranslationTeamUpdate.Invoke(userId, team);
                }
                else
                {
                    Workflow.RemoveWorkflowAssignmentFromTeam(StageId, Role, team);
                }
            }

            UserCardViewModel = null;
        }


        private void DeleteTeam()
        {
            if (Role != Roles.Drafting)
                return;
            _onDeleteTeam.Invoke(TeamList.First());
        }
    }