using System.Reactive.Linq;
using ReactiveUI;
using Render.Resources;
using Render.Resources.Styles;

namespace Render.Pages.Settings.SectionStatus.Recovery;

public partial class RecoverySectionCard 
{
    public RecoverySectionCard()
    {
        InitializeComponent();
        this.WhenActivated(d =>
        {
            d(this.BindCommand(ViewModel, vm => vm.SelectSectionCommand,
                v => v.TapGestureRecognizer));
            d(this.WhenAnyValue(x => x.ViewModel.IsSelected)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(SetColors));
            d(this.WhenAnyValue(x => x.ViewModel.IsApproved,
                x => x.ViewModel.HasConflict).Subscribe(tuple =>
            {
                if (tuple.Item2)
                {
                    ConflictIcon.IsVisible = true;
                    ApprovedIcon.IsVisible = false;
                }
                else if (tuple.Item1)
                {
                    ConflictIcon.IsVisible = false;
                    ApprovedIcon.IsVisible = true;
                }
                else
                {
                    ConflictIcon.IsVisible = false;
                    ApprovedIcon.IsVisible = false;
                }
            }));
        });
    }
    
    private void SetColors(bool isSelected)
    {
        NoSectionLabel.SetValue(IsVisibleProperty, ViewModel?.Section == null);
        if (ViewModel?.Section != null)
        {
            SectionNumber.Text = ViewModel.Section.Number.ToString();
            SectionScriptureReference.Text = ViewModel.ScriptureRange;
            SectionTitle.Text = ViewModel.Section.Title.Text;


            var red = ResourceExtensions.GetColor("Error");
            var slateDark = ResourceExtensions.GetColor("MainIconColor");
            if (isSelected)
            {
                var blue = ResourceExtensions.GetColor("Option");
                var white = ResourceExtensions.GetColor("SecondaryText");
                Card.BackgroundColor = blue;
                SectionScriptureReference.TextColor = white;
                SectionNumber.TextColor = white;
                ConflictIcon.TextColor = ViewModel is { HasConflict: true } ? red : white;
                ApprovedIcon.TextColor = ViewModel is { IsApproved: true } ? white : slateDark;
                ApprovedIcon.Opacity = 1;
            }
            else
            {
                var backgroundColor = ((ColorReference)ResourceExtensions
                    .GetResourceValue("Transparent")).Color;
                var cardTextColor = ((ColorReference)ResourceExtensions.GetResourceValue("CardText")).Color;
                Card.BackgroundColor = backgroundColor;
                SectionScriptureReference.TextColor = cardTextColor;
                SectionNumber.TextColor = cardTextColor;
                ConflictIcon.TextColor = ViewModel is { HasConflict: true } ? red : cardTextColor;
                ApprovedIcon.TextColor = slateDark;
                ApprovedIcon.Opacity = 0.8;
            }
        }
    }
}