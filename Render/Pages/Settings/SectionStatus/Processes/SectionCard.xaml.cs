using System.Reactive.Linq;
using ReactiveUI;
using Render.Resources;
using Render.Resources.Styles;

namespace Render.Pages.Settings.SectionStatus.Processes
{
    public partial class SectionCard
    {
        public SectionCard()
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
                        CenterIcon.IsVisible = true;
                        CenterIcon.Text = IconExtensions.GetIconGlyph(Icon.InvalidInput);
                        CenterIcon.FontSize = 30;
                    }
                    else if (tuple.Item1)
                    {
                        CenterIcon.IsVisible = true;
                        CenterIcon.Text = IconExtensions.GetIconGlyph(Icon.FinishedPassOrSubmit);
                        CenterIcon.FontSize = 35;
                    }
                    else
                    {
                        CenterIcon.TextColor = ResourceExtensions.GetColor("Transparent") ?? new ColorReference();
                    }
                }));
            });
        }

        private void SetColors(bool isSelected)
        {
            NoSectionLabel.SetValue(IsVisibleProperty, ViewModel?.Section == null);
            Line.SetValue(IsVisibleProperty, !NoSectionLabel.IsVisible);
            if (ViewModel?.Section != null)
            {
                SectionNumber.Text = ViewModel.Section.Number.ToString();
                SectionScriptureReference.Text = ViewModel.ScriptureRange;
                SectionTitle.Text = ViewModel.Section.Title.Text;
                Line.SetValue(IsVisibleProperty, !ViewModel.IsLastSectionCard);

                var red = ResourceExtensions.GetColor("Error");
                if (isSelected)
                {
                    var blue = ResourceExtensions.GetColor("Option");
                    var white = ResourceExtensions.GetColor("SecondaryText");
                    Card.BackgroundColor = blue;
                    SectionScriptureReference.TextColor = white;
                    SectionNumber.TextColor = white;
                    CenterIcon.TextColor = ViewModel != null && ViewModel.HasConflict ? red : white;
                }
                else
                {
                    var backgroundColor = ((ColorReference)ResourceExtensions
                        .GetResourceValue("Transparent")).Color;
                    var cardTextColor = ((ColorReference)ResourceExtensions.GetResourceValue("CardText")).Color;
                    Card.BackgroundColor = backgroundColor;
                    SectionScriptureReference.TextColor = cardTextColor;
                    SectionNumber.TextColor = cardTextColor;
                    CenterIcon.TextColor = ViewModel != null && ViewModel.HasConflict ? red : cardTextColor;
                }
            }
        }
    }
}