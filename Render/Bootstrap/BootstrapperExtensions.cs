using Microsoft.Extensions.Logging;
using Render.Kernel;
using Render.Kernel.WrappersAndExtensions;
using Splat;
using ReactiveUI;
using System.Reflection;
using Render.Interfaces.AudioServices;
using Render.Services.AudioServices;
using Render.TempFromVessel.Kernel;
using NLog.Extensions.Logging;
using NLog;
using LogLevel = NLog.LogLevel;
using NLog.Targets;
using Splat.NLog;
using Render.Interfaces;
using Render.Services.SyncService.DbFolder;

namespace Render.Bootstrap
{
    public static class BootstrapperExtensions
    {
        public static MauiAppBuilder AddDependencies(this MauiAppBuilder builder, Action addPlatformDependencies)
        {
            addPlatformDependencies.Invoke();

            return builder
                .AddGenericDependencies()
                .AddViewsAndViewModels();
        }

        public static MauiAppBuilder AddFonts(this MauiAppBuilder builder)
        {
            return builder.ConfigureFonts(AddFonts);

            void AddFonts(IFontCollection fonts)
            {
                fonts.AddFont("icons.ttf", "Icons");
                fonts.AddFont("Roboto-100-Thin.ttf", "ThinFont");
                fonts.AddFont("Roboto-100-ThinItalic.ttf", "ThinItalicFont");
                fonts.AddFont("Roboto-300-Light.ttf", "LightFont");
                fonts.AddFont("Roboto-300-LightItalic.ttf", "LightItalicFont");
                fonts.AddFont("Roboto-400-Italic.ttf", "RegularItalicFont");
                fonts.AddFont("Roboto-400-Regular.ttf", "RegularFont");
                fonts.AddFont("Roboto-500-Medium.ttf", "MediumFont");
                fonts.AddFont("Roboto-500-MediumItalic.ttf", "MediumItalicFont");
                fonts.AddFont("Roboto-700-Bold.ttf", "BoldFont");
                fonts.AddFont("Roboto-700-BoldItalic.ttf", "BoldItalicFont");
                fonts.AddFont("Roboto-900-Black.ttf", "BlackFont");
                fonts.AddFont("Roboto-900-BlackItalic.ttf", "BlackItalicFont");
            }
        }

        public static MauiAppBuilder AddHandlers(this MauiAppBuilder builder, Action<IMauiHandlersCollection> addPlatfromHandlersAction)
        {
            return builder.ConfigureMauiHandlers(addPlatfromHandlersAction);
        }

        public static MauiAppBuilder AddEffects(this MauiAppBuilder builder, Action<IEffectsBuilder> addPlatformEffects)
        {
            return builder.ConfigureEffects(addPlatformEffects);
        }

        public static MauiAppBuilder AddLogging(this MauiAppBuilder builder)
        {

#if DEBUG || AUTOTESTS
            AddLogging(builder, logToConsole: true, logToFile: true);
#elif TEST
            AddLogging(builder, logToConsole: false, logToFile: true);
#else
            AddLogging(builder, logToConsole: false, logToFile: false);
#endif

            return builder;
        }

        private static MauiAppBuilder AddGenericDependencies(this MauiAppBuilder builder)
        {
            Locator.CurrentMutable.RegisterConstant(new AppSettings(), typeof(IAppSettings));
            Locator.CurrentMutable.RegisterConstant(new ViewModelContextProvider(), typeof(IViewModelContextProvider));
            Locator.CurrentMutable.RegisterConstant(new AudioActivityService(Locator.Current.GetService<IViewModelContextProvider>().GetLogger(typeof(AudioActivityService))), typeof(IAudioActivityService));
            Locator.CurrentMutable.RegisterConstant(new MenuPopupService(), typeof(IMenuPopupService));
            Locator.CurrentMutable.RegisterConstant(new ModalService(Locator.Current.GetService<IViewModelContextProvider>()), typeof(IModalService));
            Locator.CurrentMutable.RegisterConstant(new ProjectDownloadService(Locator.Current.GetService<IViewModelContextProvider>()), typeof(IProjectDownloadService));
            Locator.CurrentMutable.RegisterConstant(new UsbSyncFolderStorageService(Locator.Current.GetService<IModalService>()), typeof(IUsbSyncFolderStorageService));
            Locator.CurrentMutable.RegisterConstant(new DbBackupService(Locator.Current.GetService<IAppDirectory>(), Locator.Current.GetService<IAppSettings>()), typeof(IDbBackupService));
            Locator.CurrentMutable.RegisterConstant(new AppLifecycleBus(Locator.Current.GetService<IViewModelContextProvider>()), typeof(AppLifecycleBus));
            return builder;
        }

        private static MauiAppBuilder AddViewsAndViewModels(this MauiAppBuilder builder)
        {
            Locator.CurrentMutable.Register(() => Application.Current, typeof(IScreen));
            Locator.CurrentMutable.RegisterViewsForViewModels(Assembly.GetExecutingAssembly());

            return builder;
        }
        
        private static void AddLogging(MauiAppBuilder builder, bool logToConsole, bool logToFile)
        {
            builder.Logging.ClearProviders();
            builder.Logging.AddNLog();

            // send Analytics events to AppCenter only from our code (all classes with 'Render.' prefix in namespace)
            LogManager.Setup()
                .RegisterAppCenter()
                .LoadConfiguration(c => c
                    .ForLogger("Render.*") 
                    .FilterLevels(LogLevel.Info, LogLevel.Warn)
                    .WriteToAppCenter(reportExceptionAsCrash: false));

            // send Exceptions to AppCenter from all classes (including exceptions from external code)
            LogManager.Setup()
                .RegisterAppCenter()
                .LoadConfiguration(c => c
                    .ForLogger("*")
                    .FilterLevels(LogLevel.Error, LogLevel.Fatal)
                    .WriteToAppCenter(reportExceptionAsCrash: true));

            if (logToConsole)
            {
                var logLayout = "${longdate}|${level}|${message} |${all-event-properties} ${exception:format=tostring}";

                LogManager.Setup()
                    .RegisterMauiLog()
                    .LoadConfiguration(c => c.ForLogger(LogLevel.Trace).WriteToMauiLog(logLayout));
            }

            if (logToFile)
            {
                // You have to get the current NLog configuration,
                // make changes to this LoggingConfiguration object,
                // then assign it back to LogManager.Configuration.
                var config = LogManager.Configuration;
                var tempDirectoryPath = Locator.Current.GetService<IAppDirectory>().Temporary;

                config.AddRule(LogLevel.Trace, LogLevel.Fatal, new FileTarget()
                {
                    FileName = Path.Combine(tempDirectoryPath, "logs/${shortdate}.log"),
                    Layout = "${longdate} ${uppercase:${level}} ${logger} ${message} ${exception:format=tostring} ${all-event-properties}"
                });

                LogManager.Configuration = config;
            }

            // use NLog for ReactiveUI framework logs
            Locator.CurrentMutable.UseNLogWithWrappingFullLogger();
        }
    }
}