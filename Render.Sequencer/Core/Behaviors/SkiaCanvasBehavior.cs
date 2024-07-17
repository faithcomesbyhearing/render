using Render.Sequencer.Core.Painters;
using SkiaSharp.Views.Maui;
using SkiaSharp;
using SkiaSharp.Views.Maui.Controls;
using System.Windows.Input;
using Render.Sequencer.Core.Utils.Extensions;

namespace Render.Sequencer.Core.Behaviors;

public class SkiaCanvasBehavior : Behavior<SKCanvasView>
{
    public static readonly BindableProperty AttachBehaviorProperty =
        BindableProperty.CreateAttached(
            propertyName: "AttachBehavior",
            returnType: typeof(bool),
            declaringType: typeof(SkiaCanvasBehavior),
            defaultValue: false,
            propertyChanged: OnAttachBehaviorChanged);

    public static readonly BindableProperty SamplesProperty =
        BindableProperty.CreateAttached(
            propertyName: "Samples",
            returnType: typeof(float[]),
            declaringType: typeof(SkiaCanvasBehavior),
            defaultValue: new float[0],
            propertyChanged: OnSamplesChanged);

    public static readonly BindableProperty ParentProperty =
        BindableProperty.CreateAttached(
            propertyName: "Parent",
            returnType: typeof(View),
            declaringType: typeof(SkiaCanvasBehavior),
            defaultValue: null,
            propertyChanged: OnParentChanged);

    public static readonly BindableProperty SizeChangedCommandProperty =
        BindableProperty.CreateAttached(
            propertyName: "SizeChangedCommand",
            returnType: typeof(ICommand),
            declaringType: typeof(SkiaCanvasBehavior),
            defaultValue: null);

    public static readonly BindableProperty WaveFormColorProperty =
        BindableProperty.CreateAttached(
            propertyName: "WaveFormColor",
            returnType: typeof(Color),
            declaringType: typeof(SkiaCanvasBehavior),
            defaultValue: Color.FromArgb("#000000"),
            propertyChanged: OnWaveFormColorChanged);

    public static readonly BindableProperty PainterProperty =
        BindableProperty.CreateAttached(
            propertyName: "Painter",
            returnType: typeof(BaseWaveformPainter),
            declaringType: typeof(SkiaCanvasBehavior),
            defaultValue: new WaveformPainter(),
            propertyChanged: OnWaveFormColorChanged);

    public static bool GetAttachBehavior(BindableObject view)
    {
        return (bool)view.GetValue(AttachBehaviorProperty);
    }

    public static void SetAttachBehavior(BindableObject view, bool value)
    {
        view.SetValue(AttachBehaviorProperty, value);
    }

    public static float[] GetSamples(BindableObject view)
    {
        return (float[])view.GetValue(SamplesProperty);
    }

    public static void SetSamples(BindableObject view, float[] value)
    {
        view.SetValue(SamplesProperty, value);
    }

    public static View GetParent(BindableObject view)
    {
        return (View)view.GetValue(ParentProperty);
    }

    public static void SetParent(BindableObject view, View value)
    {
        view.SetValue(ParentProperty, value);
    }

    public static ICommand GetSizeChangedCommand(BindableObject view)
    {
        return (ICommand)view.GetValue(SizeChangedCommandProperty);
    }

    public static void SetSizeChangedCommand(BindableObject view, ICommand value)
    {
        view.SetValue(SizeChangedCommandProperty, value);
    }

    public static Color GetWaveFormColor(BindableObject view)
    {
        return (Color)view.GetValue(WaveFormColorProperty);
    }

    public static void SetWaveFormColor(BindableObject view, Color value)
    {
        view.SetValue(WaveFormColorProperty, value);
    }

    public static BaseWaveformPainter GetPainter(BindableObject view)
    {
        return (BaseWaveformPainter)view.GetValue(PainterProperty);
    }

    public static void SetPainter(BindableObject view, BaseWaveformPainter value)
    {
        view.SetValue(PainterProperty, value);
    }

    private static void OnAttachBehaviorChanged(BindableObject view, object oldValue, object newValue)
    {
        if (view is not SKCanvasView canvas)
        {
            return;
        }

        bool attachBehavior = (bool)newValue;
        if (attachBehavior)
        {
            canvas.AddBehavior(new SkiaCanvasBehavior());
        }
        else
        {
            canvas.RemoveBehavior(canvas.GetBehavior<SkiaCanvasBehavior>());
        }
    }

    private static void OnSamplesChanged(BindableObject view, object oldValue, object newValue)
    {
        if (view is not SKCanvasView canvas)
        {
            return;
        }

        canvas.InvalidateSurface();
    }

    private static void OnParentChanged(BindableObject view, object oldValue, object newValue)
    {
        if (view is not SKCanvasView canvas)
        {
            return;
        }

        var canvasBehavior = canvas.GetBehavior<SkiaCanvasBehavior>();
        if (canvasBehavior is null)
        {
            return;
        }

        if (newValue is not View parent)
        {
            if (oldValue is View parentView)
            {
                parentView.SizeChanged -= canvasBehavior.ParentSizeChanged;
            }

            return;
        }

        parent.SizeChanged += canvasBehavior.ParentSizeChanged;
    }

    private static void OnWaveFormColorChanged(BindableObject view, object oldValue, object newValue)
    {
        if (view is not SKCanvasView canvas)
        {
            return;
        }

        canvas.InvalidateSurface();
    }

    private static void CanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        if (sender is not SKCanvasView canvasView)
        {
            return;
        }

        var dataPoints = GetSamples(canvasView);
        if (dataPoints is null)
        {
            return;
        }

        var canvasBehavior = canvasView.GetBehavior<SkiaCanvasBehavior>();
        if (canvasBehavior is null)
        {
            return;
        }

        var canvasPaint = canvasBehavior._canvasPaint;
        canvasPaint.Color = GetWaveFormColor(canvasView).ToSKColor();

        var canvas = e.Surface.Canvas;
        canvas.Clear();

        var waveformPainter = GetPainter(canvasView);
        waveformPainter.Paint(
            dataPoints: dataPoints,
            numberOfBars: dataPoints.Length,
            canvas: canvas,
            paint: canvasPaint,
            dimensions: canvas.DeviceClipBounds);
    }

    private SKPaint _canvasPaint;
    private SKCanvasView? _canvas;

    public SkiaCanvasBehavior()
    {
        _canvasPaint = new SKPaint
        {
            IsAntialias = true,
            StrokeCap = SKStrokeCap.Square,
            StrokeWidth = (float)1.4
        };
    }

    protected override void OnAttachedTo(SKCanvasView canvas)
    {
        _canvas = canvas;
        _canvas.PaintSurface += CanvasPaintSurface;

        base.OnAttachedTo(canvas);
    }

    protected override void OnDetachingFrom(SKCanvasView canvas)
    {
        if (_canvas is not null)
        {
            _canvas.PaintSurface -= CanvasPaintSurface;
        }
        _canvas = null;

        base.OnDetachingFrom(canvas);
    }

    private void ParentSizeChanged(object? sender, EventArgs e)
    {
        if (_canvas is not null)
        {
            GetSizeChangedCommand(_canvas)?.Execute(null);
        }
    }
}