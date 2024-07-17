using Render.Components.DivisionPlayer;
using SkiaSharp;
using SkiaSharp.Views.Maui;

namespace Render.Components.AudioRecorder.WaveformPainter
{
    public class PlayerWaveformPainter : WaveformPainter
    {
        private readonly double _position;
        private readonly double _duration;

        public PlayerWaveformPainter(double position, double duration)
        {
            _position = position;
            _duration = duration;
        }
        
        public override void Paint(
            float[] dataPoints,
            int numberOfBars,
            SKCanvas canvas,
            SKPaint paint,
            SKRectI dimensions)
        {
            var dataLength = dataPoints.Length;
            var totalBarWidth = (double)dimensions.Width/numberOfBars;
            var ratio = _position / _duration;

            //draw starting at the middle of the screen
            //the current time will be at the midX of the screen
            var centerBarIndex = ratio * dataLength;
            var halfNumberOfBars = numberOfBars/2;
                    
            var firstPoint = (int) Math.Max(0, centerBarIndex - halfNumberOfBars);
            var limit = (int) Math.Min(dataLength, centerBarIndex + halfNumberOfBars);
            var xOffset = (int)(centerBarIndex + halfNumberOfBars) - firstPoint;

            int x = 0;
            if (dataLength <= halfNumberOfBars)
            {
                x = halfNumberOfBars - (int)centerBarIndex;
            }
            else if (centerBarIndex <= halfNumberOfBars)
            {
                x = numberOfBars - xOffset;
            }
            else
            {
                x = 0;
            }

            for (var i = firstPoint; i < limit; i++)
            {
                var (y0, y1) = GetHeightAndYValue(dataPoints[i], dimensions);
                var x0 = (float) (x * totalBarWidth);

                canvas.DrawLine(x0, y0, x0, y1, paint);
                x++;
            }
        }
    }
    
    public class TabletPlayerWaveformPainter : WaveformPainter
    {
        private double _totalBarWidth;

        public TabletPlayerWaveformPainter(double totalBarWidth)
        {
            _totalBarWidth = totalBarWidth;
        }
        
        public override void Paint(
            float[] dataPoints,
            int numberOfBars,
            SKCanvas canvas,
            SKPaint paint,
            SKRectI dimensions)
        {
            var dataLength = dataPoints.Length;
            var firstPoint = 0;
            var limit = dataLength;
            var x = 0;

            for (var i = firstPoint; i < limit; i++)
            {
                var (y0, y1) = GetHeightAndYValue(dataPoints[i], dimensions);
                var x0 = (float) (x * _totalBarWidth);

                canvas.DrawLine(x0, y0, x0, y1, paint);
                x++;
            }
        }
    }
}