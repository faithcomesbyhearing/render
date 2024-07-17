using Render.Sequencer.Core.Utils.Extensions;
using SkiaSharp;

namespace Render.Sequencer.Core.Painters;

/// <summary>
/// This painter draws wave form with interpolation if canvas is smaller than maximum.
/// Interpolation applies typically when application is not in the full screen mod. 
/// It allows us optimize the resizing process
/// without rebuilding samples for new wave form size.
/// </summary>
public class InterpolatingWaveformPainter : BaseWaveformPainter
{
    //A bunch of math is now implicitly dependent on this being 1 all the time, so if we want to change this
    //we'll have to modify every place where we determine the number of bars drawn based on the screen width
    public static double TotalBarWidth => 1d;

    public double MaxWaveFormWidth { get; set; }

    public InterpolatingWaveformPainter(double maxWaveFormWidth)
    {
        MaxWaveFormWidth = maxWaveFormWidth;
    }

    public override void Paint(float[] dataPoints, int numberOfBars, SKCanvas canvas, SKPaint paint, SKRectI dimensions)
    {
        if (dataPoints.IsEmpty())
        {
            return;
        }

        if (dimensions.Width < MaxWaveFormWidth)
        {
            DrawInterpolated(dataPoints, canvas, paint, dimensions);
        }
        else
        {
            DrawNatural(dataPoints, canvas, paint, dimensions);
        }
    }

    private void DrawNatural(float[] dataPoints, SKCanvas canvas, SKPaint paint, SKRectI dimensions)
    {
        var x = 0;
        for (var i = 0; i < dataPoints.Length; i++)
        {
            var (y0, y1) = GetHeightAndYValue(dataPoints[i], dimensions);
            var x0 = (float)(x * TotalBarWidth);

            canvas.DrawLine(x0, y0, x0, y1, paint);
            x++;
        }
    }

    private void DrawInterpolated(float[] dataPoints, SKCanvas canvas, SKPaint paint, SKRectI dimensions)
    {
        var currentWidth = dimensions.Width;
        var pointsToPlot = currentWidth;

        if (dataPoints.Length != currentWidth)
        {
            var pointsRatio = currentWidth / MaxWaveFormWidth;
            pointsToPlot = (int)Math.Round(dataPoints.Length * pointsRatio);
        }

        var x = 0;
        const double rangeFactor = 0.0012;
        var dataLength = dataPoints.Length;
        var lastIndex = dataLength - 1;
        var stepSize = (double)(lastIndex) / (pointsToPlot - 1);

        for (var i = 0; i < pointsToPlot; i++)
        {
            int index = (int)Math.Round(i * stepSize);
            index = index > lastIndex ? lastIndex : index;

            int rangeStart = Math.Max(0, (int)(index - rangeFactor / 2 * dataLength));
            int rangeEnd = Math.Min(lastIndex, (int)(index + rangeFactor / 2 * dataLength));

            float maxInterpolatedValue = dataPoints[rangeStart];
            for (int j = rangeStart + 1; j <= rangeEnd; j++)
            {
                if (dataPoints[j] > maxInterpolatedValue)
                {
                    maxInterpolatedValue = dataPoints[j];
                }
            }

            var (y0, y1) = GetHeightAndYValue(maxInterpolatedValue, dimensions);
            var x0 = (float)(x * TotalBarWidth);

            canvas.DrawLine(x0, y0, x0, y1, paint);
            x++;
        }
    }
}