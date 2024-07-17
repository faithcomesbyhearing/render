using System.Reactive.Linq;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Render.Kernel;

namespace Render.Components.Scroller;

public class ScrollerViewModel : ViewModelBase
{
    [Reactive] public double VisibleScreenWidth { get; set; }
    [Reactive] public double InputScrollX { get; private set; }
    [Reactive] public double WidthRatio { get; set; } 
    [Reactive] public double FrameWidth { get; private set; }
    [Reactive] public double TotalWidth { get; set; }
    [Reactive] public double ScrollerTranslationX { get; private set; }
    [Reactive] public double OutputScrollX { get; private set; }
    [Reactive] public bool IsSliderDragging { get; set; }

    public ScrollerViewModel(IViewModelContextProvider viewModelContextProvider)
        : base("ScrollerViewModel", viewModelContextProvider)
    {
        WidthRatio = 1;
        
        Disposables.Add(
            this.WhenAnyValue(
                    x => x.InputScrollX,
                    x => x.WidthRatio,
                    x => x.TotalWidth)
            .Where(x => x.Item2 > 0 || x.Item3 > 0)
            .Subscribe(((double InputScrollX, double WidthRatio, double TolalWidth) options) =>
                    UpdateScrollerGeometry(options.InputScrollX, options.WidthRatio, options.TolalWidth)));
        
        Disposables.Add(
            this.WhenAnyValue(x => x.OutputScrollX, x => x.WidthRatio)
                .Where(_ => IsSliderDragging is not true)
                .Subscribe(((double OutputScrollX, double WidthRatio) e) => InputScrollX = e.OutputScrollX + 0.5 * VisibleScreenWidth));
    }

    private void UpdateScrollerGeometry(
        double inputScrollX,
        double screenRatio,
        double totalWidth)
    {
        FrameWidth = Math.Round(VisibleScreenWidth * screenRatio);

        var widthRatio = VisibleScreenWidth / totalWidth;
        
        var scrollerTranslationX = inputScrollX * widthRatio - 0.5 * FrameWidth;
        
        var maxScrollerEndX = VisibleScreenWidth - FrameWidth;

        if (scrollerTranslationX > maxScrollerEndX)
        {
            scrollerTranslationX = maxScrollerEndX;
        }

        ScrollerTranslationX = scrollerTranslationX > 0 ? scrollerTranslationX : 0;
    }

    public void ScrollTo(double position)
    {
        InputScrollX = position;
    }
}