using Render.Components.DivisionPlayer;
using SkiaSharp;
using SkiaSharp.Views.Maui;

namespace Render.Components.AudioRecorder.WaveformPainter
{
    public class MiniWaveformPainter : WaveformPainter
    {
        public override void Paint(
            float[] dataPoints,
            int numberOfBars,
            SKCanvas canvas,
            SKPaint paint,
            SKRectI dimensions)
        {
            var dataLength = dataPoints.Length;
            var numberOfPointsToPlot = Math.Min(numberOfBars, dataLength);
            var totalBarWidth = (double)dimensions.Width/numberOfBars;
            var firstPoint = dataLength - numberOfPointsToPlot;
            var x = 0;

            var info = new SKImageInfo(512, 256);
            for (var i = firstPoint; i < dataLength; i++)
            {
                var (y0, y1) = GetHeightAndYValue(dataPoints[i], dimensions);
                var x0 = (float) (x * totalBarWidth + totalBarWidth * .25);
                //call this method for debugging purpose
                //paint.Color = GetColorForJellyBeanWaveform(x);
                canvas.DrawLine(x0, y0, x0, y1, paint);
                x++;
            }
        }

        private SKColor GetColorForJellyBeanWaveform(int number)
        {
            var remainder = number % 10;
            switch (remainder)
            {
                case 0:
                    return SKColors.Aqua;
                case 1:
                    return SKColors.Purple;
                case 2:
                    return SKColors.Red;
                case 3:
                    return SKColors.Green;
                case 4:
                    return SKColors.Orange;
                case 5:
                    return SKColors.Pink;
                case 6:
                    return SKColors.Coral;
                case 7:
                    return SKColors.Beige;
                case 8:
                    return SKColors.Honeydew;
                case 9:
                    return SKColors.Brown;
            }
            return SKColors.Black;
        }
    }
}