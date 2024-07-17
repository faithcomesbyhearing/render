using System.Reactive.Linq;
using ReactiveUI;
using Render.Resources;

namespace Render.Pages.Settings.SectionStatus.Processes
{
    public partial class SectionCollectionAsStageCard
    {
        public SectionCollectionAsStageCard()
        {
            InitializeComponent();
            this.WhenActivated(d =>
            {
                d(this.OneWayBind(ViewModel, vm => vm.ShowSections,
                    v => v.SectionCollection.IsVisible));
                d(this.OneWayBind(ViewModel, vm => vm.HasSections,
                    v => v.StageCardBarSeparator.IsVisible));
                d(this.OneWayBind(ViewModel, vm => vm.Title,
                    v => v.Title.Text));
                d(this.OneWayBind(ViewModel, vm => vm.Glyph, 
                    v => v.IconLabel.Text));
                d(this.BindCommand(ViewModel, vm => vm.ToggleStepsCommand,
                    v => v.TapGestureRecognizer));
                d(this.WhenAnyValue(x => x.ViewModel.Sections)
                    .Subscribe(source => { BindableLayout.SetItemsSource(SectionCollection, source); }));
                d(this.WhenAnyValue(x => x.ViewModel.ShowSections)
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Subscribe(SetColors));
            });
        }

        private void SetColors(bool showingSteps)
        {
            ListIcon.Text = showingSteps
                ? IconExtensions.GetIconGlyph(Icon.Minus)
                : IconExtensions.GetIconGlyph(Icon.Plus);
            ListIcon.FontSize = showingSteps ? 4 : 26;
            StageCardBarSeparator.SetValue(IsVisibleProperty, ViewModel != null && ViewModel.HasSections && showingSteps);
        }
    }
}