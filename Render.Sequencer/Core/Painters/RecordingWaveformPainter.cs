using SkiaSharp;

namespace Render.Sequencer.Core.Painters
{
    public class RecordingWaveformPainter : WaveformPainter
    {
        public override void Paint(float[] dataPoints, int numberOfBars, SKCanvas canvas, SKPaint paint, SKRectI dimensions)
        {
            var dataLength = dataPoints.Length;
            var numberOfPointsToPlot = Math.Min(numberOfBars, dataLength);
            var totalBarWidth = (double)dimensions.Width / numberOfBars;
            var firstPoint = dataLength - numberOfPointsToPlot;
            var x = Math.Min(dataLength, numberOfBars);

            for (var i = firstPoint; i < dataLength; i++)
            {
                var (y0, y1) = GetHeightAndYValue(dataPoints[i], dimensions);
                var x0 = (float)(dimensions.Width - (x + x * totalBarWidth));

                canvas.DrawLine(x0, y0, x0, y1, paint);
                x--;
            }
        }
    }
}