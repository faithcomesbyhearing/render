using ReactiveUI;
using Render.Resources;

namespace Render.Pages.Translator.DraftingPage
{
    public partial class DraftReplacementCard
    {
        public DraftReplacementCard()
        {
            InitializeComponent();
            this.WhenActivated(d =>
            {
                d(this.OneWayBind(ViewModel, vm => vm.Title, v => v.Label.Text));
                d(this.OneWayBind(ViewModel, vm => vm.IsPreviousDraft, v => v.PreviousDraftStar.IsVisible));

                d(this.BindCommand(ViewModel, vm => vm.SelectCommand, v => v.Tap));

                d(this.WhenAnyValue(x => x.ViewModel.Selected, x => x.ViewModel.IsPreviousDraft)
                      .Subscribe(state => ChangeState(state)));
            });
        }

        private void ChangeState((bool IsSelected, bool IsPreviousDraft) state)
        {
            var blueColor = ResourceExtensions.GetColor("Option");
            var whiteColor = ResourceExtensions.GetColor("SecondaryText");
            var greyColor = ResourceExtensions.GetColor("DraftDeselectedOutline");


            if (state.IsPreviousDraft)
            {
                DraftCard.SetValue(OpacityProperty, 0.5);
                DraftCard.SetValue(Border.StrokeProperty, greyColor);
                DraftCard.SetValue(BackgroundColorProperty, blueColor);
                WaveformIcon.SetValue(Label.TextColorProperty, whiteColor);
                Label.SetValue(Label.TextColorProperty, whiteColor);
                PreviousDraftStar.SetValue(Label.TextColorProperty, whiteColor);

                return;
            }

            if (state.IsSelected)
            {
                DraftCard.SetValue(Border.StrokeProperty, blueColor);
                DraftCard.SetValue(BackgroundColorProperty, blueColor);
                WaveformIcon.SetValue(Label.TextColorProperty, whiteColor);
                Label.SetValue(Label.TextColorProperty, whiteColor);
            }
            else
            {
                DraftCard.SetValue(Border.StrokeProperty, greyColor);
                DraftCard.SetValue(BackgroundColorProperty, whiteColor);
                WaveformIcon.SetValue(Label.TextColorProperty, blueColor);
                Label.SetValue(Label.TextColorProperty, blueColor);
            }
        }
    }
}