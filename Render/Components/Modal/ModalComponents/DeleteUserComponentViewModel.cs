using System.Reactive;
using System.Reactive.Linq;
using ReactiveUI;
using Render.Kernel;
using Render.Pages.Configurator.SectionAssignment;
using Render.Resources.Localization;

namespace Render.Components.Modal.ModalComponents;

public class DeleteUserComponentViewModel : ViewModelBase
{
    private Guid TeamId { get; }
    public string DeleteUserMessage { get; set; }
    public bool EnableSectionAssignment { get; }
    public ReactiveCommand<Unit, Unit> ViewSectionAssignmentsCommand { get;}
    
    public DeleteUserComponentViewModel(
        IViewModelContextProvider viewModelContextProvider,
        Guid teamId,
        string userName,
        bool enableSectionAssignment) :
        base("DeleteUserComponentViewModel", viewModelContextProvider)
    {
        TeamId = teamId;
        DeleteUserMessage = string.Format(AppResources.DeleteUserModalMessage, userName);
        EnableSectionAssignment = enableSectionAssignment;
        ViewSectionAssignmentsCommand = ReactiveCommand.CreateFromTask(OnViewSectionAssignmentsPressedAsync);
    }
    
    private async Task OnViewSectionAssignmentsPressedAsync()
    {
        var projectId = ViewModelContextProvider.GetGrandCentralStation().CurrentProjectId;
        var vm = await SectionAssignmentPageViewModel.CreateAsync(ViewModelContextProvider, projectId, false, TeamId);
        await NavigateToAndReset(vm);
    } 
}