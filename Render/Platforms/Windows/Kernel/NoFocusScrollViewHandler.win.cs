using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;

namespace Render.Platforms.Kernel
{
    // Workaround for https://github.com/dotnet/maui/issues/11472
    public class NoFocusScrollViewHandler : ScrollViewHandler
    {
        public NoFocusScrollViewHandler()
        {
            Mapper.AppendToMapping(nameof(IScrollView.Content), MapContent);
        }

        private static new void MapContent(IScrollViewHandler handler, IScrollView view)
        {
            if (handler.PlatformView is null
                || handler.MauiContext is null
                || view.PresentedContent is null
                || handler.PlatformView.Content is not ContentPanel)
            {
                return;
            }

            // Maui always puts the ScrollView content into a ContentPanel.
            // Setting IsTabStop = true on the panel prevents us from running into
            // https://github.com/dotnet/maui/issues/11472
            var panel = (ContentPanel)handler.PlatformView.Content;
            panel.IsTabStop = true;
        }
    }
}
