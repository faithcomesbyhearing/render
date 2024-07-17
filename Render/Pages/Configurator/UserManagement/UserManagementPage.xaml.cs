using ReactiveUI;
using Render.Kernel.WrappersAndExtensions;

namespace Render.Pages.Configurator.UserManagement;

public partial class UserManagementPage
{
    public UserManagementPage()
    {
        InitializeComponent();

        this.WhenActivated(d =>
        {
            d(this.OneWayBind(ViewModel, vm => vm.FlowDirection, 
                v => v.TopLevelElement.FlowDirection));
            d(this.OneWayBind(ViewModel, vm => vm.TitleBarViewModel, v => v.TitleBar.BindingContext));
            d(this.OneWayBind(ViewModel, vm => vm.ProceedButtonViewModel, v => v.ProceedButton.BindingContext));
            d(this.OneWayBind(ViewModel, vm => vm.RenderUserTileViewModels.Items, v => v.RenderUsersList.ItemsSource));
            d(this.OneWayBind(ViewModel, vm => vm.VesselUserTileViewModels.Items, v => v.VesselUsersList.ItemsSource));
            d(this.BindCommandCustom(AddUserGestureRecognizer, v => v.ViewModel.CreateUserCommand));
            d(this.OneWayBind(ViewModel, vm => vm.IsLoading, v => v.LoadingView.IsVisible));
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
    }
}