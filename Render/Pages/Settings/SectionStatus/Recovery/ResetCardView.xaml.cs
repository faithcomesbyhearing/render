using ReactiveUI;
using Render.Resources;

namespace Render.Pages.Settings.SectionStatus.Recovery;

public partial class ResetCard
{
    public ResetCard()
    {
        InitializeComponent();
        this.WhenActivated(d =>
        {
            d(this.BindCommand(ViewModel, vm => vm.ResetSectionCommand,
                v => v.TapGestureRecognizer));

            d(this.OneWayBind(ViewModel, vm => vm.SectionHasSnapshots,
                v => v.BottomLine.BackgroundColor, Selector));
        });

        SizeChanged += OnSizeChanged;
    }

    private static Color Selector(bool arg)
    {
        return arg ? ResourceExtensions.GetColor("AlternateButton") : Colors.Transparent;
    }

    private void OnSizeChanged(object sender, EventArgs e)
    {
        Card.WidthRequest = Application.Current?.MainPage?.Width * (2.0 / 3.0) - 60 ?? Card.WidthRequest;
    }
}