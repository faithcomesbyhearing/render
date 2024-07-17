using WinRT;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Input;
using Render.Sequencer.Core.Controls;
using Render.Sequencer.Platforms.Windows.Models;
using Border = Microsoft.UI.Xaml.Controls.Border;
using Grid = Microsoft.UI.Xaml.Controls.Grid;
using Slider = Microsoft.UI.Xaml.Controls.Slider;
using GridLength = Microsoft.UI.Xaml.GridLength;
using GridUnitType = Microsoft.UI.Xaml.GridUnitType;

namespace Render.Sequencer.Platforms;

public class ScrubberHandler : SliderHandler
{
    static ScrubberHandler()
    {
        Mapper.AppendToMapping(nameof(Scrubber.ScrubberColor), ScrubberColorChanged);
        Mapper.AppendToMapping(nameof(Scrubber.ScrubberThickness), ScrubberThicknessChanged);
        Mapper.AppendToMapping(nameof(Scrubber.ThumbVisualWidth), ScrubberThumbVisualWidthChanged);
        Mapper.AppendToMapping(nameof(Scrubber.Padding), ScrubberPaddingChanged);
    }

    private static void ScrubberThumbVisualWidthChanged(ISliderHandler handler, ISlider slider)
    {
        if (slider is Scrubber scrubber && handler is ScrubberHandler scrubberHandler)
        {
            scrubberHandler.UpdateSliderThumbWidth(scrubber);
        }
    }

    private static void ScrubberThicknessChanged(ISliderHandler handler, ISlider slider)
    {
        if (slider is Scrubber scrubber && handler is ScrubberHandler scrubberHandler)
        {
            scrubberHandler.UpdateThumbLayoutWidth(scrubber);
        }
    }

    private static void ScrubberColorChanged(ISliderHandler handler, ISlider slider)
    {
        if (slider is Scrubber scrubber && handler is ScrubberHandler scrubberHandler)
        {
            scrubberHandler.UpdateThumbLayoutColor(scrubber);
        }
    }

    private static void ScrubberPaddingChanged(ISliderHandler handler, ISlider slider)
    {
        if (slider is Scrubber scrubber && handler is ScrubberHandler scrubberHandler)
        {
            scrubberHandler.UpdateSliderGridPadding(scrubber);
        }
    }

    private Grid? _sliderGrid;
    private Thumb? _sliderThumb;
    private Border? _thumbLayout;
    private Ellipse? _sliderInnerThumb;

    private PointerEventHandler _pointerEnteredHandler;
    private PointerEventHandler _pointerReleasedHandler;

    public ScrubberHandler()
    {
        _pointerEnteredHandler = new PointerEventHandler(OnPointerEntered);
        _pointerReleasedHandler = new PointerEventHandler(OnPointerReleased);
    }

    protected override void ConnectHandler(Slider platformView)
    {
        platformView.Loaded += SliderLoaded;

        platformView.AddHandler(UIElement.PointerPressedEvent, _pointerEnteredHandler, true);
        platformView.AddHandler(UIElement.PointerReleasedEvent, _pointerReleasedHandler, true);
        platformView.AddHandler(UIElement.PointerCanceledEvent, _pointerReleasedHandler, true);
    }

    protected override void DisconnectHandler(Slider platformView)
    {
        platformView.Loaded -= SliderLoaded;
        platformView.ValueChanged -= OnPlatformValueChanged;

        platformView.RemoveHandler(UIElement.PointerPressedEvent, _pointerEnteredHandler);
        platformView.RemoveHandler(UIElement.PointerReleasedEvent, _pointerReleasedHandler);
        platformView.RemoveHandler(UIElement.PointerCanceledEvent, _pointerReleasedHandler);

        _pointerEnteredHandler = null!;
        _pointerReleasedHandler = null!;
    }

