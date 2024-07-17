using System.Reactive;
using ReactiveUI;
using Render.Kernel;
using Render.Pages.AppStart.Login;

namespace Render.Components.AddProject;

public interface IAddProjectViewModel : IViewModelBase
{
    AddProjectState AddProjectState { get; }
    ReactiveCommand<Unit, Unit> CancelOnDownloadingCommand { get; }
    ReactiveCommand<Unit, Unit> RetryOnErrorCommand { get; }
    ReactiveCommand<Unit, Unit> CancelOnErrorCommand { get; }
    ReactiveCommand<Unit, IRoutableViewModel> NavigateOnCompletedCommand { get; }
    string NavigateOnCompletedButtonText { get; }
    string DownloadErrorText { get; }
}