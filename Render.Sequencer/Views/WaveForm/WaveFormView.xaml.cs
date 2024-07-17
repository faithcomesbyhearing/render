using Render.Sequencer.Views.WaveForm.Items;

namespace Render.Sequencer.Views.WaveForm;

public partial class WaveFormView : ContentView
{
	public WaveFormView()
	{
		InitializeComponent();
	}

    private void OnWaveformsCollectionChildAdded(object? sender, ElementEventArgs e)
    {
        if (e.Element is WaveFormItemView waveFormItem)
        {
            waveFormItem.SubscribeToParentSize(waveformsCollection);
        }
    }

    private void OnWaveformsCollectionChildRemoved(object? sender, ElementEventArgs e)
    {
        if (e.Element is WaveFormItemView waveFormItem)
        {
            waveFormItem.UnsubscribeFromParentSize(waveformsCollection);
        }
    }
}