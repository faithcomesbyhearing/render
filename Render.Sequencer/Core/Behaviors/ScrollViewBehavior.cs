using Render.Sequencer.Core.Utils.Extensions;
using System.ComponentModel;

namespace Render.Sequencer.Core.Behaviors;

public class ScrollViewBehavior : Behavior<ScrollView>
{
    public static readonly BindableProperty AttachBehaviorProperty =
        BindableProperty.CreateAttached(
            propertyName: "AttachBehavior",
            returnType: typeof(bool),
            declaringType: typeof(ScrollViewBehavior),
            defaultValue: false,
            propertyChanged: OnAttachBehaviorChanged);

    /// <summary>
    /// InputScrollX is a receiver bindable property to reflect 
    /// ScrollView X coordinate changing from outer clients.
    /// Update flow: Scroller -> InternalSequencer -> ScrollView.
    /// Whereas, ScrollView.ScrollX is a transmitter property to reflect 
    /// X coordinate changing from ScrollView to outer clients.
    /// Update flow: Scrubber -> ScrollView -> InternalSequencer -> Scroller
    /// </summary>
    public static readonly BindableProperty InputScrollXProperty =
        BindableProperty.CreateAttached(
            propertyName: "InputScrollX",
            returnType: typeof(double),
            declaringType: typeof(ScrollViewBehavior),
            defaultValue: 0d,
            propertyChanged: InputScrollXChanged);

    /// <summary>
    /// Ratio of visible ScrollView content width to 
    /// the whole ScrollView content width, 
    /// including invisible part beyond the edge of the screen.
    /// Used to correct reflection calculation of visible part of the screen on mini-scroller.
    /// </summary>
    public static readonly BindableProperty WidthRatioProperty =
        BindableProperty.CreateAttached(
            propertyName: "WidthRatio",
            returnType: typeof(double),
            declaringType: typeof(ScrollViewBehavior),
            defaultValue: 1d);

    /// <summary>
    /// Width of the whole ScrollView content, including invisible part beyond the edge of the screen.
    /// </summary>
    public static readonly BindableProperty TotalWidthProperty =
       BindableProperty.CreateAttached(
           propertyName: "TotalWidth",
           returnType: typeof(double),
           declaringType: typeof(ScrollViewBehavior),
           defaultValue: 0d);

    public static bool GetAttachBehavior(BindableObject view)
    {
        return (bool)view.GetValue(AttachBehaviorProperty);
    }

    public static void SetAttachBehavior(BindableObject view, bool value)
    {
        view.SetValue(AttachBehaviorProperty, value);
    }

    public static double GetInputScrollX(BindableObject view)
    {
        return (double)view.GetValue(InputScrollXProperty);
    }

    public static void SetInputScrollX(BindableObject view, double value)
    {
        view.SetValue(InputScrollXProperty, value);
    }

    public static double GetWidthRatio(BindableObject view)
    {
        return (double)view.GetValue(WidthRatioProperty);
    }

    public static void SetWidthRatio(BindableObject view, double value)
    {
        view.SetValue(WidthRatioProperty, value);
    }

    public static double GetTotalWidth(BindableObject view)
    {
        return (double)view.GetValue(TotalWidthProperty);
    }

    public static void SetTotalWidth(BindableObject view, double value)
    {
        view.SetValue(TotalWidthProperty, value);
    }

    private static void OnAttachBehaviorChanged(BindableObject view, object oldValue, object newValue)
    {
        if (view is not ScrollView scrollView)
        {
            return;
        }

        bool attachBehavior = (bool)newValue;
        if (attachBehavior)
        {
            scrollView.AddBehavior(new ScrollViewBehavior());
            scrollView.PropertyChanged += ScrollViewPropertyChanged;
        }
        else
        {
            scrollView.RemoveBehavior(scrollView.GetBehavior<ScrollViewBehavior>());
            scrollView.PropertyChanged -= ScrollViewPropertyChanged;
        }
    }

    private static void ScrollViewPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ScrollView.ContentSize) && sender is ScrollView scrollView)
        {
            ScrollViewContentSizeChanged(scrollView);
        }
    }

    private static void ScrollViewContentSizeChanged(ScrollView scrollView)
    {
        SetTotalWidth(scrollView, scrollView.ContentSize.Width);
        UpdateWidthRatio(scrollView);
    }

    private static async void InputScrollXChanged(BindableObject view, object oldValue, object newValue)
    {
        if (view is not ScrollView scrollView || scrollView.IsEnabled is false)
        {
            return;
        }

        var scrollX = (double)newValue;
        scrollX = AdjustToScrollerCenter(scrollView, scrollX);

        var targetScrollX = scrollX > 0 ? scrollX : 0;
        await scrollView.ScrollToAsync(targetScrollX, 0, false);
    }

    /// <summary>
    /// Adjust target scrollX coordinate for ScrollView, 
    /// to correlate ScrollView moving with the mini Scroller moving,
    /// relative to Scroller center.
    /// </summary>
    private static double AdjustToScrollerCenter(ScrollView scrollView, double scrollX)
    {
        return scrollX - 0.5 * scrollView.Width;
    }

    /// <summary>
    /// Returns width of the whole ScrollView content, 
    /// including invisible part beyond the edge of the screen
    /// </summary>
    private static double GetContentSizeWidth(ScrollView scrollView)
    {
        return scrollView.ContentSize.Width > 0 ? scrollView.ContentSize.Width : scrollView.Width;
    }

    private static void UpdateWidthRatio(ScrollView scrollView)
    {
        var widthRatio = scrollView.Width / GetContentSizeWidth(scrollView);
        SetWidthRatio(scrollView, widthRatio);
    }

    protected override void OnAttachedTo(ScrollView scrollView)
    {
        scrollView.SizeChanged += ScrollViewSizeChanged;

        base.OnAttachedTo(scrollView);
    }

    protected override void OnDetachingFrom(ScrollView scrollView)
    {
        scrollView.SizeChanged -= ScrollViewSizeChanged;

        base.OnDetachingFrom(scrollView);
    }

    private void ScrollViewSizeChanged(object? sender, EventArgs e)
    {
        if (sender is not ScrollView scrollView)
        {
            return;
        }

        UpdateWidthRatio(scrollView);
    }
}