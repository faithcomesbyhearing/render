using ReactiveUI;
using Render.Components.AudioRecorder.WaveformPainter;
using Render.Resources;
using SkiaSharp;
using SkiaSharp.Views.Maui;

namespace Render.Components.MiniWaveformPlayer
{
    public partial class MiniWaveformPlayer
    {
        public MiniWaveformPlayer()
        {
            InitializeComponent();

            MiniPassageDivideCanvas.IsVisible = false;

            this.WhenActivated(d =>
            {
                d(this
                    .WhenAnyValue(x => x.ViewModel.PassagePainter.Duration)
                    .Subscribe(n => { MiniWaveformCanvas.InvalidateSurface(); }));

                d(this
                    .WhenAnyValue(x => x.ViewModel.PassagePainter.Markers)
                    .Subscribe(l => { MiniWaveformCanvas.InvalidateSurface(); }));

                d(this
                    .WhenAnyValue(x => x.ViewModel.ActionState)
                    .Subscribe(a => { MiniWaveformCanvas.InvalidateSurface(); }));

                d(this
                    .WhenAnyValue(x => x.ViewModel.AudioSamples)
                    .Subscribe(x => MiniWaveformCanvas.InvalidateSurface()));
            });
        }

        private void OnPaintSurface(object sender, SKPaintSurfaceEventArgs e)
        {
            if (ViewModel == null)
            {
                return;
            }

            var miniWaveformPlayerViewModel = (IMiniWaveformPlayerViewModel)ViewModel;
            var dataPoints = miniWaveformPlayerViewModel.AudioSamples;

            if (dataPoints == null)
            {
                return;
            }

            var canvas = e.Surface.Canvas;
            var dimensions = canvas.DeviceClipBounds;

            var numberOfBars = 100;
            var totalBarWidth = (double)dimensions.Width / numberOfBars;
            var rectWidth = totalBarWidth * .5;

            var color = ResourceExtensions.GetColor("SelectedPassageHighlight");
            color = color.MultiplyAlpha(0.5f);

            var paint = new SKPaint
            {
                Color = color.ToSKColor(),
                IsAntialias = true,
                StrokeCap = SKStrokeCap.Round,
                StrokeWidth = (float)rectWidth
            };
            canvas.Clear();

            WaveformPainter waveformPainter = new MiniWaveformPainter();
            waveformPainter.Paint(dataPoints, numberOfBars, canvas, paint, dimensions);
        }
    }
}