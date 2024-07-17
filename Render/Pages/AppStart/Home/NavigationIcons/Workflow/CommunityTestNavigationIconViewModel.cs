using System.Reactive.Linq;
using ReactiveUI;
using Render.Kernel;
using Render.Kernel.NavigationFactories;
using Render.Models.Workflow;
using Render.Models.Workflow.Stage;

namespace Render.Pages.AppStart.Home.NavigationIcons.Workflow;

public class CommunityTestNavigationIconViewModel : WorkflowNavigationIconViewModel
{
    public CommunityTestNavigationIconViewModel(IViewModelContextProvider viewModelContextProvider, Stage stage, Step step, int sectionsAtStep)
        : base(viewModelContextProvider, stage, step, sectionsAtStep)
    {
    }

    protected override async Task<IRoutableViewModel> NavigateOnClickAsync()
    {
        foreach (var sectionId in GrandCentralStation.SectionsAtStep(Step.Id))
        {
            var section = await _sectionRepository.GetSectionWithDraftsAsync(sectionId, getCommunityTest: true, withReferences: true);

            if (IsSectionDocumentMissing(sectionId, section))
            {
                continue;
            }

            if (IsAudioMissing(section, Step.RenderStepType, checkForCommunityAudio: true))
            {
                return null;
            }

            var vm = await Task.Run(async () => await WorkflowPageViewModelFactory
                .GetViewModelToNavigateTo(ViewModelContextProvider, Step, section));
            return await HostScreen.Router.NavigateAndReset.Execute(vm);
        }

        return null;
    }
}