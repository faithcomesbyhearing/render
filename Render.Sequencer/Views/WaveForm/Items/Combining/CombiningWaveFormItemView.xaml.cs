namespace Render.Sequencer.Views.WaveForm.Items.Combining;

public partial class CombiningWaveFormItemView : ContentView
{
    public const double MinTimerWidth = 135;

	public CombiningWaveFormItemView()
	{
		InitializeComponent();

        combinedItemsStack.SizeChanged += CombinedItemsStackSizeChanged;
	}

    private void CombinedItemsStackSizeChanged(object? sender, EventArgs e)
    {
        timerBorder.IsVisible = combinedItemsStack.Width >= MinTimerWidth;
    }
}