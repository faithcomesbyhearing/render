using System.Reactive.Linq;
using ReactiveUI;
using Render.Kernel;
using Render.Resources;
using SkiaSharp.Views.Maui;

namespace Render.Components.BarPlayer
{
    public partial class BarPlayer
    {
        public const double PlayerHeight = 80;
        
        public static readonly BindableProperty ThumbColorProperty = BindableProperty.Create(
            nameof(ThumbColor),
            typeof(Color),
            typeof(BarPlayer),
            ResourceExtensions.GetColor("AudioPlayerThumbBackground"));

        public static readonly BindableProperty MinimumTrackColorProperty = BindableProperty.Create(
            nameof(MinimumTrackColor),
            typeof(Color),
            typeof(BarPlayer),
            ResourceExtensions.GetColor("AudioPlayerSliderBeforeBackground"));

        public static readonly BindableProperty MaximumTrackColorProperty = BindableProperty.Create(
            nameof(MaximumTrackColor),
            typeof(Color),
            typeof(BarPlayer),
            ResourceExtensions.GetColor("AudioPlayerSliderAfterBackground"));

        public static readonly BindableProperty PlayerLabelColorProperty = BindableProperty.Create(
            nameof(PlayerLabelColor),
            typeof(Color),
            typeof(BarPlayer),
            ResourceExtensions.GetResourceValue<Color>("SlateDark"));

        public static readonly BindableProperty TimerLabelColorProperty = BindableProperty.Create(
            nameof(TimerLabelColor),
            typeof(Color),
            typeof(BarPlayer),
            ResourceExtensions.GetResourceValue<Color>("SlateLight"));

        public static readonly BindableProperty PlayerButtonColorProperty = BindableProperty.Create(
            nameof(PlayerButtonColor),
            typeof(Color),
            typeof(BarPlayer),
            ResourceExtensions.GetColor("Option"));

        public static readonly BindableProperty MainStackBackgroundColorProperty = BindableProperty.Create(
            nameof(MainStackBackgroundColor),
            typeof(Color),
            typeof(BarPlayer),
            ResourceExtensions.GetColor("OptionAudioPlayerBackground"));

        public static readonly BindableProperty MainStackBorderColorProperty = BindableProperty.Create(
            nameof(MainStackBorderColor),
            typeof(Color),
            typeof(BarPlayer),
            ResourceExtensions.GetResourceValue<Color>("Gray"));

        public static readonly BindableProperty SeparatorBorderColorProperty = BindableProperty.Create(
            nameof(SeparatorBorderColor),
            typeof(Color),
            typeof(BarPlayer),
            ResourceExtensions.GetResourceValue<Color>("Gray"));

        public static readonly BindableProperty EnableSeparatorGradientProperty = BindableProperty.Create(
            nameof(EnableSeparatorGradient),
            typeof(bool),
            typeof(BarPlayer),
            false);

        public static readonly BindableProperty ContentBeforePlayerProperty = BindableProperty.Create(
            nameof(ContentBeforePlayer),
            typeof(View),
            typeof(BarPlayer));

        public static readonly BindableProperty ContentOverlappingPlayerProperty = BindableProperty.Create(
            nameof(ContentOverlappingPlayer),
            typeof(View),
            typeof(BarPlayer));

        private Animation _spinAnimation;
        private string _spinAnimationHandle;

        public Color MinimumTrackColor
        {
            get => (Color)GetValue(MinimumTrackColorProperty);
            set => SetValue(MinimumTrackColorProperty, value);
        }

        public Color MaximumTrackColor
        {
            get => (Color)GetValue(MaximumTrackColorProperty);
            set => SetValue(MaximumTrackColorProperty, value);
        }

        public Color PlayerLabelColor
        {
            get => (Color)GetValue(PlayerLabelColorProperty);
            set => SetValue(PlayerLabelColorProperty, value);
        }

        public Color TimerLabelColor
        {
            get => (Color)GetValue(TimerLabelColorProperty);
            set => SetValue(TimerLabelColorProperty, value);
        }

        public Color PlayerButtonColor
        {
            get => (Color)GetValue(PlayerButtonColorProperty);
            set => SetValue(PlayerButtonColorProperty, value);
        }

        public Color MainStackBackgroundColor
        {
            get => (Color)GetValue(MainStackBackgroundColorProperty);
            set => SetValue(MainStackBackgroundColorProperty, value);
        }

        public Color MainStackBorderColor
        {
            get => (Color)GetValue(MainStackBorderColorProperty);
            set => SetValue(MainStackBorderColorProperty, value);
        }

