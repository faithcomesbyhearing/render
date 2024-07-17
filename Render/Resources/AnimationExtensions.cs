namespace Render.Resources
{
    public static class AnimationExtensions
    {
        public static async Task FadeOutAsync(this VisualElement element, uint duration = 300, Easing easing = null)
        {
            if (easing == null)
            {
                easing = Easing.CubicOut;
            }
            await element.FadeTo(0, duration, easing);
            element.IsVisible = false;
        }

        public static async Task FadeInAsync(this VisualElement element, uint duration = 300, Easing easing = null)
        {
            element.Opacity = 0;
            if (easing == null)
            {
                easing = Easing.CubicOut;
            }
            await Task.Delay(300);
            await element.FadeTo(1, duration, easing);

            element.IsVisible = true;
        }

        public static async Task FadeUpInAsync(this VisualElement element, uint duration = 300, uint distance = 15, Easing easing = null)
        {
            element.TranslationY = distance;
            element.Opacity = 0;
            if (easing == null)
            {
                easing = Easing.CubicOut;
            }
            await Task.Delay(300);
            await Task.WhenAll(
                element.FadeInAsync(duration, easing),
                element.TranslateTo(0, 0, duration)
            );
        }

        public static async Task FadeDownInAsync(this VisualElement element, uint duration = 300, uint distance = 15, Easing easing = null)
        {
            element.TranslationY = -distance;
            element.Opacity = 0;
            if (easing == null)
            {
                easing = Easing.CubicOut;
            }
            await Task.Delay(300);
            await Task.WhenAll(
                element.FadeInAsync(duration, easing),
                element.TranslateTo(0, 0, duration)
            );
        }

        public static async Task FadeLeftInAsync(this VisualElement element, uint duration = 300, uint distance = 15, Easing easing = null)
        {
            element.TranslationX = -distance;
            element.Opacity = 0;
            if (easing == null)
            {
                easing = Easing.CubicOut;
            }
            await Task.Delay(300);
            await Task.WhenAll(
                element.FadeInAsync(duration, easing),
                element.TranslateTo(0, 0, duration)
            );
        }

        public static async Task FadeRightInAsync(this VisualElement element, uint duration = 300, uint distance = 15, Easing
         easing = null)
        {
            element.TranslationX = distance;
            element.Opacity = 0;
            if (easing == null)
            {
                easing = Easing.CubicOut;
            }
            await Task.Delay(300);
            await Task.WhenAll(
                element.FadeInAsync(duration, easing),
                element.TranslateTo(0, 0, duration)
            );
        }
    }
}