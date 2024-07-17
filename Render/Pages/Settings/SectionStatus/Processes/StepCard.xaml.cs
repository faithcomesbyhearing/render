using ReactiveUI;
using Render.Resources;
using Render.Resources.Styles;
using System.Reactive.Linq;

namespace Render.Pages.Settings.SectionStatus.Processes
{
    public partial class StepCard
    {
        public StepCard()
        {
            InitializeComponent();
            this.WhenActivated(d =>
            {
                d(this.OneWayBind(ViewModel, vm => vm.ShowSections,
                    v => v.SectionCollection.IsVisible));
                d(this.OneWayBind(ViewModel, vm => vm.StepName,
                    v => v.StepName.Text));
                d(this.BindCommand(ViewModel, vm => vm.ToggleSectionsCommand,
                    v => v.TapGestureRecognizer));
                d(this.OneWayBind(ViewModel, vm => vm.LastStepCard,
                    v => v.StepTitleBarSeparator.IsVisible, ShowOrHideStepTitleSeparator));
                d(this.WhenAnyValue(x => x.ViewModel.SectionCards)
                    .Subscribe(source => { BindableLayout.SetItemsSource(SectionCollection, source); }));
                d(this.WhenAnyValue(x => x.ViewModel.ShowSections)
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Subscribe(SetColors));
            });
        }

        private bool ShowOrHideStepTitleSeparator(bool show)
        {
            return !show;
        }

        private void SetColors(bool showingSections)
        {
            StepListIcon.FontSize = showingSections ? 4 : 26;
            StepListIcon.HeightRequest = StepListIcon.FontSize;
            if (showingSections)
            {
                StepTitle.BackgroundColor =
                    ((ColorReference)ResourceExtensions.GetResourceValue("Option")).Color;
                var textColor = ((ColorReference)ResourceExtensions.GetResourceValue("SecondaryText")).Color;
                StepTitleFrame.Margin = new Thickness(0, -4, 0, 0);
                StepName.TextColor = textColor;
                StepListIcon.Text = IconExtensions.BuildFontImageSource(Icon.Minus).Glyph;
                StepListIcon.TextColor = textColor;
            }
            else
            {
                StepTitleFrame.Margin = new Thickness(0, 0, 0, 0);
                StepTitle.BackgroundColor =
                    ((ColorReference)ResourceExtensions.GetResourceValue("UnselectedCard")).Color;
                var textColor = ((ColorReference)ResourceExtensions.GetResourceValue("MainIconColor")).Color;
                StepName.TextColor = textColor;
                StepListIcon.Text = IconExtensions.BuildFontImageSource(Icon.Plus, textColor).Glyph;
                StepListIcon.TextColor = textColor;
            }
        }
    }
}