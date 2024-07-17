using ReactiveUI;

namespace Render.Components.DivisionPlayer;

public partial class Division
{
    private const double MillisecondsPerSeconds = 1000d;
    public Division()
    {
        InitializeComponent();
        
        this.WhenActivated(d =>
        {
            d(this.OneWayBind(ViewModel, vm => vm.ChunkDuration, v => v.WidthRequest));
            d(this.OneWayBind(ViewModel, vm => vm.Text, v => v.PassageNumberLabel.Text));
            d(this.WhenAnyValue(division => division.ViewModel.Scale).Subscribe(UpdateWidth));
            
            d(this.WhenAnyValue(
                    division => division.ViewModel.LeftMargin,
                    division => division.ViewModel.RightMargin)
                .Subscribe(((double LabelMargin, double RightMargin) properties) =>
                {
                    UpdateLabelPosition(properties.LabelMargin, properties.RightMargin);
                }));
        });
    }
    
    private void UpdateWidth(double scale)
    {
        WidthRequest = ViewModel.ChunkDuration * scale / MillisecondsPerSeconds;
    }

    private void UpdateLabelPosition(double labelMargin, double rightMargin)
    {
        PassageNumberLabel.Margin = new Thickness(labelMargin, 0, rightMargin, 0);
    }
}