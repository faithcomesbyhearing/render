using Splat;
using Render.Platforms.Kernel;
using Render.Kernel.CustomRenderer;
using Render.Kernel;
using Render.Platforms.Kernel.AudioPlayer;
using Render.Services.AudioPlugins.AudioPlayer;
using Render.Platforms.Kernel.AudioRecorder;
using Render.Services.AudioPlugins.AudioRecorder.Interfaces;
using Render.Platforms.Permissions;
using Render.Services;
using Render.Services.AudioServices;
using Render.Platforms.Kernel.AudioTools;

namespace Render.Platforms.Win.Bootstrap
{
    public static class Bootstrapper
    {
        public static void AddHandlers(IMauiHandlersCollection handlers)
        {
            handlers.AddHandler(typeof(CustomEditor), typeof(CustomEditorHandler));
            handlers.AddHandler(typeof(CustomEntry), typeof(CustomEntryHandler));
            handlers.AddHandler(typeof(CustomFrame), typeof(CustomFrameHandler));
            handlers.AddHandler(typeof(CustomSlider), typeof(CustomSliderHandler));
            handlers.AddHandler(typeof(CustomPicker), typeof(CustomPickerHandler));
            handlers.AddHandler(typeof(CustomSwitch), typeof(CustomSwitchHandler));
            handlers.AddHandler(typeof(CustomSwitch), typeof(CustomSwitchHandler));
            handlers.AddHandler(typeof(ToolTipButton), typeof(ToolTipButtonHandler));
            handlers.AddHandler(typeof(Panel), typeof(PanelHandler));
            handlers.AddHandler(typeof(NavigationPage), typeof(CustomNavigationViewHandler));
            handlers.AddHandler(typeof(NoFocusScrollView), typeof(NoFocusScrollViewHandler));
        }

        public static void AddEffects(IEffectsBuilder builder)
        {
            builder.Add<Render.Kernel.DragAndDrop.DragRecognizerEffect, DragRecognizerEffect>();
            builder.Add<Render.Kernel.DragAndDrop.DropRecognizerEffect, DropRecognizerEffect>();
            builder.Add<Render.Kernel.PropagateScrollEffect, Kernel.PropagateScrollEffect>();
            builder.Add<Render.Kernel.TouchActions.TouchEffect, TouchEffect>();
        }

        public static void AddDependencies()
        {
            Locator.CurrentMutable.Register(() => new CustomMicrophone(), typeof(ICustomMicrophonePermission));
            Locator.CurrentMutable.Register(() => new AudioRecorderFactory(), typeof(IAudioRecorderFactory));
            Locator.CurrentMutable.Register(() => new AudioPlayer(Locator.Current.GetService<IAudioDeviceMonitor>()), typeof(IAudioPlayer));
            Locator.CurrentMutable.Register(() => new CloseApplication(), typeof(ICloseApplication));
            Locator.CurrentMutable.Register(() => new DownloadService(), typeof(IDownloadService));
            Locator.CurrentMutable.Register(() => new TextMeter(), typeof(ITextMeter));
            Locator.CurrentMutable.Register(() => new LocalizationService(), typeof(ILocalizationService));
            Locator.CurrentMutable.RegisterConstant<IAudioDeviceMonitor>(new AudioDeviceMonitor());
        }
    }
}