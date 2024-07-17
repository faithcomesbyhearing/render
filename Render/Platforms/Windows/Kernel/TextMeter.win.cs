using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Render.Kernel;

namespace Render.Platforms.Kernel
{
    public class TextMeter : ITextMeter
    {
        public double MeasureTextSize(string text, double width, double fontSize, string fontName = null)
        {
            TextBlock textBlock = new TextBlock();

            if (fontName == null)
            {
                fontName = "Segoe UI";
            }

            textBlock.TextWrapping = TextWrapping.Wrap;
            textBlock.Text = text;
            textBlock.FontFamily = new FontFamily(fontName);
            textBlock.FontSize = fontSize;
            textBlock.Measure(new Windows.Foundation.Size(width, double.PositiveInfinity));

            return textBlock.DesiredSize.Height;
        }
    }
}
