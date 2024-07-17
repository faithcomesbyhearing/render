using ReactiveUI;
using Render.Resources;

namespace Render.Components.DivisionPlayer;

public partial class BreathPause
{
    private const double MillisecondsPerSeconds = 1000d;
    
    private readonly Color _selectedColor;
    private readonly Color _unselectedColor;
    public BreathPause()
    {
        InitializeComponent();
        
        _selectedColor = ResourceExtensions.GetColor("SelectedDivisionMarker");
        _unselectedColor = ResourceExtensions.GetColor("UnselectedDivisionMarker");
        
        this.WhenActivated(d =>
        {
            d(this.OneWayBind(ViewModel, vm => vm.ChunkDuration, v => v.WidthRequest));
            d(this.WhenAnyValue(breathPause => breathPause.ViewModel.Scale).Subscribe(UpdateWidth));
            d(this.WhenAnyValue(breathPause => breathPause.ViewModel.IsDivisionMarker).Subscribe(UpdateBorderStyle));
        });
    }

    private void UpdateWidth(double scale)
    {
        WidthRequest = ViewModel.ChunkDuration * scale / MillisecondsPerSeconds;
    }

    private void UpdateBorderStyle(bool isDivisionMarker)
    {
        DivisionButton.SetValue(BackgroundColorProperty, isDivisionMarker ? _selectedColor : _unselectedColor);
    }
}