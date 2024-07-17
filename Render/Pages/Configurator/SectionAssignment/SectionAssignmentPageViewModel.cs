using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;
using DynamicData;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Render.Kernel;
using Render.Models.Scope;
using Render.Models.Workflow;
using Render.Pages.AppStart.Home;
using Render.Pages.Configurator.SectionAssignment.SectionView;
using Render.Pages.Configurator.SectionAssignment.TeamView;
using Render.Repositories.WorkflowRepositories;
using Render.Resources;
using Render.Resources.Localization;
using Render.Resources.Styles;
using Render.TempFromVessel.Project;

 namespace Render.Pages.Configurator.SectionAssignment
{
    public class SectionAssignmentPageViewModel : WorkflowPageBaseViewModel
    {
        private readonly IWorkflowRepository _workflowRepository;
        private RenderWorkflow _renderWorkflow;

        private SourceList<TeamSectionAssignment> _sourceList = new SourceList<TeamSectionAssignment>();
        private ReadOnlyObservableCollection<TeamSectionAssignment> _teamSectionAssignments;
        public ReadOnlyObservableCollection<TeamSectionAssignment> TeamSectionAssignments => _teamSectionAssignments;

        public SectionAssignmentSectionViewViewModel SectionViewViewModel;
        public SectionAssignmentTeamViewViewModel TeamViewViewModel;
        
        public ReactiveCommand<Unit, Unit> SelectSectionViewCommand;
        public ReactiveCommand<Unit, Unit> SelectTeamViewCommand;

        [Reactive]
        public bool ShowSectionView { get; set; }

        public static async Task<SectionAssignmentPageViewModel> CreateAsync(
            IViewModelContextProvider viewModelContextProvider,
            Guid projectId, bool showSectionView = true, Guid? teamId = null)
        {
            var workflowRepository = viewModelContextProvider.GetWorkflowRepository();
            var workflow = await workflowRepository.GetWorkflowForProjectIdAsync(projectId);
            var sectionRepository = viewModelContextProvider.GetSectionRepository();
            var allSections = await sectionRepository.GetSectionsForProjectAsync(projectId);
            var projectRepository = viewModelContextProvider.GetPersistence<Project>();
            var project = await projectRepository.GetAsync(projectId);
            //get all scopes by project id
            var scopePersistence = viewModelContextProvider.GetPersistence<Scope>();
            var projectScopes = await scopePersistence.QueryOnFieldAsync("ProjectId", projectId.ToString(), 0);

            var teams = workflow.GetTeams();
            var teamTranslatorUsers = new List<TeamTranslatorUser>();
            var userRepository = viewModelContextProvider.GetUserRepository();
            var teamSectionAssignments = new List<TeamSectionAssignment>();
            foreach (var team in teams)
            {
                var user = await userRepository.GetUserAsync(team.TranslatorId);
                teamTranslatorUsers.Add(new TeamTranslatorUser(team, user));
            }
            var priority = 1;
            foreach (var assignment in workflow.AllSectionAssignments)
            {
                var team = teamTranslatorUsers.FirstOrDefault(x => x.Team.SectionAssignments
                    .Any(s => s.SectionId == assignment.SectionId));
                var section = allSections.FirstOrDefault(x => x.Id == assignment.SectionId);
                if (section != null)
                {
                    //Only show sections whose scopes status are active
                    if ((projectScopes.Any(x => x.Status == "Active" && x.Id == section.ScopeId)))
                    {
                        var teamSectionAssignment = new TeamSectionAssignment(section, team, assignment.Priority);
                        teamSectionAssignments.Add(teamSectionAssignment);
                        priority = assignment.Priority;
                    }
                }
            }

            if (allSections.Any())
            {
                foreach (var section in allSections.OrderBy(x => x.Number))
                {
                    //Ignore showing sections whose scopes status are inactive
                    if (workflow.AllSectionAssignments.Any(x => x.SectionId == section.Id) ||  
                    (projectScopes.Any(x => x.Status == "Inactive" && x.Id == section.ScopeId)))
                    {
                        continue;
                    }
                    var team = teamTranslatorUsers.FirstOrDefault(x => x.Team.SectionAssignments
                        .Any(s => s.SectionId == section.Id));
                    var teamSectionAssignment = new TeamSectionAssignment(section, team, ++priority);
                    teamSectionAssignments.Add(teamSectionAssignment);
                }
            }

            var vm = new SectionAssignmentPageViewModel(viewModelContextProvider, workflow, 
            teamSectionAssignments, teamTranslatorUsers, showSectionView, teamId, project?.Name??"");
            return vm;
        }

