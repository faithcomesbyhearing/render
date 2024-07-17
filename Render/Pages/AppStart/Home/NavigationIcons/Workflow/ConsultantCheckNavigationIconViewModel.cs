using System.Reactive.Linq;
using ReactiveUI;
using Render.Kernel;
using Render.Models.Workflow;
using Render.Models.Workflow.Stage;
using Render.Pages.Consultant.ConsultantCheck;

namespace Render.Pages.AppStart.Home.NavigationIcons.Workflow;

public class ConsultantCheckNavigationIconViewModel : WorkflowNavigationIconViewModel
{
    private readonly Guid _projectId;
    public ConsultantCheckNavigationIconViewModel(IViewModelContextProvider viewModelContextProvider, Stage 
        stage, Step step, Guid projectId, int sectionsAtStep) 
        : base(viewModelContextProvider, stage, step, sectionsAtStep)
    {
        _projectId = projectId;
        ActionState = sectionsAtStep > 0 ? ActionState.Required : ActionState.Optional;
    }
        
    protected override async Task<IRoutableViewModel> NavigateOnClickAsync()
    {
        var vm = await Task.Run(
            async () => await ConsultantCheckSectionSelectViewModel.CreateAsync(_projectId, 
                ViewModelContextProvider, Stage, Step));
        
        return await HostScreen.Router.NavigateAndReset.Execute(vm);
    }
}