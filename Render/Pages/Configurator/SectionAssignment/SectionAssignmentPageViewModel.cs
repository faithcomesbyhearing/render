using System.Reactive;
using System.Reactive.Linq;
using DynamicData;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Render.Kernel;
using Render.Extensions;
using Render.Models.Workflow;
using Render.Repositories.WorkflowRepositories;
using Render.Resources;
using Render.Resources.Localization;
using Render.Resources.Styles;
using Render.Pages.AppStart.Home;
using Render.Pages.Configurator.SectionAssignment.Tabs.Section;
using Render.Pages.Configurator.SectionAssignment.Tabs.Team;
using Render.Pages.Configurator.SectionAssignment.Factory;
using Render.Pages.Configurator.SectionAssignment.Manager;

namespace Render.Pages.Configurator.SectionAssignment
{
    public class SectionAssignmentPageViewModel : WorkflowPageBaseViewModel
    {
        private IWorkflowRepository _workflowRepository;
        private RenderWorkflow _renderWorkflow;
        private SectionCollectionsManager _manager;

        [Reactive]
        public SectionViewTabViewModel SectionTabViewModel { get; private set; }
        
        [Reactive]
        public TeamViewTabViewModel TeamTabViewModel { get; private set; }
        
        [Reactive]
        public bool ShowSectionView { get; private set; } = true;

        [Reactive]
        public ReactiveCommand<Unit, Unit> SelectSectionViewCommand { get; private set; }
        
        [Reactive]
        public ReactiveCommand<Unit, Unit> SelectTeamViewCommand { get; private set; }

        public static async Task<SectionAssignmentPageViewModel> CreateAsync(
            IViewModelContextProvider viewModelContextProvider,
            Guid projectId, 
            Guid? displayTeamId = null)
        {
            var workflowRepository = viewModelContextProvider.GetWorkflowRepository();
            var workflow = await workflowRepository.GetWorkflowForProjectIdAsync(projectId);
            var cards = await SectionAssignmentCardsFactory.CreateCards(viewModelContextProvider, workflow, projectId);

            return new SectionAssignmentPageViewModel(
                viewModelContextProvider,
                workflowRepository,
                workflow,
                cards, 
                displayTeamId);
        }

        private SectionAssignmentPageViewModel(
            IViewModelContextProvider viewModelContextProvider,
            IWorkflowRepository workflowRepository,
            RenderWorkflow workflow, 
            SectionAssignmentCards cards,
            Guid? displayTeamId=null)
            : base(
                urlPathSegment: "SectionAssignmentPage",
                viewModelContextProvider: viewModelContextProvider,
                pageName: AppResources.AssignSections,
                section: null,
                stage: null)
        {
            _renderWorkflow = workflow;
            _workflowRepository = workflowRepository;
            _manager = new SectionCollectionsManager(cards);

            TitleBarViewModel.PageGlyph = IconExtensions.BuildFontImageSource(
                icon: Icon.AssignSections,
                color: ResourceExtensions.GetResourceValue<ColorReference>("SecondaryText")?.Color)?.Glyph;

            ProceedButtonViewModel.Icon = IconExtensions.BuildFontImageSource(
                icon: Icon.CheckCircle, 
                color: ResourceExtensions.GetColor("SecondaryText"));
            
            ShowSectionView = displayTeamId is null;
            SectionTabViewModel = new SectionViewTabViewModel(ViewModelContextProvider, _manager);
            TeamTabViewModel = new TeamViewTabViewModel(ViewModelContextProvider, _manager, displayTeamId);
            
            SelectSectionViewCommand = ReactiveCommand.Create(SelectSectionView);
            SelectTeamViewCommand = ReactiveCommand.Create(SelectTeamView);
            ProceedButtonViewModel.SetCommand(NavigateHomeAsync);

            var homeNavigationCommand = ReactiveCommand.CreateFromTask(NavigateHomeAsync);

            TitleBarViewModel.NavigateHomeCommand = homeNavigationCommand;
            TitleBarViewModel.NavigateBackCommand = homeNavigationCommand;
                        
            Disposables
                .Add(ProceedButtonViewModel.NavigateToPageCommand.IsExecuting
                .Subscribe(isExecuting => IsLoading = isExecuting));

            Disposables
                .Add(homeNavigationCommand.IsExecuting
                .Subscribe(isExecuting => IsLoading = isExecuting));
        }

        public async Task<IRoutableViewModel> NavigateHomeAsync()
        {
            var homeViewModel = await HomeViewModel.CreateAsync(_renderWorkflow.ProjectId, ViewModelContextProvider);
            return await NavigateToAndReset(homeViewModel);
        }

        protected override Task NavigatingAwayAsync()
        {
            return SaveWorkflowAsync();
        }

        private void SelectTeamView()
        {
            ShowSectionView = false;
        }

        private void SelectSectionView()
        {
            ShowSectionView = true;
        }

        private Task SaveWorkflowAsync()
        {
            var allTeams = _renderWorkflow.GetTeams();
            var allTeamsModels = _manager.TeamCards;

            foreach (var team in allTeams)
            {
                var teamCard = allTeamsModels.First(card => card.TeamId == team.Id);
                var sectionIdsToRemove = team.SectionAssignments
                    .Select(assignment => assignment.SectionId)
                    .Except(teamCard
                        .AssignedSections
                        .Select(section => section.SectionId))
                    .ToList();

                sectionIdsToRemove.ForEach(idToRemove => team.RemoveSectionAssignment(idToRemove));
                teamCard.AssignedSections.ForEach(section => team.AddSectionAssignment(section.SectionId, section.Priority));
            }

            return _workflowRepository.SaveWorkflowAsync(_renderWorkflow);
        }

        public override void Dispose()
        {
            _manager.Dispose();
            _manager = null;
            _workflowRepository = null;
            _renderWorkflow = null;

            TeamTabViewModel?.Dispose();
            TeamTabViewModel = null;

            SectionTabViewModel?.Dispose();
            SectionTabViewModel = null;
            
            SelectSectionViewCommand?.Dispose();
            SelectSectionViewCommand = null;

            SelectTeamViewCommand?.Dispose();
            SelectTeamViewCommand = null;

            base.Dispose();
        }
    }
}