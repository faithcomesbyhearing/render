using System.Reactive.Linq;
using ReactiveUI;
using Render.Resources;

namespace Render.Pages.Settings.SectionStatus.Processes
{
    public partial class StageCard
    {
        public StageCard()
        {
            InitializeComponent();
            this.WhenActivated(d =>
            {
                d(this.OneWayBind(ViewModel, vm => vm.Stage.Name,
                    v => v.StageName.Text));
                d(this.OneWayBind(ViewModel, vm => vm.Glyph,
                    v => v.StageIcon.Text));
                d(this.BindCommand(ViewModel, vm => vm.ToggleStepsCommand,
                    v => v.TapGestureRecognizer));
                d(this.WhenAnyValue(x => x.ViewModel.StepCards)
                    .Subscribe(source => { BindableLayout.SetItemsSource(StepCollection, source); }));
                d(this.OneWayBind(ViewModel, vm => vm.ShowSteps,
                    v => v.StepCollection.IsVisible));
                d(this.WhenAnyValue(x => x.ViewModel.ShowSteps)
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Subscribe(SetColors));
            });
        }

        private void SetColors(bool showingSteps)
        {
            StageListIcon.Text = showingSteps
                ? IconExtensions.GetIconGlyph(Icon.Minus)
                : IconExtensions.GetIconGlyph(Icon.Plus);
            StageListIcon.FontSize = showingSteps ? 4 : 26;
            StageTitleBarSeparator.SetValue(IsVisibleProperty, showingSteps);
        }
        
    }
}