using System.Reactive.Linq;
using ReactiveUI;
using Render.Kernel;
using Render.Kernel.NavigationFactories;
using Render.Models.Workflow;
using Render.Models.Workflow.Stage;

namespace Render.Pages.AppStart.Home.NavigationIcons.Workflow;

public class PeerReviseNavigationIconViewModel : WorkflowNavigationIconViewModel
{
    public PeerReviseNavigationIconViewModel(IViewModelContextProvider viewModelContextProvider, Stage stage, Step step, int sectionsAtStep)
        : base(viewModelContextProvider, stage, step, sectionsAtStep)
    {
    }

    protected override async Task<IRoutableViewModel> NavigateOnClickAsync()
    {
        foreach (var sectionId in StageService.SectionsAtStep(Step.Id))
        {
            var section = await SectionRepository.GetSectionWithDraftsAsync(sectionId, withReferences: true);

            if (IsSectionDocumentMissing(sectionId, section))
            {
                continue;
            }

            if (IsAudioMissing(section, Step.RenderStepType))
            {
                return null;
            }

            var vm = await WorkflowPageViewModelFactory
                .GetViewModelToNavigateTo(ViewModelContextProvider, Step, section);
            return await NavigateToAndReset(vm);
        }

        return null;
    }
}