        private SectionAssignmentPageViewModel(IViewModelContextProvider viewModelContextProvider,
            RenderWorkflow workflow,
            List<TeamSectionAssignment> assignments,
            List<TeamTranslatorUser> teamTranslatorUsers, bool showSectionView, Guid? teamId = null, string secondPageName = null)
            : base("SectionAssignmentPage",
                viewModelContextProvider,
                AppResources.AssignSections,
                null, null,
                secondPageName: secondPageName)
        {
            var color = (ColorReference)ResourceExtensions.GetResourceValue("SecondaryText");
            if (color != null)
                TitleBarViewModel.PageGlyph = IconExtensions.BuildFontImageSource(Icon.AssignSections, color.Color)?.Glyph;
            
            _renderWorkflow = workflow;
            _workflowRepository = viewModelContextProvider.GetWorkflowRepository();

            var changeList = _sourceList.Connect().Publish();
            Disposables.Add(changeList
                .Bind(out _teamSectionAssignments)
                .Subscribe());
           
            Disposables.Add(changeList.Connect());
            _sourceList.AddRange(assignments);
            
            SectionViewViewModel = new SectionAssignmentSectionViewViewModel(viewModelContextProvider, 
                teamTranslatorUsers, assignments, UpdateWorkflow, ReorderSections);
            TeamViewViewModel = new SectionAssignmentTeamViewViewModel(viewModelContextProvider, 
                teamTranslatorUsers, assignments, UpdateWorkflow, ReorderSections, teamId);
            
            SelectSectionViewCommand = ReactiveCommand.Create(SelectSectionView);
            SelectTeamViewCommand = ReactiveCommand.Create(SelectTeamView);
            ShowSectionView = showSectionView;
            
            ProceedButtonViewModel.Icon = IconExtensions.BuildFontImageSource(Icon.CheckCircle, ResourceExtensions.GetColor("SecondaryText"));
            ProceedButtonViewModel.SetCommand(NavigateHomeAsync);
            Disposables.Add(ProceedButtonViewModel.NavigateToPageCommand.IsExecuting
                .Subscribe(isExecuting =>
                {
                    IsLoading = isExecuting;
                }));

            TitleBarViewModel.NavigateBackCommand = ReactiveCommand.CreateFromTask(NavigateHomeAsync);
        }

        private void SelectTeamView()
        {
            ShowSectionView = false;
        }

        private void SelectSectionView()
        {
            ShowSectionView = true;
        }

        public async Task UpdateWorkflow(TeamSectionAssignment assignment)
        {
            if (assignment.Team == null)
            {
                var team = _renderWorkflow.GetTeams().FirstOrDefault(x => x.SectionAssignments
                    .Any(s => s.SectionId == assignment.Section.Id));
                if(team != null)
                    _renderWorkflow.RemoveSectionAssignmentFromTeam(team, assignment.Section.Id);
            }
            else
            {
                _renderWorkflow.AddSectionAssignmentToTeam(assignment.Team.Team, assignment.Section.Id, assignment.Priority);
            }

            await _workflowRepository.SaveWorkflowAsync(_renderWorkflow);
        }

        public async Task ReorderSections(TeamSectionAssignment latestChangedAssignment)
        {
            var assignments = 
                _teamSectionAssignments.Where(x => x.Section != latestChangedAssignment.Section)
                .OrderBy(x => x.Priority)
                .Where(x => x.Priority >= latestChangedAssignment.Priority);
            foreach (var teamAssignment in assignments)
            {
                teamAssignment.Priority++;
            }
            var index = 1;
            foreach (var teamSectionAssignment in _teamSectionAssignments.OrderBy(x => x.Priority))
            {
                teamSectionAssignment.Priority = index;
                index++;
            }

            foreach (var sectionAssignment in _renderWorkflow.AllSectionAssignments)
            {
                var newAssignment = _teamSectionAssignments.FirstOrDefault(x => x.Section.Id == sectionAssignment.SectionId);
                if (newAssignment != null)
                {
                     sectionAssignment.Priority = newAssignment.Priority;
                }
            }

            _ = _workflowRepository.SaveWorkflowAsync(_renderWorkflow);
        }

        public async Task<IRoutableViewModel> NavigateHomeAsync()
        {
            var home = await Task.Run(async () => await HomeViewModel.CreateAsync(_renderWorkflow.ProjectId, ViewModelContextProvider));
            return await NavigateToAndReset(home);
        }

        public override void Dispose()
        {
            _renderWorkflow = null;
            _sourceList?.Dispose();
            _sourceList = null;
            _teamSectionAssignments = null;

            SectionViewViewModel?.Dispose();
            SectionViewViewModel = null;

            TeamViewViewModel?.Dispose();
            TeamViewViewModel = null;

            base.Dispose();
        }
    }
}