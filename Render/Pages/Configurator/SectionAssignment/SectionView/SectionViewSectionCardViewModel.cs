using System.Reactive;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Render.Kernel;

namespace Render.Pages.Configurator.SectionAssignment.SectionView
{
    public class SectionViewSectionCardViewModel : ViewModelBase
    {
        [Reactive] public TeamSectionAssignment Assignment { get; set; }

        public readonly ReactiveCommand<Unit, Unit> RemoveTeamCommand;
        public readonly ReactiveCommand<Unit, Unit> OnDragLeaveCommand;
        public readonly ReactiveCommand<TeamSectionAssignment, Unit> OnSectionDropCommand;
        public readonly ReactiveCommand<NewSectionViewTeamCardViewModel, Unit> AssignTeamToSectionCommand;

        [Reactive] public bool ShowTeam { get; set; }

        [Reactive] public NewSectionViewTeamCardViewModel SectionViewTeamCardViewModel { get; set; }

        [Reactive] public bool ShowDragOverSection { get; set; }

        private readonly Func<TeamSectionAssignment, Task> _updateWorkflowCallback;
        private readonly Func<TeamSectionAssignment, Task> _reorderSectionsCallback;
        
        public SectionViewSectionCardViewModel(IViewModelContextProvider viewModelContextProvider,
            TeamSectionAssignment assignment, TeamTranslatorUser teamTranslatorUser,
            Func<TeamSectionAssignment, Task> updateWorkflowCallback, 
            Func<TeamSectionAssignment, Task> reorderSectionsCallback)
            : base("SectionViewSectionCard", viewModelContextProvider)
        {
            Assignment = assignment;
            
            RemoveTeamCommand = ReactiveCommand.CreateFromTask(RemoveTeam);
            OnDragLeaveCommand = ReactiveCommand.Create(OnDragLeave);
            OnSectionDropCommand = ReactiveCommand.CreateFromTask<TeamSectionAssignment>(OnSectionDrop);
            AssignTeamToSectionCommand = ReactiveCommand.CreateFromTask<NewSectionViewTeamCardViewModel>(OnAssignTeamToSection);

            _updateWorkflowCallback = updateWorkflowCallback;
            _reorderSectionsCallback = reorderSectionsCallback;
            
            if (teamTranslatorUser != null)
            {
                SectionViewTeamCardViewModel = new NewSectionViewTeamCardViewModel(viewModelContextProvider, teamTranslatorUser);
                ShowTeam = true;
            }

            Disposables.Add(Assignment.WhenAnyValue(x => x.Team)
                .Subscribe(t =>
                {
                    if (t != null)
                    {
                        SectionViewTeamCardViewModel = new NewSectionViewTeamCardViewModel(viewModelContextProvider, t);
                        ShowTeam = true;
                    }
                    else
                    {
                        SectionViewTeamCardViewModel = null;
                        ShowTeam = false;
                    }
                }));
            
            Disposables.Add(RemoveTeamCommand.ThrownExceptions.Subscribe(async exception =>
            {
                await ErrorManager.ShowErrorPopupAsync(viewModelContextProvider, exception);
            }));
            
            Disposables.Add(OnSectionDropCommand.ThrownExceptions.Subscribe(async exception =>
            {
                await ErrorManager.ShowErrorPopupAsync(viewModelContextProvider, exception);
            }));
            
            Disposables.Add(AssignTeamToSectionCommand.ThrownExceptions.Subscribe(async exception =>
            {
                await ErrorManager.ShowErrorPopupAsync(viewModelContextProvider, exception);
            }));
        }

        public void OnDragOver()
        {
            ShowDragOverSection = true;
        }

        private void OnDragLeave()
        {
            ShowDragOverSection = false;
        }

        private async Task RemoveTeam()
        {
            Assignment.Team = null;
            ShowTeam = false;

            await _updateWorkflowCallback(Assignment);
        }
        
        private async Task OnAssignTeamToSection(NewSectionViewTeamCardViewModel sectionViewTeam)
        {
            Assignment.Team = sectionViewTeam.Team;
            SectionViewTeamCardViewModel = sectionViewTeam;
            ShowTeam = true;

            await _updateWorkflowCallback(Assignment);
        }

        private async Task OnSectionDrop(TeamSectionAssignment assignment)
        {
            OnDragLeave();

            if (assignment == Assignment)
            {
                return;
            }

            assignment.Priority = Assignment.Priority > 0 ? Assignment.Priority + 1 : 0;

            await _reorderSectionsCallback(assignment);
        }
    }
}