using System.Reactive.Linq;
using ReactiveUI;
using Render.Kernel;
using Render.Kernel.NavigationFactories;
using Render.Models.Workflow;
using Render.Models.Workflow.Stage;

namespace Render.Pages.AppStart.Home.NavigationIcons.Workflow;

public class ReferencesWorkflowNavigationIconViewModel : WorkflowNavigationIconViewModel
{
    public ReferencesWorkflowNavigationIconViewModel(IViewModelContextProvider viewModelContextProvider, Stage stage, Step step, int sectionsAtStep)
        : base(viewModelContextProvider, stage, step, sectionsAtStep)
    {
    }

    protected override async Task<IRoutableViewModel> NavigateOnClickAsync()
    {
        var viewModel = await Task.Run(async () =>
        {
            var workflowRepository = ViewModelContextProvider.GetWorkflowRepository();
            var workflow = await workflowRepository.GetWorkflowForProjectIdAsync(GrandCentralStation.CurrentProjectId);
            var sectionsAtStep = GrandCentralStation.SectionsAtStep(Step.Id);
            var priorSectionId = workflow.AllSectionAssignments
                .OrderBy(assignment => assignment.Priority)
                .Select(assignment => assignment.SectionId)
                .Intersect(sectionsAtStep)
                .First();

            var section = await _sectionRepository.GetSectionWithReferencesAsync(priorSectionId);

            if (IsSectionDocumentMissing(priorSectionId, section) || IsAudioMissing(section, Step.RenderStepType))
            {
                return null;
            }

            return await WorkflowPageViewModelFactory.GetViewModelToNavigateTo(ViewModelContextProvider, Step, section);
        });

        if (viewModel != null)
        {
            return await HostScreen.Router.NavigateAndReset.Execute(viewModel);
        }

        return null;
    }
}