using ReactiveUI;
using Render.Kernel.WrappersAndExtensions;

namespace Render.Components.ProceedButton;

public partial class ProceedButton
{
    public const double ButtonSize = 75;
    public const int BorderCornerRadius = 12;
    public const double BorderThickness = 2;

    // calculated values
    public const double BorderSize = ButtonSize + BorderCornerRadius;
    private const double BorderMarginRightBottom = -1 * (BorderCornerRadius + BorderThickness);
    private const double BorderMarginRight = -1 * (BorderCornerRadius + 2 * BorderThickness);

    public ProceedButton()
    {
        InitializeComponent();

        DisposableBindings = this.WhenActivated(d =>
        {
            d(this.BindCommandCustom(ProceedButtonTap, v => v.ViewModel.NavigateToPageCommand));
            d(this.OneWayBind(ViewModel, vm => vm.FlowDirection,
                v => v.ButtonBorder.Margin, SetThickness));
            d(this.OneWayBind(ViewModel, vm => vm.FlowDirection,
                v => v.ButtonDisabledOverlay.Margin, SetThickness));
            d(this.OneWayBind(ViewModel, vm => vm.BackgroundColor,
                v => v.ButtonFrame.BackgroundColor));
            d(this.OneWayBind(ViewModel, vm => vm.BorderColor,
                v => v.ButtonBorder.BackgroundColor));
            d(this.OneWayBind(ViewModel, vm => vm.Icon.Glyph,
                v => v.ButtonImage.Text));
            d(this.OneWayBind(ViewModel, vm => vm.Icon.Color,
                v => v.ButtonImage.TextColor));
            d(this.OneWayBind(ViewModel, vm => vm.ProceedActive,
                v => v.ButtonDisabledOverlay.Opacity, SetOverlayOpacity));
        });
    }

    // We have to set overlay visibility by Opacity, because changing IsVisible 
    // of ButtonDisabledOverlay with constant opacity 0.6 does not work correctly
    private double SetOverlayOpacity(bool isProceedActive)
    {
        return isProceedActive ? 0 : 0.6;
    }

    private Thickness SetThickness(FlowDirection flowDirection)
    {
        if (flowDirection == FlowDirection.LeftToRight)
        {
            return new Thickness(0, 0, BorderMarginRightBottom, BorderMarginRightBottom);
        }

        return new Thickness(0, 0, BorderMarginRight, BorderMarginRightBottom);
    }
}