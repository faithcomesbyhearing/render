using SkiaSharp;

namespace Render.Components.AudioRecorder.WaveformPainter
{
    public class RecordingWaveformPainter : WaveformPainter
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
            var x = Math.Min(dataLength, numberOfBars);

            for (var i = firstPoint; i < dataLength; i++)
            {
                var (y0, y1) = GetHeightAndYValue(dataPoints[i], dimensions);
                var x0 = (float) (dimensions.Width - (x + (x * totalBarWidth)));

                canvas.DrawLine(x0, y0, x0, y1, paint);
                x--;
            }
        }
    }

    public class TabletWaveformPainter : WaveformPainter
    {
        //A bunch of math is now implicitly dependent on this being 1 all the time, so if we want to change this
        //we'll have to modify every place where we determine the number of bars drawn based on the screen width
        public static double TotalBarWidth => 1d;
        
        public override void Paint(
            float[] dataPoints,
            int numberOfBars,
            SKCanvas canvas,
            SKPaint paint,
            SKRectI dimensions)
        {
            var numberOfPointsToPlot = Math.Min(numberOfBars, dataPoints.Length);
            var firstPoint = dataPoints.Length - numberOfPointsToPlot;
            var x = 0;

            for (var i = firstPoint; i < dataPoints.Length; i++)
            {
                var (y0, y1) = GetHeightAndYValue(dataPoints[i], dimensions);
                var x0 = (float) (x * TotalBarWidth + TotalBarWidth * .25);

                canvas.DrawLine(x0, y0, x0, y1, paint);
                x++;
            }
        }
    }
}