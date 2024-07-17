namespace Render.Kernel.CustomRenderer;

public class CustomPicker : Picker
{
	public static readonly BindableProperty IconSizeProperty = BindableProperty.Create(
			nameof(IconSize),
			typeof(double),
			typeof(CustomPicker),
			defaultValue: 29d);

	public double IconSize
	{
		get => (double)GetValue(IconSizeProperty);
		set => SetValue(IconSizeProperty, value);
	}
}
