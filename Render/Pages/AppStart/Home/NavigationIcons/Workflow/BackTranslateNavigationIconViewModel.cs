using System.Reactive.Linq;
using ReactiveUI;
using Render.Kernel;
using Render.Kernel.NavigationFactories;
using Render.Models.Workflow;
using Render.Models.Workflow.Stage;

namespace Render.Pages.AppStart.Home.NavigationIcons.Workflow;

public class BackTranslateNavigationIconViewModel : WorkflowNavigationIconViewModel
{
    public BackTranslateNavigationIconViewModel(IViewModelContextProvider viewModelContextProvider, Stage stage,
        Step step, int sectionsAtStep)
        : base(viewModelContextProvider, stage, step, sectionsAtStep)
    {
    }

    protected override async Task<IRoutableViewModel> NavigateOnClickAsync()
    {
        var vm = await Task.Run(async () =>
        {
            foreach (var sectionId in GrandCentralStation.SectionsAtStep(Step.Id))
                {
                var section = await _sectionRepository.GetSectionWithDraftsAsync(sectionId, true, true);
            
                if (IsSectionDocumentMissing(sectionId, section))
                {
                    continue;
                }
                if (IsAudioMissing(section, Step.RenderStepType))
                {
                    return null;
                }

                return await WorkflowPageViewModelFactory
                .GetViewModelToNavigateTo(ViewModelContextProvider, Step, section);
            }

            return null;
        });

        if (vm != null)
        {
            return await HostScreen.Router.NavigateAndReset.Execute(vm);
        }

        return null;
    }
}