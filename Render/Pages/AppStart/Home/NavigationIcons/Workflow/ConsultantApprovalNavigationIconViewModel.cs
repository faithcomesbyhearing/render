using ReactiveUI;
using System.Reactive.Linq;
using Render.Kernel;
using Render.Models.Workflow;
using Render.Models.Workflow.Stage;
using Render.Pages.Consultant.ConsultantApproval;

namespace Render.Pages.AppStart.Home.NavigationIcons.Workflow;

public class ConsultantApprovalNavigationIconViewModel : WorkflowNavigationIconViewModel
{
    public ConsultantApprovalNavigationIconViewModel(
        IViewModelContextProvider viewModelContextProvider,
        Stage stage,
        Step step,
        int sectionsAtStep) 
        : base(viewModelContextProvider, stage, step, sectionsAtStep) { }

    protected override async Task<IRoutableViewModel> NavigateOnClickAsync()
    {
        var vm = await Task.Run(() => SelectSectionToApproveViewModel.CreateAsync(ViewModelContextProvider, Step, Stage));
        return await HostScreen.Router.NavigateAndReset.Execute(vm);
    }
}