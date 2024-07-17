using ReactiveUI;
namespace Render.Components.ProfileAvatar;

public partial class UserLoginIconVertical 
{
	public UserLoginIconVertical()
	{
		InitializeComponent();
        this.WhenActivated(d =>
        {
            d(this.OneWayBind(ViewModel, vm => vm.User.FullName, v => v.Label.Text));
            d(this.BindCommand(ViewModel, vm => vm.OnSelectUser, v => v.FrameTap));
            d(this.OneWayBind(ViewModel, vm => vm.IsRenderUser, v => v.GlobalUserBadge.Opacity, (v) => v ? 0 : 1));
        });
    }

    public static readonly BindableProperty OrientationProperty = BindableProperty.Create(
           "",
           typeof(StackOrientation),
           typeof(UserLoginIconVertical),
           propertyChanged: OrientationPropertyChanged);

    public StackOrientation Orientation
    {
        get => (StackOrientation)GetValue(OrientationProperty);
        set => SetValue(OrientationProperty, value);
    }

    private static void OrientationPropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var control = (UserLoginIconVertical)bindable;
        control.Layout.Orientation = (StackOrientation)newValue;
        control.Label.Margin = (StackOrientation)newValue == StackOrientation.Horizontal
            ? new Thickness(5, 15, 0, 0)
            : new Thickness(0);
    }

    protected override void Dispose(bool disposing)
    {
        ViewModel?.Dispose();

        base.Dispose(disposing);
    }
}
