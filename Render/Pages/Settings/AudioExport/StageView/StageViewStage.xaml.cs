using System;
using System.Reactive.Linq;
using ReactiveUI;
using Render.Resources;

namespace Render.Pages.Settings.AudioExport.StageView
{
    public partial class StageViewStage
    {
        public StageViewStage()
        {
            InitializeComponent();
            this.WhenActivated(d =>
            {
                d(this.OneWayBind(ViewModel, vm => vm.Icon.Glyph, v => v.StageIcon.Text));
                d(this.OneWayBind(ViewModel, vm => vm.Expand, v => v.SectionList.IsVisible));
                d(this.OneWayBind(ViewModel, vm => vm.Stage.Name, v => v.StageName.Text));
                d(this.BindCommand(ViewModel, vm => vm.ToggleExpandCommand, v => v.TapGestureRecognizer));
                d(this.WhenAnyValue(x => x.ViewModel.SectionCards)
                    .Subscribe(source =>
                    {
                        BindableLayout.SetItemsSource(SectionList, source);
                    }));
                
                d(this.WhenAnyValue(x => x.ViewModel.Expand)
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Subscribe(SetIconState));
            });
        }

        private void SetIconState(bool showingSteps)
        {
            StageListIcon.Text = showingSteps
                ? IconExtensions.GetIconGlyph(Icon.Minus)
                : IconExtensions.GetIconGlyph(Icon.Plus);
            StageListIcon.FontSize = showingSteps ? 4 : 26;
            StageTitleBarSeparator.SetValue(IsVisibleProperty, showingSteps);
        }
    }
}