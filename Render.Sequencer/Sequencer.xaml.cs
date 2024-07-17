namespace Render.Sequencer;

public partial class Sequencer : ContentView
{
    public static readonly BindableProperty WaveFormsMarginProperty = BindableProperty.Create(
        propertyName: nameof(WaveFormsMargin),
        returnType: typeof(Thickness),
        declaringType: typeof(Sequencer),
        defaultValue: default(Thickness),
        propertyChanged: WaveFormsMarginPropertyChanged);

    private static void WaveFormsMarginPropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is not Sequencer sequencer)
        {
            return;
        }

        if (newValue is Thickness margin)
        {
            sequencer.waveFormView.Margin = margin;
            sequencer.scrollerView.Margin = margin;
        }
    }

    public Thickness WaveFormsMargin
    {
        get => (Thickness)GetValue(WaveFormsMarginProperty);
        set => SetValue(WaveFormsMarginProperty, value);
    }

    public Sequencer()
    {
        InitializeComponent();
    }
}