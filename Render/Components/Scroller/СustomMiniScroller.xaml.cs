using ReactiveUI;

namespace Render.Components.Scroller;

public partial class CustomMiniScroller
{
    public CustomMiniScroller()
    {
        InitializeComponent();

        DisposableBindings = this.WhenActivated(d =>
        {
            d(this.OneWayBind(ViewModel, vm => vm.ScrollerTranslationX,
                v => v.HiddenAreaBefore.WidthRequest));
            d(this.OneWayBind(ViewModel, vm => vm.FrameWidth,
                v => v.VisibleArea.WidthRequest));
            d(this.OneWayBind(ViewModel, vm => vm.FrameWidth,
                v => v.Frame.WidthRequest));
            d(this.OneWayBind(ViewModel, vm => vm.TotalWidth, v => v.HiddenScroller.Maximum));
            d(this.Bind(ViewModel, vm => vm.InputScrollX, v => v.HiddenScroller.Value));
            
            HiddenScroller.DragStarted += SliderDragStarted;
            HiddenScroller.DragCompleted += SliderDragCompleted;
        });
    }
    
    private void SliderDragStarted(object sender, EventArgs e)
    {
        if (sender is not Slider)
        {
            return;
        }

        if (ViewModel != null)
        {
            ViewModel.IsSliderDragging = true;
        }

    }

    private void SliderDragCompleted(object sender, EventArgs e)
    {
        if (sender is not Slider)
        {
            return;
        }
        
        if (ViewModel != null)
        {
            ViewModel.IsSliderDragging = false;
        }
    }
}