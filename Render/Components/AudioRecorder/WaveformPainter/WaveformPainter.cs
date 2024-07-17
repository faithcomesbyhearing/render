using SkiaSharp;

namespace Render.Components.AudioRecorder.WaveformPainter
{
    public abstract class WaveformPainter
    {
        public abstract void Paint(float[] dataPoints, int numberOfBars, SKCanvas canvas, SKPaint paint, SKRectI dimensions);
        
        protected (float y0, float y1) GetHeightAndYValue(float height, SKRectI dimensions)
        {
            const int minHeight = 2;

            var minOffset = 0.05f * dimensions.Height;
            var maxY = dimensions.Height - minOffset;
            var minY = minOffset;
            var scaleFactorY = dimensions.Height * 2;
            var midpoint = dimensions.MidY;
            
            height *= scaleFactorY;
            var y = midpoint - height;
            
            if (height < minHeight)
            {
                height = minHeight;
                y = midpoint - minHeight;
            }
            var y1 = y + (height * 2);

            //Check for out of bounds y values and reset them
            if (y < minY)
            {
                y = minY;
            }

            if (y1 > maxY)
            {
                y1 = maxY;
            }

            return (y, y1);
        }
    }
}