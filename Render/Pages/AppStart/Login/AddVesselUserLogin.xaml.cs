using ReactiveUI;
using Render.Kernel.WrappersAndExtensions;
using Render.Resources;

namespace Render.Pages.AppStart.Login;

public partial class AddVesselUserLogin
{
    public AddVesselUserLogin()
    {
        InitializeComponent();
        this.WhenActivated(d =>
        {
            d(this.OneWayBind(ViewModel, vm => vm.FlowDirection,
                v => v.TopLevelElement.FlowDirection));
            d(this.OneWayBind(ViewModel, vm => vm.UsernameViewModel,
                v => v.Username.BindingContext));
            d(this.OneWayBind(ViewModel, vm => vm.PasswordViewModel,
                v => v.Password.BindingContext));
            d(this.OneWayBind(ViewModel, vm => vm.AddFromComputerViewModel,
                v => v.AddViaFolderView.BindingContext));
            
            d(this.OneWayBind(ViewModel, vm => vm.Loading,
             v => v.LoadingStack.IsVisible));

#if DEMO
            AddProjectViaGuidButton.IsVisible = false;
            AddProjectFromComputer.IsVisible = false;
#else
            d(this.OneWayBind(ViewModel, vm => vm.ShowAddProjectIdButton,
                v => v.AddProjectViaGuidButton.IsVisible));
#endif

            d(this.OneWayBind(ViewModel, vm => vm.ShowBackButton,
                v => v.LoginBackButton.IsVisible));
            d(this.BindCommandCustom(AddAProjectFromComputerGesture, v => v.ViewModel.AddProjectFromComputerCommand));
            d(this.BindCommandCustom(AddAProjectViaIdGesture, v => v.ViewModel.NavigateToAddProjectId));
            d(this.BindCommandCustom(LoginFrameGesture, v => v.ViewModel.LoginCommand));
            d(this.OneWayBind(ViewModel, vm => vm.Loading,
                v => v.LoginStack.IsVisible, Selector));
            d(this.WhenAnyValue(x => x.ViewModel.ShowAddNewUserLabel)
                .Subscribe(LabelSelector));
            d(this.WhenAnyValue(x => x.ViewModel.AddFromComputerViewModel.ShowAddFromComputer)
                .Subscribe(ViewSelector));
            d(this.OneWayBind(ViewModel, vm => vm.AllowLoginCommand,
                v => v.LoginButtonFrame.Opacity, isEnabled => isEnabled ? 1 : 0.3));
        });
        LoginButtonFrame.GestureRecognizers.Add(AddClickEffect(LoginButtonFrame));
        AddProjectViaGuidButton.GestureRecognizers.Add(AddClickEffect(AddProjectViaGuidButton));
        AddProjectFromComputer.GestureRecognizers.Add(AddClickEffect(AddProjectFromComputer));
    }

    private static TapGestureRecognizer AddClickEffect(VisualElement buttonBorder)
    {
        return new TapGestureRecognizer
        {
            Command = new Command(async (o) =>
            {
                await buttonBorder.ScaleTo(0.95, 100, Easing.CubicOut);
                await buttonBorder.ScaleTo(1, 100, Easing.CubicIn);
            })
        };
    }

    private bool Selector(bool arg)
    {
        return !arg;
    }

    private void LabelSelector(bool showAddNewUserLabel)
    {
        if (!showAddNewUserLabel)
        {
            WelcomeLabel.SetValue(IsVisibleProperty, true);
            LoginLabel.SetValue(IsVisibleProperty, true);
        }
        else
        {
            NewUserLabel.SetValue(IsVisibleProperty, true);
        }
    }
    
    private void ViewSelector(bool showAddFromComputer)
    {
        if (!showAddFromComputer)
        {
            MainLoginView.SetValue(IsVisibleProperty, true);
            AddViaFolderView.SetValue(IsVisibleProperty, false);
        }
        else
        {
            MainLoginView.SetValue(IsVisibleProperty, false);
            AddViaFolderView.SetValue(IsVisibleProperty, true);
        }
    }

    protected override void Dispose(bool disposing)
    {
        Username?.Dispose();
        Password?.Dispose();

        base.Dispose(disposing);
    }
}