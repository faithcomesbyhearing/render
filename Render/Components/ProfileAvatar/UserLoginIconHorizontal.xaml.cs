using ReactiveUI;
namespace Render.Components.ProfileAvatar;

public partial class UserLoginIconHorizontal
{
    public UserLoginIconHorizontal()
    {
        InitializeComponent();
        this.WhenActivated(d =>
        {
            d(this.OneWayBind(ViewModel, vm => vm.User.FullName, v => v.Label.Text));
            d(this.OneWayBind(ViewModel, vm => vm.User.FullName, v => v.GlobalUserLabel.Text));
            d(this.BindCommand(ViewModel, vm => vm.OnSelectUser, v => v.FrameTap));
            d(this.OneWayBind(ViewModel, vm => vm.IsRenderUser, v => v.Label.IsVisible));
            d(this.OneWayBind(ViewModel, vm => vm.IsRenderUser, v => v.GlobalUser.IsVisible, Selector));
        });
    }

    public static readonly BindableProperty OrientationProperty = BindableProperty.Create(
        "",
        typeof(StackOrientation),
        typeof(UserLoginIconHorizontal),
        propertyChanged: OrientationPropertyChanged);

    public StackOrientation Orientation
    {
        get => (StackOrientation)GetValue(OrientationProperty);
        set => SetValue(OrientationProperty, value);
    }

    private static void OrientationPropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var control = (UserLoginIconHorizontal)bindable;
        control.Layout.Orientation = (StackOrientation)newValue;
        control.Label.Margin = (StackOrientation)newValue == StackOrientation.Horizontal
            ? new Thickness(5, 0, 0, 0)
            : new Thickness(0);
    }

    private bool Selector(bool arg)
    {
        return !arg;
    }

    protected override void Dispose(bool disposing)
    {
        ViewModel?.Dispose();

        base.Dispose(disposing);
    }
}