        public Color SeparatorBorderColor
        {
            get => (Color)GetValue(SeparatorBorderColorProperty);
            set => SetValue(SeparatorBorderColorProperty, value);
        }

        public bool EnableSeparatorGradient
        {
            get => (bool)GetValue(EnableSeparatorGradientProperty);
            set => SetValue(EnableSeparatorGradientProperty, value);
        }

        public View ContentBeforePlayer
        {
            get => (View)GetValue(ContentBeforePlayerProperty);
            set => SetValue(ContentBeforePlayerProperty, value);
        }

        public View ContentOverlappingPlayer
        {
            get => (View)GetValue(ContentOverlappingPlayerProperty);
            set => SetValue(ContentOverlappingPlayerProperty, value);
        }

        public Color ThumbColor
        {
            get => (Color)GetValue(ThumbColorProperty);
            set => SetValue(ThumbColorProperty, value);
        }

        public BarPlayer()
        {
            InitializeComponent();

            Unloaded += BarPlayerUnLoaded;
            
            this.WhenActivated(d =>
            {
                d(this.Bind(ViewModel, vm => vm.CurrentPosition, v => v.AudioPlayerSlider.Value));
                d(this.OneWayBind(ViewModel, vm => vm.Glyph, v => v.AudioPlayerGlyph.Text));
                d(this.OneWayBind(ViewModel, vm => vm.AudioTitle, v => v.AudioPlayerLabel.Text));
                d(this.OneWayBind(ViewModel, vm => vm.Duration, v => v.AudioPlayerSlider.Maximum));
                d(this.OneWayBind(ViewModel, vm => vm.ShowPlayButton, v => v.PlayButton.IsVisible));
                d(this.OneWayBind(ViewModel, vm => vm.ShowPauseButton, v => v.PauseButton.IsVisible));
                d(this.OneWayBind(ViewModel, vm => vm.CurrentPosition, v => v.Timer.Text, FormatTime));
                d(this.OneWayBind(ViewModel, vm => vm.Duration, v => v.Duration.Text, FormatTime));
                d(this.OneWayBind(ViewModel, vm => vm.Loading, v => v.LoadingIndicator.IsVisible));
                d(this.OneWayBind(ViewModel, vm => vm.ShowSecondaryButton, v => v.SecondaryStack.IsVisible));
                d(this.BindCommand(ViewModel, vm => vm.PlayAudioCommand, v => v.PlayGesture));
                d(this.BindCommand(ViewModel, vm => vm.PauseAudioCommand, v => v.PauseGesture));
                d(this.BindCommand(ViewModel, vm => vm.PauseOnSeekCommand, v => v.AudioPlayerSlider, nameof(Slider.DragStarted)));
                d(this.BindCommand(ViewModel, vm => vm.SeekCommand, v => v.AudioPlayerSlider, nameof(Slider.DragCompleted)));
                
                d(this
                    .WhenAnyValue(x => x.ViewModel)
                    .Subscribe(vm =>
                    {
                        if (vm != null && vm.SecondaryButtonClickCommand != null && vm.SecondaryButtonIcon != null)
                        {
                            SecondaryButton.Text = ((FontImageSource)vm.SecondaryButtonIcon).Glyph;
                            SecondaryGesture.Command = vm.SecondaryButtonClickCommand;
                        }
                    }));

                d(this
                    .WhenAnyValue(x => x.ViewModel.Glyph)
                    .Subscribe(glyph =>
                    {
                        AudioPlayerGlyph.IsVisible = string.IsNullOrEmpty(glyph) == false;
					}));

				d(this
				   .WhenAnyValue(x => x.ViewModel.GlyphColor)
				   .Subscribe(color =>
				   {
					   if (ViewModel != null && ViewModel.Glyph != null)
					   {
						   AudioPlayerGlyph.TextColor = color;
					   }
				   }));

				d(this
                    .WhenAnyValue(x => x.ViewModel.SecondaryButtonBackgroundColor)
                    .Subscribe(s =>
                    {
                        if (ViewModel != null && ViewModel.SecondaryButtonIcon != null)
                        {
                            SecondaryStack.SetValue(BackgroundColorProperty, s);
                        }
                    }));

                d(this
                    .WhenAnyValue(x => x.ViewModel.Loading)
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Subscribe(loading =>
                    {
                        SetOpacityOfPlayer(!loading);
                    }));

                d(this
                    .WhenAnyValue(p => p.ViewModel.ActionState)
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Subscribe((actionState) =>
                    {
                        var style = ResourceExtensions.GetResourceValue<Style>(
                            resources: Resources,
                            keyName: !(ContentBeforePlayer?.IsVisible ?? false) && actionState is ActionState.Required
                                ? "RequiredPlayerButton"
                                : "PlayerButton");

                        PlayButton.Style = PauseButton.Style = style;
                    }));

                d(this
                    .WhenAnyValue(x => x.ViewModel.PassagePainter.Duration)
                    .Subscribe(n => { MiniPassageDivideCanvas.InvalidateSurface(); }));

                d(this
                    .WhenAnyValue(x => x.ViewModel.PassagePainter.Markers)
                    .Subscribe(l => { MiniPassageDivideCanvas.InvalidateSurface(); }));

                d(this
                    .WhenAnyValue(x => x.ViewModel.ActionState)
                    .Subscribe(a => { MiniPassageDivideCanvas.InvalidateSurface(); }));

				d(this
				   .WhenAnyValue(x => x.ViewModel.Loading)
				   .Subscribe(a => { MiniPassageDivideCanvas.InvalidateSurface(); }));

				d(this
                    .WhenAnyValue(x => x.ViewModel.Loading)
                    .Subscribe(AnimateLoadingIcon));

                d(this
                    .WhenAnyValue(x => x.EnableSeparatorGradient)
                    .Subscribe(hasGradient =>
                    {
                        Separator.IsVisible = !hasGradient;
                        GradientSeparator.IsVisible = hasGradient;
                    }));
            });
        }

