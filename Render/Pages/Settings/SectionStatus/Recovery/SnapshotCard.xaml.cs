using System.Reactive.Linq;
using ReactiveUI;
using Render.Resources;

namespace Render.Pages.Settings.SectionStatus.Recovery;

    public partial class SnapshotCard
    {
        public SnapshotCard()
        {
            InitializeComponent();

            this.WhenActivated(d =>
            {
                d(this.OneWayBind(ViewModel, vm => vm.SnapshotDateLabel, 
                    v => v.SnapshotDateLabel.Text));
                d(this.OneWayBind(ViewModel, vm => vm.Stage.Name, 
                    v => v.StageLabel.Text));
                d(this.OneWayBind(ViewModel, vm => vm.ShowDescription, 
                    v => v.StageDescriptionLabel.IsVisible));
                d(this.OneWayBind(ViewModel, vm => vm.ShowDescription,
                    v => v.ToolTipButton.IsVisible));
                d(this.OneWayBind(ViewModel, vm => vm.Last, 
                    v => v.BottomLine.BackgroundColor, Selector));
                d(this.BindCommand(ViewModel, vm => vm.SelectSnapshotCommand,
                    v => v.TapGestureRecognizer));
                d(this.BindCommand(ViewModel, vm => vm.RestoreSnapshotCommand,
                    v => v.RestoreButtonTap));
                d(this.OneWayBind(ViewModel, vm => vm.IsSelected,
                    v => v.RestoreButton.IsVisible));
                d(this.WhenAnyValue(x => x.ViewModel.IsSelected)
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Subscribe(SetColors));
                d(this.WhenAnyValue(x => x.ViewModel.CurrentSnapshot)
                    .Subscribe(x => 
                    {
                        MiddleCircle.BackgroundColor = x
                            ? ResourceExtensions.GetColor("SelectedBreathPauseBar")
                            : ResourceExtensions.GetColor("SeparatorBar");
                    }));
            });
            
            SizeChanged += OnSizeChanged;
            
        }
        
        private void SetColors(bool isSelected)
        {
            SnapshotStack.BackgroundColor = isSelected
                ? ResourceExtensions.GetColor("SecondaryText")
                : ResourceExtensions.GetColor("AlternateBackground");
        }
        
        private void OnSizeChanged(object sender, EventArgs e)
        {
            Card.WidthRequest = Application.Current?.MainPage?.Width * (2.0 / 3.0) - 60 ?? Card.WidthRequest;
        }
        
        private Color Selector(bool arg)
        {
            return arg ?
                Colors.Transparent :
                 ResourceExtensions.GetColor("AlternateButton");
        }
    }