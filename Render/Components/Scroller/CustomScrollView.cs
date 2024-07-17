using System.Diagnostics;

namespace Render.Components.Scroller;

public class CustomScrollView : ScrollView
{
    /// <summary>
    /// InputScrollX is a receiver bindable property to reflect 
    /// ScrollView X coordinate changing from outer clients.
    /// Update flow: Scroller -> InternalSequencer -> ScrollView.
    /// Whereas, ScrollView.ScrollX is a transmitter property to reflect 
    /// X coordinate changing from ScrollView to outer clients.
    /// </summary>
    public static readonly BindableProperty InputScrollXProperty =
        BindableProperty.Create(
            propertyName: "InputScrollX",
            returnType: typeof(double),
            declaringType: typeof(CustomScrollView),
            defaultValue: 0d,
            propertyChanged: InputScrollXChanged);

    /// <summary>
    /// Ratio of visible ScrollView content width to 
    /// the whole ScrollView content width, 
    /// including invisible part beyond the edge of the screen.
    /// Used to correct reflection calculation of visible part of the screen on mini-scroller.
    /// </summary>
    public static readonly BindableProperty WidthRatioProperty =
        BindableProperty.Create(
            propertyName: "WidthRatio",
            returnType: typeof(double),
            declaringType: typeof(CustomScrollView),
            defaultValue: 1d);

    /// <summary>
    /// Width of the whole ScrollView content, including invisible part beyond the edge of the screen.
    /// </summary>
    public static readonly BindableProperty TotalWidthProperty =
        BindableProperty.Create(
            propertyName: "TotalWidth",
            returnType: typeof(double),
            declaringType: typeof(CustomScrollView),
            defaultValue: 0d);


    public double InputScrollX
    {
        get => (double)GetValue(InputScrollXProperty);
        set => SetValue(InputScrollXProperty, value);
    }

    public double WidthRatio
    {
        get => (double)GetValue(WidthRatioProperty);
        set => SetValue(WidthRatioProperty, value);
    }

    public double TotalWidth
    {
        get => (double)GetValue(TotalWidthProperty);
        set => SetValue(TotalWidthProperty, value);
    }

    protected override void OnPropertyChanged(string propertyName = null)
    {
        base.OnPropertyChanged(propertyName);

        if (propertyName == "ContentSize")
        {            
            CalculateWidthRatio();
            TotalWidth = ContentSize.Width;
        }
        
    }
    
    private static async void InputScrollXChanged(BindableObject view, object oldValue, object newValue)
    {
        if (view is not CustomScrollView scrollView || scrollView.IsEnabled is false)
        {
            return;
        }

        var scrollX = (double)newValue;
        scrollX = scrollView.AdjustToScrollerCenter(scrollX);

        var targetScrollX = scrollX > 0 ? scrollX : 0;
        await scrollView.ScrollToAsync(targetScrollX, 0, false);
    }

    /// <summary>
    /// Adjust target scrollX coordinate for ScrollView, 
    /// to correlate ScrollView moving with the mini Scroller moving,
    /// relative to Scroller center.
    /// </summary>
    private double AdjustToScrollerCenter(double scrollX)
    {
        return scrollX - 0.5 * Width;
    }

    /// <summary>
    /// Returns width of the whole ScrollView content, 
    /// including invisible part beyond the edge of the screen
    /// </summary>
    private double GetContentSizeWidth()
    {
        return ContentSize.Width > 0 ? ContentSize.Width : Width;
    }
    
    private void CalculateWidthRatio()
    {
        var widthRatio = Width / GetContentSizeWidth();
        WidthRatio = widthRatio;
    }

    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);
        
        CalculateWidthRatio();
    }
}