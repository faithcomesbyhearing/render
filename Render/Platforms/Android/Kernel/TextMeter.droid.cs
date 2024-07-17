using Android.Widget;
using Android.Views;
using Android.Graphics;
using Android.Util;
using Render.Kernel;

namespace Render.Platforms.Kernel
{
    public class TextMeter : ITextMeter
    {
        private Typeface textTypeface;

        public double MeasureTextSize(string text, double width, double fontSize, string fontName = null)
        {
            var textView = new TextView(global::Android.App.Application.Context);
            textView.Typeface = GetTypeface(fontName);
            textView.SetText(text, TextView.BufferType.Normal);
            textView.SetTextSize(ComplexUnitType.Px, (float)fontSize);

            int widthMeasureSpec = Android.Views.View.MeasureSpec.MakeMeasureSpec((int)width, MeasureSpecMode.AtMost);
            int heightMeasureSpec = Android.Views.View.MeasureSpec.MakeMeasureSpec(0, MeasureSpecMode.Unspecified);

            textView.Measure(widthMeasureSpec, heightMeasureSpec);
            return textView.MeasuredHeight;
        }

        private Typeface GetTypeface(string fontName)
        {
            if (fontName == null)
            {
                return Typeface.Default;
            }

            if (textTypeface == null)
            {
                textTypeface = Typeface.Create(fontName, TypefaceStyle.Normal);
            }

            return textTypeface;
        }
    }
}