using ReactiveUI;
using Render.Kernel;
using Render.Resources;
using Render.Resources.Styles;

namespace Render.Pages.AppStart.Home.NavigationIcons;

public partial class MiniNavigationIcon
{
    public MiniNavigationIcon()
    {
        InitializeComponent();
            
        DisposableBindings = this.WhenActivated(d =>
        {
            d(this.OneWayBind(ViewModel, vm => vm.ActionState, 
                v => v.ComponentFrame.BackgroundColor, Selector));
            d(this.OneWayBind(ViewModel, vm => vm.IsFirstIcon,
                v => v.Rectangle.IsVisible, Selector));
        });
        MiniConfigIconStack.SizeChanged += OnSizeChanged;
    }

    private bool Selector(bool arg)
    {
        return !arg;
    }

    private Color Selector(ActionState arg)
    {
        switch (arg)
        {
            case ActionState.Inactive:
            case ActionState.Optional:
                return ((ColorReference)ResourceExtensions.GetResourceValue("Option")).Color;
            case ActionState.Required:
                return ((ColorReference)ResourceExtensions.GetResourceValue("Required")).Color;
            default:
                throw new ArgumentOutOfRangeException(nameof(arg), arg, null);
        }
    }

    private void OnSizeChanged(object sender, EventArgs e)
    {
        MiniConfigIconStack.SetValue(HeightRequestProperty, 20);
    }
}