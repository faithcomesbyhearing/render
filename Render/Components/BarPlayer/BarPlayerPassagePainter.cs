using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Render.Models.Sections;
using Render.Resources;
using SkiaSharp;
using SkiaSharp.Views.Maui;

namespace Render.Components.BarPlayer
{
    public class BarPlayerPassagePainter : ReactiveObject
    {
        [Reactive]
        public List<TimeMarkers> Markers { get; private set; }

        public double Duration { get; private set; }

        public BarPlayerPassagePainter(List<TimeMarkers> markers)
        {
            Markers = markers;
        }

        public void UpdateMarkers(List<TimeMarkers> markers)
        {
            Markers = markers;
        }

        public void UpdateDuration(double duration)
        {
            Duration = duration;
        }

        public void Paint(SKCanvas canvas, SKRectI dimensions, double heightRequest)
        {
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if(Markers == null || Markers.Count == 0 || Duration == default) return;

            var paint = new SKPaint
            {
                StrokeWidth = 2,
                Color = ResourceExtensions.GetColor("AudioPlayerPassageDivideBackground").ToSKColor(),
                PathEffect = null,
                IsAntialias = true,
                StrokeCap = SKStrokeCap.Square
            };
            
            foreach (var marker in Markers)
            {
                var inSeconds = marker.StartMarkerTime * .001;
                var ratio = dimensions.Width / Duration;
                var xPosition = ratio * inSeconds;

                if (xPosition == 0) // skip first marker at 00:00
                {
                    continue;
                }
                canvas.DrawLine((float)xPosition, 0, (float)xPosition, (float)heightRequest, paint);
            }
        }
    }
}