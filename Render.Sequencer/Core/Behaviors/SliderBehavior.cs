using Render.Sequencer.Core.Utils.Extensions;

namespace Render.Sequencer.Core.Behaviors;

public class SliderBehavior : Behavior<Slider>
{
    public static readonly BindableProperty AttachBehaviorProperty =
        BindableProperty.CreateAttached(
            propertyName: "AttachBehavior",
            returnType: typeof(bool),
            declaringType: typeof(SliderBehavior),
            defaultValue: false,
            propertyChanged: OnAttachBehaviorChanged);

    /// <summary>
    /// ScrollView for reflecting scrolling from Slider values
    /// </summary>
    public static readonly BindableProperty AttachedScrollViewProperty =
        BindableProperty.CreateAttached(
            propertyName: "AttachedScrollView",
            returnType: typeof(ScrollView),
            declaringType: typeof(SliderBehavior),
            defaultValue: null);

    /// <summary>
    /// Margin from the edge of the screen when scrolling appears
    /// </summary>
    public static readonly BindableProperty ScrollMarginProperty =
        BindableProperty.CreateAttached(
            propertyName: "ScrollMargin",
            returnType: typeof(double),
            declaringType: typeof(SliderBehavior),
            defaultValue: 50d);

    /// <summary>
    /// True, when user move (drag or tap) Scrubber, otherwise is false
    /// </summary>
    public static readonly BindableProperty DraggingProperty =
        BindableProperty.CreateAttached(
            propertyName: "Dragging",
            returnType: typeof(bool),
            declaringType: typeof(SliderBehavior),
            defaultValue: false,
            defaultBindingMode: BindingMode.OneWayToSource);

    public static ScrollView GetAttachedScrollView(BindableObject view)
    {
        return (ScrollView)view.GetValue(AttachedScrollViewProperty);
    }

    public static void SetAttachedScrollView(BindableObject view, ScrollView value)
    {
        view.SetValue(AttachedScrollViewProperty, value);
    }

    public static bool GetAttachBehavior(BindableObject view)
    {
        return (bool)view.GetValue(AttachBehaviorProperty);
    }

    public static void SetAttachBehavior(BindableObject view, bool value)
    {
        view.SetValue(AttachBehaviorProperty, value);
    }

    public static double GetScrollMargin(BindableObject view)
    {
        return (double)view.GetValue(ScrollMarginProperty);
    }

    public static void SetScrollMargin(BindableObject view, double value)
    {
        view.SetValue(AttachBehaviorProperty, value);
    }

    public static bool GetDragging(BindableObject view)
    {
        return (bool)view.GetValue(DraggingProperty);
    }

    public static void SetDragging(BindableObject view, bool value)
    {
        view.SetValue(DraggingProperty, value);
    }

    private static void OnAttachBehaviorChanged(BindableObject view, object oldValue, object newValue)
    {
        if (view is not Slider slider)
        {
            return;
        }

        bool attachBehavior = (bool)newValue;
        if (attachBehavior)
        {
            slider.AddBehavior(new SliderBehavior());
        }
        else
        {
            slider.RemoveBehavior(slider.GetBehavior<SliderBehavior>());
        }
    }

    protected override void OnAttachedTo(Slider slider)
    {
        slider.ValueChanged += SliderValueChanged;
        slider.DragStarted += SliderDragStarted;
        slider.DragCompleted += SliderDragCompleted;

        base.OnAttachedTo(slider);
    }

    protected override void OnDetachingFrom(Slider slider)
    {
        slider.ValueChanged -= SliderValueChanged;
        slider.DragStarted -= SliderDragStarted;
        slider.DragCompleted -= SliderDragCompleted;

        base.OnDetachingFrom(slider);
    }

    private async void SliderValueChanged(object? sender, ValueChangedEventArgs e)
    {
        var slider = sender as Slider;
        if (slider == null)
        {
            return;
        }

        var scrollView = GetAttachedScrollView(slider);
        if (scrollView is null)
        {
            return;
        }

        var durationSec = slider.Maximum;
        var positionSec = slider.Value;
        var widthPerSecond = scrollView.ContentSize.Width / durationSec;
        var position = positionSec * widthPerSecond;
        var isRightScroll = e.NewValue > e.OldValue && position > scrollView.ScrollX;
        var scrollMargin = GetScrollMargin(slider);

        if (isRightScroll)
        {
            await ScrollRight(scrollView, rightScrubberPosition: position + scrollMargin);
        }
        else
        {
            await ScrollLeft(scrollView, leftScrubberPosition: position - scrollMargin);
        }
    }

    private static Task ScrollRight(ScrollView scrollView, double rightScrubberPosition)
    {
        var width = scrollView.Width;
        if (rightScrubberPosition > width)
        {
            var scrollToX = rightScrubberPosition - width;
            if (scrollToX < scrollView.ScrollX)
            {
                return Task.CompletedTask;
            }

            return scrollView.ScrollToAsync(scrollToX, 0, true);
        }

        return Task.CompletedTask;
    }

    private static Task ScrollLeft(ScrollView scrollView, double leftScrubberPosition)
    {
        if (leftScrubberPosition < scrollView.ScrollX)
        {
            var scrollToX = leftScrubberPosition;
            return scrollView.ScrollToAsync(scrollToX, 0, true);
        }

        return Task.CompletedTask;
    }

    private static void SliderDragStarted(object? sender, EventArgs e)
    {
        if (sender is not Slider slider)
        {
            return;
        }

        SetDragging(slider, true);
    }

    private static void SliderDragCompleted(object? sender, EventArgs e)
    {
        if (sender is not Slider slider)
        {
            return;
        }

        SetDragging(slider, false);
    }
}