    private void SliderLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is not Slider slider)
        {
            return;
        }

        slider.ValueChanged += OnPlatformValueChanged;
        slider.ApplyTemplate();

        var scrubber = (Scrubber)VirtualView;
        var sliderRoot = (FrameworkElement)VisualTreeHelper.GetChild(PlatformView, 0);

        _sliderGrid = (Grid)sliderRoot.FindName("HorizontalTemplate");
        _sliderThumb = (Thumb)sliderRoot.FindName("HorizontalThumb");
        _sliderThumb.ApplyTemplate();

        _thumbLayout = (Border)VisualTreeHelper.GetChild(_sliderThumb!, 0);
        _sliderInnerThumb = (Ellipse)_thumbLayout.FindName("SliderInnerThumb");

        StretchThumbLayout();
        StretchThumb();
        DrawScrubber();

        UpdateSliderGridPadding(scrubber);
        UpdateThumbLayoutColor(scrubber);
        UpdateThumbLayoutWidth(scrubber);
        UpdateSliderThumbWidth(scrubber);
    }

    private void OnPointerEntered(object? sender, PointerRoutedEventArgs e)
    {
        if (VirtualView is not Scrubber scrubber)
        {
            return;
        }

        VirtualView?.DragStarted();
    }

    private void OnPointerReleased(object sender, PointerRoutedEventArgs e)
    {
        VirtualView?.DragCompleted();

        if (VirtualView is not Scrubber scrubber)
        {
            return;
        }
        
        if (scrubber.ViewSearchContainer?.Handler?.PlatformView is not UIElement nativeContainer)
        {
            return;
        }

        TryInvokeTargetTouchElement(
            scrubber: scrubber,
            point: e.GetCurrentPoint(null),
            subtree: nativeContainer);
    }

    private void OnPlatformValueChanged(object? sender, RangeBaseValueChangedEventArgs e)
    {
        if (VirtualView is null)
        {
            return;
        }

        VirtualView.Value = e.NewValue;
    }

    private void TryInvokeTargetTouchElement(Scrubber scrubber, PointerPoint point, UIElement subtree)
    {
        var targetElement = VisualTreeHelper
            .FindElementsInHostCoordinates(point.Position, subtree)
            .Where(view => view is FrameworkElement)
            .Cast<FrameworkElement>()
            .FirstOrDefault(element => element.Tag is ViewTag viewTag && viewTag.Tag == scrubber.TargetViewTag);

        targetElement?
            .Tag.As<ViewTag>()
            .Callback.Invoke();
    }

    private void StretchThumbLayout()
    {
        if (_sliderGrid is not null && _sliderGrid.RowDefinitions.Count > 2)
        {
            _sliderGrid.RowDefinitions[1].Height = new GridLength(1, GridUnitType.Star);
        }
    }

    private void StretchThumb()
    {
        if (_sliderThumb is not null)
        {
            _sliderThumb.Height = double.NaN;
        }
    }

    private void DrawScrubber()
    {
        if (_thumbLayout is not null && _sliderInnerThumb is not null)
        {
            _sliderInnerThumb.Width = 0;
            _sliderInnerThumb.Height = 0;

            _thumbLayout.BorderBrush = Colors.White.ToPlatform();
            _thumbLayout.BorderThickness = new Microsoft.UI.Xaml.Thickness(1);
            _thumbLayout.CornerRadius = new Microsoft.UI.Xaml.CornerRadius(1);
        }
    }

    private void UpdateSliderGridPadding(Scrubber scrubber)
    {
        if (_sliderGrid is not null)
        {
            _sliderGrid.Padding = scrubber.Padding.ToPlatform();
        }
    }

    private void UpdateThumbLayoutColor(Scrubber scrubber)
    {
        if (_thumbLayout is not null)
        {
            _thumbLayout.Background = scrubber.ScrubberColor.ToPlatform();
        }
    }

    private void UpdateThumbLayoutWidth(Scrubber scrubber)
    {
        if (_thumbLayout is not null)
        {
            _thumbLayout.Width = scrubber.ScrubberThickness;
        }
    }

    private void UpdateSliderThumbWidth(Scrubber scrubber)
    {
        if (_sliderThumb is not null)
        {
            _sliderThumb.Width = scrubber.ThumbVisualWidth;
        }
    }
}