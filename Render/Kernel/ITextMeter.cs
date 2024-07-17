namespace Render.Kernel
{
    public interface ITextMeter
	{
		double MeasureTextSize(string text, double width, double fontSize, string fontName = null);
	}
}