using System.Reactive.Linq;
using ReactiveUI;
using Render.Extensions;
using Render.Resources;

namespace Render.Components.DivisionPlayer
{
    public partial class DivisionPlayer
    {
        private const double EnabledOpacity = 1d;
        private const double DisabledOpacity = 0.3d;
        
        public DivisionPlayer()
        {
            InitializeComponent();
            
            this.WhenActivated(d =>
            {
                d(this.WhenAnyValue(v => v.ViewModel.Duration)
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Subscribe(_ => CalculateWidth()));
                
                d(this.WhenAnyValue(v => v.ViewModel.IsLocked)
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Subscribe(_ => ChangeLockedState()));
                
                d(this.WhenAnyValue(x => x.ViewModel.BreathPauses.Items)
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Subscribe(breathPauses =>
                    {
                        var source = BindableLayout.GetItemsSource(BreathPauseList);
                        if (source == null)
                        {
                            BindableLayout.SetItemsSource(BreathPauseList, breathPauses);
                        }
                    }));
                
                d(this.WhenAnyValue(x => x.ViewModel.Divisions)
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Subscribe(divisions =>
                    {
                        var source = BindableLayout.GetItemsSource(DivisionList);
                        if (source == null)
                        {
                            BindableLayout.SetItemsSource(DivisionList, divisions);
                        }
                    }));

                d(this.OneWayBind(ViewModel, vm => vm.ReferenceName, v => v.ReferenceNameLabel.Text));
                d(this.Bind(ViewModel, vm => vm.CurrentPosition, v => v.AudioPlayerSlider.Value));
                d(this.OneWayBind(ViewModel, vm => vm.Duration, v => v.AudioPlayerSlider.Maximum));
                d(this.OneWayBind(ViewModel, vm => vm.ShowPlayButton, v => v.PlayButton.IsVisible));
                d(this.OneWayBind(ViewModel, vm => vm.ShowPauseButton, v => v.PauseButton.IsVisible));
                d(this.OneWayBind(ViewModel, vm => vm.Loading, v => v.LoadingIndicator.IsVisible));
                d(this.BindCommand(ViewModel, vm => vm.PlayAudioCommand, v => v.PlayGesture));
                d(this.BindCommand(ViewModel, vm => vm.PauseAudioCommand, v => v.PauseGesture));
                d(this.BindCommand(ViewModel, vm => vm.PauseOnSeekCommand, v => v.AudioPlayerSlider,
                    nameof(Slider.DragStarted)));
                d(this.BindCommand(ViewModel, vm => vm.SeekCommand, v => v.AudioPlayerSlider,
                    nameof(Slider.DragCompleted)));
                d(this.OneWayBind(ViewModel, vm => vm.CurrentPosition, v => v.TimerLabel.Text, FormatTime));
                d(this.OneWayBind(ViewModel, vm => vm.Duration, v => v.DurationLabel.Text, FormatTime));
                d(this.BindCommand(ViewModel, vm => vm.ChangeLockStateCommand, v => v.ReferenceNameTap));
                
                d(this.OneWayBind(ViewModel, vm => vm.ScrollerViewModel, v => v.Scroller.BindingContext));
                d(this.Bind(ViewModel, vm => vm.ScrollerViewModel.WidthRatio, 
                    v => v.PlayerScrollView.WidthRatio));
                d(this.Bind(ViewModel, vm => vm.ScrollerViewModel.VisibleScreenWidth,
                    v => v.PlayerScrollView.Width));
                d(this.Bind(ViewModel, vm => vm.ScrollerViewModel.TotalWidth, 
                    v => v.PlayerScrollView.TotalWidth));
                d(this.Bind(ViewModel, vm => vm.ScrollerViewModel.InputScrollX, 
                    v => v.PlayerScrollView.InputScrollX));
                d(this.Bind(ViewModel, vm => vm.ScrollerViewModel.OutputScrollX, 
                    v => v.PlayerScrollView.ScrollX));
                d(this.Bind(ViewModel, vm => vm.FlowDirection, 
                    v => v.ReferencenameStack.FlowDirection));
            });
            
            MainGrid.SizeChanged += OnSizeChanged;
        }

        private void CalculateWidth()
        {
            var playerWidth = MainGrid.Bounds.Width - PlayerButtonsGrid.WidthRequest - MainGrid.Padding.Left -
                              MainGrid.Padding.Right - MainGrid.ColumnSpacing * 2;
            
            if (ViewModel?.IsAudioAvailable is true)
            {
                PlayerGrid.WidthRequest = ViewModel.Duration * playerWidth / ViewModel.SecondsPerScreen;
            }
            else
            {
                PlayerGrid.WidthRequest = PlayerScrollView.Width > 0 ? PlayerScrollView.Width : -1;
            }
            ViewModel.UpdateScale(playerWidth / ViewModel.SecondsPerScreen);
        }
        
        private void ChangeLockedState()
        {
            BreathPauseList.IsVisible = !ViewModel.IsLocked;
            
            Scroller.IsEnabled = !ViewModel.IsLocked;
            AudioPlayerSlider.IsEnabled = !ViewModel.IsLocked;
            
            var opacity = GetOpacity(!ViewModel.IsLocked);

            AudioPlayerSlider.Opacity = opacity;
            TimerLabel.Opacity = opacity;
            DurationLabel.Opacity = opacity;
            PlayerButtonsGrid.Opacity = opacity;
            Scroller.Opacity = opacity;

            var redColor = ResourceExtensions.GetColor("LockedDivisionPlayerReference");
            var blueColor = ResourceExtensions.GetColor("UnselectedDivisionMarker");

            ReferenceIcon.TextColor = ViewModel.IsLocked ? redColor : blueColor;
            ReferenceNameLabel.TextColor = ViewModel.IsLocked ? redColor : blueColor;
            LockIcon.TextColor = ViewModel.IsLocked ? redColor : blueColor;

            LockIcon.Text = ViewModel.IsLocked ? IconExtensions.GetIconGlyph(Icon.Lock) : IconExtensions.GetIconGlyph(Icon.Unlock);
        }

        private double GetOpacity(bool isEnabled)
        {
            return isEnabled ? EnabledOpacity : DisabledOpacity;
        }
        
        private void OnSizeChanged(object sender, EventArgs e)
        {
            CalculateWidth();
        }
        
        private string FormatTime(double arg)
        {
            var timeSpan = TimeSpan.FromSeconds(arg);
            return timeSpan.ToString(@"hh\:mm\:ss");
        }
        
        protected override void Dispose(bool disposing)
        {
            BreathPauseList
                .Cast<BreathPause>()
                .ForEach(bp => bp.Dispose());
            
            DivisionList
                .Cast<Division>()
                .ForEach(d => d.Dispose());

            base.Dispose(disposing);
        }

        private void BreathPauseListTapped(object sender, TappedEventArgs e)
        {
            var tapPosition = e.GetPosition(BreathPauseList)?.X;
            if (tapPosition != null)
            {
                AudioPlayerSlider.Value = tapPosition.Value / AudioPlayerSlider.Bounds.Width * ViewModel.Duration;
            }
        }
    }
}