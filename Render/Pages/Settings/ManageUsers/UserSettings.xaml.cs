using ReactiveUI;
using Render.Resources;
using Render.Resources.Styles;

namespace Render.Pages.Settings.ManageUsers;

public partial class UserSettings
{
    public UserSettings()
    {
        InitializeComponent();

        this.WhenActivated(d =>
        {
            d(this.OneWayBind(ViewModel, vm => vm.FlowDirection,
                v => v.TopLevelElement.FlowDirection));
            d(this.OneWayBind(ViewModel, vm => vm.TitleBarViewModel, v => v.TitleBar.BindingContext));

            d(this.OneWayBind(ViewModel, vm => vm.IsRenderUser, v => v.PasswordSection.IsVisible));
            d(this.OneWayBind(ViewModel, vm => vm.IsRenderUser, v => v.EmptyPasswordSection.IsVisible, Selector));
            d(this.OneWayBind(ViewModel, vm => vm.IsRenderUser, v => v.GlobalUserName.IsVisible, Selector));
            d(this.OneWayBind(ViewModel, vm => vm.IsRenderUser, v => v.RenderUserNameFrame.IsVisible));

            d(this.OneWayBind(ViewModel, vm => vm.ShowDeleteButton, v => v.DeleteUserButton.IsVisible));

            d(this.OneWayBind(ViewModel, vm => vm.SelectedPasswordType, v => v.TextPasswordStack.IsVisible, ShowTextPassword));
            d(this.OneWayBind(ViewModel, vm => vm.SelectedPasswordType, v => v.GridPasswordStack.IsVisible, ShowGridPassword));

            d(this.OneWayBind(ViewModel, vm => vm.UserName, v => v.GlobalUserName.Text));
            d(this.Bind(ViewModel, vm => vm.UserName, v => v.RenderUserName.Text));

            d(this.OneWayBind(ViewModel, vm => vm.UserTypeString, v => v.UserTypeLabel.Text));
            d(this.OneWayBind(ViewModel, vm => vm.UserTypeString, v => v.UserTypeLabel.TextColor, ConversionHintTypeTextColor));
            d(this.OneWayBind(ViewModel, vm => vm.UserTypeString, v => v.UserTypeFrame.BackgroundColor,
                ConversionHintFrameBackground));
            d(this.BindCommand(ViewModel, vm => vm.ResetGridPasswordCommand, v => v.ResetGesture));
            d(this.BindCommand(ViewModel, vm => vm.DeleteUserCommand, v => v.DeleteUserGesture));
            d(this.BindCommand(ViewModel, vm => vm.GeneratePasswordCommand, v => v.GeneratePasswordGesture));
            d(this.OneWayBind(ViewModel, vm => vm.PasswordGridViewModel, v => v.PasswordGrid.BindingContext));
            d(this.OneWayBind(ViewModel, vm => vm.ProceedButtonViewModel, v => v.ProceedButton.BindingContext));
            d(this.OneWayBind(ViewModel, vm => vm.LocalizationResources, v => v.LocalizationPicker.ItemsSource));
            d(this.Bind(ViewModel, vm => vm.SelectedLocalizationResource, v => v.LocalizationPicker.SelectedItem));
            d(this.OneWayBind(ViewModel, vm => vm.PasswordTypes, v => v.PasswordTypePicker.ItemsSource));
            d(this.Bind(ViewModel, vm => vm.Password, v => v.PasswordEntry.Text));
            d(this.BindCommand(ViewModel, vm => vm.ToggleShowPasswordCommand, v => v.ViewPasswordGesture));
            d(this.Bind(ViewModel, vm => vm.HidePassword, v => v.PasswordEntry.IsPassword));
            d(this.Bind(ViewModel, vm => vm.SelectedLocalizedPasswordType, v => v.PasswordTypePicker.SelectedItem));
            d(this.OneWayBind(ViewModel, vm => vm.IsLoading, v => v.loadingView.IsVisible));
        });
    }

    protected override void OnAppearing()
    {
        LocalizationPicker.ItemDisplayBinding = new Binding("ResourceDisplayName");
        PasswordTypePicker.ItemDisplayBinding = new Binding("LocalizedPasswordType");
        base.OnAppearing();
    }

    private Color ConversionHintTypeTextColor(string arg)
    {
        switch (arg)
        {
            case "RENDER":
                return ((ColorReference)ResourceExtensions.GetResourceValue("RenderUserTypeText")).Color;
            case "GLOBAL":
                return ((ColorReference)ResourceExtensions.GetResourceValue("VesselUserTypeText")).Color;
            default:
                return ViewModel.IsRenderUser
                    ? ((ColorReference)ResourceExtensions.GetResourceValue("RenderUserTypeText")).Color
                    : ((ColorReference)ResourceExtensions.GetResourceValue("VesselUserTypeText")).Color;
        }
    }

    private Color ConversionHintFrameBackground(string arg)
    {
        switch (arg)
        {
            case "RENDER":
                return ((ColorReference)ResourceExtensions.GetResourceValue("RenderUserType")).Color;
            case "GLOBAL":
                return ((ColorReference)ResourceExtensions.GetResourceValue("VesselUserType")).Color;
            default:
                return ViewModel.IsRenderUser
                    ? ((ColorReference)ResourceExtensions.GetResourceValue("RenderUserType")).Color
                    : ((ColorReference)ResourceExtensions.GetResourceValue("VesselUserType")).Color;
        }
    }

    private bool Selector(bool arg)
    {
        GlobalUserName.HorizontalOptions = arg ? LayoutOptions.StartAndExpand : LayoutOptions.Start;
        RenderUserName.HorizontalOptions = arg ? LayoutOptions.StartAndExpand : LayoutOptions.Start;
        UserTypeFrame.HorizontalOptions = arg ? LayoutOptions.Center : LayoutOptions.StartAndExpand;
        return !arg;
    }

    private void PasswordEntry_OnFocused(object sender, FocusEventArgs e)
    {
        LogInfo("Entry Focused", new Dictionary<string, string>
        {
            {"Entry Label", "Password"}
        });
        LogInfo("Password entry focused");
    }

    private void UsernameEntry_OnFocused(object sender, FocusEventArgs e)
    {
        LogInfo("Entry Focused", new Dictionary<string, string>
        {
            {"Entry Label", "Username"}
        });
        LogInfo("Username entry focused");
    }

    private bool ShowGridPassword(PasswordType arg)
    {
        return arg == PasswordType.Grid;
    }

    private bool ShowTextPassword(PasswordType arg)
    {
        return arg == PasswordType.Text;
    }
}