using SkiaSharp;

namespace Render.Sequencer.Core.Painters;

public class MiniWaveformPainter : BaseWaveformPainter
{
    public override void Paint(float[] dataPoints, int numberOfBars, SKCanvas canvas, SKPaint paint, SKRectI dimensions)
    {
        var dataLength = dataPoints.Length;
        var numberOfPointsToPlot = Math.Min(numberOfBars, dataLength);
        var totalBarWidth = (double)dimensions.Width / numberOfBars;
        var firstPoint = dataLength - numberOfPointsToPlot;
        var x = 0;

        for (var i = firstPoint; i < dataLength; i++)
        {
            var (y0, y1) = GetHeightAndYValue(dataPoints[i], dimensions);
            var x0 = (float)(x * totalBarWidth + totalBarWidth * .25);

            //call this method for debugging purpose
            //paint.Color = GetColorForJellyBeanWaveform(x);
            canvas.DrawLine(x0, y0, x0, y1, paint);
            x++;
        }
    }
}