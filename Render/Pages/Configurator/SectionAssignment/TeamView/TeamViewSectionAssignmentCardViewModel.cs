using System.Reactive;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Render.Kernel;
using Render.Models.Sections;

namespace Render.Pages.Configurator.SectionAssignment.TeamView
{
    public class TeamViewSectionAssignmentCardViewModel : ViewModelBase
    {
        private readonly Func<TeamSectionAssignment, Task> _updateWorkflowCallback;
        private readonly Func<TeamSectionAssignment, Task> _reorderSectionsCallback;
        
        [Reactive]
        public Section Section { get; set; }
        
        private TeamTranslatorUser Team { get; set; }
        
        [Reactive]
        public TeamSectionAssignment Assignment { get; set; }
        
        [Reactive]
        public bool ShowSection { get; set; }
        
        [Reactive]
        public bool ShowSectionDropZone { get; set; }

        public ReactiveCommand<TeamSectionAssignment, Unit> OnSectionDropCommand { get; set; }
        public ReactiveCommand<Unit, Unit> OnDragLeaveCommand { get; set; }
        public ReactiveCommand<Unit, Unit> RemoveAssignmentCommand { get; set; }
        public ReactiveCommand<TeamSectionAssignment, Unit> AssignSectionCommand { get; set; }

        public TeamViewSectionAssignmentCardViewModel(IViewModelContextProvider viewModelContextProvider,
            TeamTranslatorUser team,
            List<TeamSectionAssignment> teamViewSectionAssignments, Func<TeamSectionAssignment, Task> updateWorkflowCallback, 
            Func<TeamSectionAssignment, Task> reorderSectionsCallback,
            TeamSectionAssignment assignment = null)
        : base("TeamViewSectionAssignment", viewModelContextProvider)
        {
            _updateWorkflowCallback = updateWorkflowCallback;
            _reorderSectionsCallback = reorderSectionsCallback;
            
            if (assignment == null)
            {
                assignment = new TeamSectionAssignment(null, team, 9999);
            }

            Section = assignment.Section;
            Team = team;
            Assignment = assignment;
            ShowSection = Assignment.Section != null;
            
            RemoveAssignmentCommand = ReactiveCommand.CreateFromTask(RemoveAssignment);
            OnDragLeaveCommand = ReactiveCommand.Create(OnDragLeave);
            OnSectionDropCommand = ReactiveCommand.CreateFromTask<TeamSectionAssignment>(OnSectionDrop);
            AssignSectionCommand = ReactiveCommand.CreateFromTask<TeamSectionAssignment>(OnAssignSection);
            
            Disposables.Add(RemoveAssignmentCommand.ThrownExceptions.Subscribe(async exception =>
            {
                await ErrorManager.ShowErrorPopupAsync(viewModelContextProvider, exception);
            }));
            
            Disposables.Add(OnSectionDropCommand.ThrownExceptions.Subscribe(async exception =>
            {
                await ErrorManager.ShowErrorPopupAsync(viewModelContextProvider, exception);
            }));
            
            Disposables.Add(AssignSectionCommand.ThrownExceptions.Subscribe(async exception =>
            {
                await ErrorManager.ShowErrorPopupAsync(viewModelContextProvider, exception);
            }));
        }

        public void OnDragOver()
        {
            ShowSectionDropZone = true;
        }

        private void OnDragLeave()
        {
            ShowSectionDropZone = false;
        }
        
        private async Task OnAssignSection(TeamSectionAssignment sectionAssignment)
        {
            sectionAssignment.Team = Team;
            Assignment = sectionAssignment;
            Section = Assignment.Section;
            ShowSection = true;

            await _updateWorkflowCallback(sectionAssignment);
        }

        private async Task RemoveAssignment()
        {
            Assignment.Team = null;
            
            await _updateWorkflowCallback(Assignment);
            
            Assignment = null;
            ShowSection = false;
        }
        
        private async Task OnSectionDrop(TeamSectionAssignment team)
        {
            OnDragLeave();
            if(team == Assignment) return;
            var priority = Assignment.Priority > 0 ? Assignment.Priority : 0;
            team.Priority = priority;
            
            await _reorderSectionsCallback(team);
        }
    }
}