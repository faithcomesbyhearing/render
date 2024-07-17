namespace Render.Sequencer.Views.WaveForm.Items;

public partial class WaveFormItemView : ContentView
{
    public WaveFormItemView()
    {
		InitializeComponent();
	}

    public void SubscribeToParentSize(VisualElement parent)
    {
        parent.SizeChanged -= ParentSizeChanged;
        parent.SizeChanged += ParentSizeChanged;
    }

    public void UnsubscribeFromParentSize(VisualElement parent)
    {
        parent.SizeChanged -= ParentSizeChanged;
    }

    private void ParentSizeChanged(object? sender, EventArgs e)
    {
        if (sender is VisualElement parent)
        {
            HeightRequest = parent.Height;
        }
    }
}