namespace Render.Sequencer.Core.Controls;

public class Scrubber : Slider
{
    public static readonly BindableProperty ScrubberColorProperty =
        BindableProperty.Create(
            propertyName: nameof(ScrubberColor),
            returnType: typeof(Color),
            declaringType: typeof(Scrubber),
            defaultValue: Colors.Black);

    public static readonly BindableProperty ScrubberThicknessProperty =
        BindableProperty.Create(
            propertyName: nameof(ScrubberThickness),
            returnType: typeof(double),
            declaringType: typeof(Scrubber),
            defaultValue: 6d);

    /// <summary>
    /// Visual width of the scrubber thumb.
    /// Used to adjust visual margins of scrubber thumb from the edges
    /// </summary>
    public static readonly BindableProperty ThumbVisualWidthProperty =
        BindableProperty.Create(
            propertyName: nameof(ThumbVisualWidth),
            returnType: typeof(double),
            declaringType: typeof(Scrubber),
            defaultValue: 8d);

    /// <summary>
    /// Scrubber intercepts all touch gestures and doesn't propagate them to underlying views (by Z axis).
    /// Another complexity occurs when target views are not in the same visual tree hierarchy,
    /// but siblings with Scrubber in the common parent. In this scenario, we can't use event bubbling.
    /// Need to set common parent container to find target sibling view by TargetViewTag on the platform level code.
    /// See ScrubberHandler for detailed implementation.
    /// </summary>
    public static readonly BindableProperty ViewSearchContainerProperty =
        BindableProperty.Create(
            propertyName: nameof(ViewSearchContainer),
            returnType: typeof(View),
            declaringType: typeof(Scrubber),
            defaultValue: null);

    /// <summary>
    /// Tag for target views for gesture recognition. 
    /// Need to distinguish view from other underlying views by Z axis.
    /// </summary>
    public static readonly BindableProperty TargetViewTagProperty =
        BindableProperty.Create(
            propertyName: nameof(TargetViewTag),
            returnType: typeof(string),
            declaringType: typeof(Scrubber),
            defaultValue: null);

    public static readonly BindableProperty PaddingProperty = 
        BindableProperty.Create(
            propertyName: nameof(Padding),
            returnType: typeof(Thickness),
            declaringType: typeof(Scrubber),
            defaultValue: default(Thickness));

    public Color ScrubberColor
    {
        get => (Color)GetValue(ScrubberColorProperty);
        set => SetValue(ScrubberColorProperty, value);
    }

    public double ScrubberThickness
    {
        get => (double)GetValue(ScrubberThicknessProperty);
        set => SetValue(ScrubberThicknessProperty, value);
    }

    public double ThumbVisualWidth
    {
        get => (double)GetValue(ThumbVisualWidthProperty);
        set => SetValue(ThumbVisualWidthProperty, value);
    }

    public View? ViewSearchContainer
    {
        get => (View)GetValue(ViewSearchContainerProperty);
        set => SetValue(ViewSearchContainerProperty, value);
    }

    public string TargetViewTag
    {
        get => (string)GetValue(TargetViewTagProperty);
        set => SetValue(TargetViewTagProperty, value);
    }

    public Thickness Padding
    {
        get => (Thickness)GetValue(PaddingProperty);
        set => SetValue(PaddingProperty, value);
    }

    public Scrubber()
    {
        MaximumTrackColor = MinimumTrackColor = Colors.Transparent;
    }
}