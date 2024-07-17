using SkiaSharp;

namespace Render.Sequencer.Core.Painters
{
    /// <summary>
    /// Optimized mini wave form painter to prevent performance degradation
    /// for long audio graph drawing.
    /// See details: https://dev.azure.com/FCBH/Software%20Development/_workitems/edit/7605
    /// </summary>
    public class RecorderMiniWaveformPainter : WaveformPainter
    {
        /// <summary>
        /// Recommended value 10,000. This value adds one skip magnifier point per ~5 minutes
        /// with almost no difference from visual perspective.
        /// </summary>
        private const int MaxPointsToDraw = 10000;

        public override void Paint(float[] dataPoints, int numberOfBars, SKCanvas canvas, SKPaint paint, SKRectI dimensions)
        {
            var dataLength = dataPoints.Length;
            var skipMagnifier = dataLength / MaxPointsToDraw + 1;
            var numberOfBarsLocal = skipMagnifier > 1 ? (int)(dataLength * 1d / skipMagnifier) : dataLength;
            var totalBarWidth = (double)dimensions.Width / numberOfBarsLocal;
            var rectWidth = totalBarWidth * .5;
            paint.StrokeWidth = rectWidth < 1.4f ? 1.4f : (float)rectWidth;

            var x = 0;
            for (var i = 0; i < dataLength; i += skipMagnifier)
            {
                var (y0, y1) = GetHeightAndYValue(dataPoints[i], dimensions);
                var x0 = (float)(x * totalBarWidth + totalBarWidth * .25);

                canvas.DrawLine(x0, y0, x0, y1, paint);
                x++;
            }

            // Uncomment line bellow to see logs in debug console:
            //Debug.WriteLine($"initial length: {dataPoints.Length}, drawn length: {x}, skipRatio: {skipMagnifier - 1}");
        }
    }
}