using System.Reactive.Linq;
using ReactiveUI;
using Render.Components.AudioRecorder.WaveformPainter;
using Render.Services.AudioServices;
using Render.Resources;
using Render.Resources.Styles;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using Render.Kernel.WrappersAndExtensions;

namespace Render.Components.AudioRecorder
{
    public partial class NewMiniAudioRecorder
    {
        public static readonly BindableProperty ContainerFrameBorderColorProperty = BindableProperty.Create(
            nameof(ContainerFrameBorderColor),
            typeof(Color),
            typeof(NewMiniAudioRecorder), 
            defaultValue: ResourceExtensions.GetColor("RecorderBorderColor"));

        public Color ContainerFrameBorderColor
        {
            get => (Color)GetValue(ContainerFrameBorderColorProperty);
            set => SetValue(ContainerFrameBorderColorProperty, value);
        }

        public static readonly BindableProperty HideActionButtonsProperty = BindableProperty.Create(
            nameof(HideActionButtons),
            typeof(bool),
            typeof(NewMiniAudioRecorder));

        public bool HideActionButtons
        {
            get => (bool)GetValue(HideActionButtonsProperty);
            set => SetValue(HideActionButtonsProperty, value);
        }
        
        public static readonly BindableProperty HideTimerWhenNoAudioProperty = BindableProperty.Create(
            nameof(HideTimerWhenNoAudio),
            typeof(bool),
            typeof(NewMiniAudioRecorder));

        public bool HideTimerWhenNoAudio
        {
            get => (bool)GetValue(HideTimerWhenNoAudioProperty);
            set => SetValue(HideTimerWhenNoAudioProperty, value);
        }
        
        public NewMiniAudioRecorder()
        {
            InitializeComponent();
            
            this.WhenActivated(d =>
            {
                d(this.BindCommandCustom(PlayButton, v => v.ViewModel.PlayCommand));
                d(this.BindCommandCustom(PauseButton, v => v.ViewModel.PauseCommand));
                d(this.BindCommandCustom(DeleteButton, v => v.ViewModel.DeleteCommand));
                
                d(this.WhenAnyValue(x => x.ViewModel.RecordedFileDuration, x => x.ViewModel.CurrentPosition)
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Subscribe((timer) =>
                    {
                        if (ViewModel.AudioPlayer.AudioPlayerState == AudioPlayerState.Initial ||
                            (ViewModel.AudioPlayer.AudioPlayerState == AudioPlayerState.Paused && timer.Item2 == 0))
                        {
                            Timer.Text = Utilities.Utilities.GetFormattedTime(timer.Item1);
                        }
                        else
                        {
                            Timer.Text = Utilities.Utilities.GetFormattedTime(timer.Item2);
                        }
                    }));
                
                d(this.WhenAnyValue(x => x.ViewModel.AudioSamples)
                    .Subscribe(Redraw));
                d(this.WhenAnyValue(x => x.ViewModel.AudioRecorderState)
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Subscribe(OnAudioRecorderStateChange));
            });
        }
        
        private void OnAudioRecorderStateChange(AudioRecorderState state)
        {
            switch (state)
            {
                case AudioRecorderState.NoAudio:
                case AudioRecorderState.CanAppendAudio:
                    if (HideTimerWhenNoAudio)
                    {
                        Timer.SetValue(OpacityProperty, 0);
                    }

                    DeleteButton.SetValue(OpacityProperty, 0);
                    DeleteButton.SetValue(IsEnabledProperty, false);
                    PlayButton.SetValue(IsVisibleProperty, false);
                    PauseButton.SetValue(IsVisibleProperty, false);

                    break;
                case AudioRecorderState.Recording:
                    Timer.SetValue(OpacityProperty, 1);
                    DeleteButton.SetValue(OpacityProperty, 0);
                    DeleteButton.SetValue(IsEnabledProperty, false);
                    PlayButton.SetValue(IsVisibleProperty, false);
                    PauseButton.SetValue(IsVisibleProperty, false);
                    break;
                case AudioRecorderState.CanPlayAudio:
                    Timer.SetValue(OpacityProperty, 1);
                    DeleteButton.SetValue(OpacityProperty, 1);
                    DeleteButton.SetValue(IsEnabledProperty, true);
                    PlayButton.SetValue(IsVisibleProperty, true);
                    PauseButton.SetValue(IsVisibleProperty, false);
                    break;
                case AudioRecorderState.PlayingAudio:
                    Timer.SetValue(OpacityProperty, 1);
                    DeleteButton.SetValue(OpacityProperty, 0.3);
                    DeleteButton.SetValue(IsEnabledProperty, false);
                    PlayButton.SetValue(IsVisibleProperty, false);
                    PauseButton.SetValue(IsVisibleProperty, true);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(state), state, null);
            }
            WaveformCanvas.InvalidateSurface();
        }

        private void Redraw(float[] data)
        {
            WaveformCanvas.InvalidateSurface();
        }
        
        private double _rectWidth;
        private void OnPaintSurface(object sender, SKPaintSurfaceEventArgs e)
        {
            var dataPoints = ViewModel?.AudioSamples;
            if (dataPoints == null) return;
            
            var canvas = e.Surface.Canvas;
            var dimensions = canvas.DeviceClipBounds;

            var numberOfBars = 200;

            var totalBarWidth = (double)dimensions.Width / numberOfBars;
            _rectWidth = totalBarWidth * .5;
            var color = ((ColorReference)ResourceExtensions.GetResourceValue("WaveformBlueColor")).Color;
            var paint = new SKPaint
            {
                Color = color.ToSKColor(),
                IsAntialias = true,
                StrokeCap = SKStrokeCap.Round,
                StrokeWidth = (float) _rectWidth,
            };
            canvas.Clear();
            WaveformPainter.WaveformPainter waveformPainter;
            switch (ViewModel.AudioRecorderState)
            {
                case AudioRecorderState.TemporaryDeleted:
                case AudioRecorderState.NoAudio:
                case AudioRecorderState.Recording:
                case AudioRecorderState.CanAppendAudio:
                    waveformPainter = new RecordingWaveformPainter();
                    waveformPainter.Paint(dataPoints, numberOfBars, canvas, paint, dimensions);
                    break;
                case AudioRecorderState.CanPlayAudio:
                case AudioRecorderState.PlayingAudio:
                    waveformPainter = new MiniWaveformPainter();
                    waveformPainter.Paint(dataPoints, dataPoints.Length, canvas, paint, dimensions);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}