using Render.Platforms.Kernel;
using Render.Kernel.CustomRenderer;
using Render.Kernel;
using Render.Platforms.Kernel.AudioPlayer;
using Render.Services.AudioPlugins.AudioPlayer;
using Render.Platforms.Kernel.AudioRecorder;
using Render.Services.AudioPlugins.AudioRecorder.Interfaces;
using Render.Platforms.Permissions;
using Render.Services;

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
            Splat.Locator.CurrentMutable.Register(() => new CustomMicrophone(), typeof(ICustomMicrophonePermission));
            Splat.Locator.CurrentMutable.Register(() => new AudioRecorderFactory(), typeof(IAudioRecorderFactory));
            Splat.Locator.CurrentMutable.Register(() => new AudioPlayer(), typeof(IAudioPlayer));
            Splat.Locator.CurrentMutable.Register(() => new CloseApplication(), typeof(ICloseApplication));
            Splat.Locator.CurrentMutable.Register(() => new DownloadService(), typeof(IDownloadService));
            Splat.Locator.CurrentMutable.Register(() => new TextMeter(), typeof(ITextMeter));
            Splat.Locator.CurrentMutable.Register(() => new LocalizationService(), typeof(ILocalizationService));
        }
    }
}