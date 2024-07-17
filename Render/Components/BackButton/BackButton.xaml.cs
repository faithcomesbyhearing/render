using ReactiveUI;
using Render.Kernel.WrappersAndExtensions;
using Render.Resources;

namespace Render.Components.BackButton;

public partial class BackButton
{
    public BackButton()
    {
        InitializeComponent();

        DisposableBindings = this.WhenActivated(d =>
        {
            d(this.BindCommandCustom(BackButtonTap, v => v.ViewModel.NavigateBackCommand));
            d(this.OneWayBind(ViewModel, vm => vm.FlowDirection, v => v.BackButtonIcon.Text, SetBackButtonDirection));
        });
    }

    private static string SetBackButtonDirection(FlowDirection flowDirection)
    {
        return flowDirection == FlowDirection.RightToLeft
            ? IconExtensions.GetIconGlyph(Icon.ChevronRight)
            : IconExtensions.GetIconGlyph(Icon.ChevronLeft);
    }
}