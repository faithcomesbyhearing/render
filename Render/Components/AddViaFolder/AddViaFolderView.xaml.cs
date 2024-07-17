using System.Reactive.Linq;
using ReactiveUI;
using Render.Kernel.WrappersAndExtensions;
using Render.Pages.AppStart.Login;

namespace Render.Components.AddViaFolder;

//TODO: rename this control to AddProjectView and move it to Components\AddProject\ folder.
public partial class AddViaFolderView
{
    public AddViaFolderView()
    {
        InitializeComponent();

        this.WhenActivated(d =>
        {
            d(this.OneWayBind(ViewModel, vm => vm.FlowDirection, v => v.TopLevelElement.FlowDirection));
            d(this.OneWayBind(ViewModel, vm => vm.NavigateOnCompletedButtonText, v => v.ReturnToScreenLabel.Text));
            d(this.OneWayBind(ViewModel, vm => vm.DownloadErrorText, v => v.ErrorAddingProjectLabel.Text));

            d(this.BindCommandCustom(CancelOnDownloadingGesture, v => v.ViewModel.CancelOnDownloadingCommand));
            d(this.BindCommandCustom(RetryOnErrorGesture, v => v.ViewModel.RetryOnErrorCommand));
            d(this.BindCommandCustom(CancelOnErrorGesture, v => v.ViewModel.CancelOnErrorCommand));
            d(this.BindCommandCustom(NavigateOnCompletedGesture, v => v.ViewModel.NavigateOnCompletedCommand));

            d(this.WhenAnyValue(x => x.ViewModel.AddProjectState)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(SetState));

            SetProjectIdLabel(d);
        });
    }

    private void SetProjectIdLabel(Action<IDisposable> d)
    {
        var isVisibleProjectId = false;
        if (ViewModel is IAddProjectViaIdViewModel addProjectViaIdViewModel)
        {
            d(this.OneWayBind(addProjectViaIdViewModel, vm => vm.ProjectId, v => v.AddingProjectIdLabel.Text));
            d(this.OneWayBind(addProjectViaIdViewModel, vm => vm.ProjectId, v => v.ErrorProjectIdLabel.Text));
            d(this.OneWayBind(addProjectViaIdViewModel, vm => vm.ProjectId, v => v.SuccessProjectIdLabel.Text));

            isVisibleProjectId = true;
        }

        AddingProjectIdLabel.IsVisible = isVisibleProjectId;
        ErrorProjectIdLabel.IsVisible = isVisibleProjectId;
        SuccessProjectIdLabel.IsVisible = isVisibleProjectId;
    }

    private void SetState(AddProjectState addProjectState)
    {
        switch (addProjectState)
        {
            case AddProjectState.None:
                LoadingStack.IsVisible = false;
                ErrorAddingProjectStack.IsVisible = false;
                ProjectAddedSuccessStack.IsVisible = false;
                break;
            case AddProjectState.Loading:
                LoadingStack.IsVisible = true;
                ErrorAddingProjectStack.IsVisible = false;
                ProjectAddedSuccessStack.IsVisible = false;
                break;
            case AddProjectState.ErrorAddingProject:
            case AddProjectState.ErrorConnectingToLocalMachine:
                LoadingStack.IsVisible = false;
                ErrorAddingProjectStack.IsVisible = true;
                ProjectAddedSuccessStack.IsVisible = false;
                break;
            case AddProjectState.ProjectAddedSuccessfully:
                LoadingStack.IsVisible = false;
                ProjectAddedSuccessStack.IsVisible = true;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(addProjectState), addProjectState, null);
        }
    }
}