        private void BarPlayerUnLoaded(object sender, EventArgs e)
        {
            DestroyAnimation();
        }

        protected override void OnPropertyChanged(string propertyName = null)
        {
            base.OnPropertyChanged(propertyName);

            if (propertyName == ContentBeforePlayerProperty.PropertyName)
            {
                ContentBeforePlayerPlaceholder.Children.Add(ContentBeforePlayer);
                ContentBeforePlayerPlaceholder.IsVisible = true;
            }

            if (propertyName == ContentOverlappingPlayerProperty.PropertyName)
            {
                ContentOverlappingPlayerPlaceholder.Children.Add(ContentOverlappingPlayer);
                ContentOverlappingPlayerPlaceholder.IsVisible = true;
            }
        }

        private void AnimateLoadingIcon(bool loading)
        {
            if (ViewModel == null)
            {
                return;
            }

            DestroyAnimation();

            // here we check IsLoaded because visual element can be hidden (empty section), loading will be always true and we will start animation that shouldn't be created
            if (IsLoaded && loading && ViewModel != null)
            {
                _spinAnimationHandle = Guid.NewGuid().ToString();
                _spinAnimation = new Animation(v => LoadingIndicator.Rotation = v, 0, 360, Easing.Linear);
                _spinAnimation?.Commit(this, _spinAnimationHandle, length: 2000, repeat: () => ViewModel?.Loading == true);
            }
        }

        private string FormatTime(double arg)
        {
            var timeSpan = TimeSpan.FromSeconds(arg);
            return timeSpan.ToString(@"mm\:ss");
        }

        protected override void OnButtonClicked(object sender, EventArgs e)
        {
            var element = (Element)sender;
            var name = element.AutomationId;
            var properties = new Dictionary<string, string>
            {
                { "Button Name", name },
                { "Bar Player Title", ViewModel.AudioTitle },
                { "Bar Player Position", ViewModel.PlayerPositionInList.ToString() }
            };

            LogInfo("Bar Player Button Clicked", properties);
            base.OnButtonClicked(sender, e);
        }

        private void SetOpacityOfPlayer(bool isActive)
        {
            var opacity = isActive ? 1 : 0.4;

            if (DeviceInfo.Platform == DevicePlatform.WinUI)
            {
                AudioPlayerSlider.SetValue(OpacityProperty, opacity);
            }

            AudioPlayerLabel.SetValue(OpacityProperty, opacity);
            Timer.SetValue(OpacityProperty, opacity);
            Duration.SetValue(OpacityProperty, opacity);
            PlayButton.SetValue(OpacityProperty, opacity);

            SetValue(IsEnabledProperty, isActive);
        }

        private void OnPaintSurface(object sender, SKPaintSurfaceEventArgs e)
        {
            if (ViewModel?.PaintPassageMarkers == true)
            {
                var canvas = e.Surface.Canvas;
                var visualElement = (VisualElement)sender;

                ViewModel.PassagePainter?.Paint(canvas, canvas.DeviceClipBounds, visualElement.HeightRequest);
            }
        }

        private void DestroyAnimation()
        {
            if (string.IsNullOrEmpty(_spinAnimationHandle) is false)
            {
                this.AbortAnimation(_spinAnimationHandle);
            }

            _spinAnimation?.Dispose();
            _spinAnimation = null;
            _spinAnimationHandle = null;
        }
    }
}