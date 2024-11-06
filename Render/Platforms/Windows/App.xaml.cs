using System.Diagnostics;
using System.Reactive.Concurrency;
using Windows.ApplicationModel;
using Windows.Storage;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.LifecycleEvents;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using ReactiveUI;
using Render.Interfaces;
using Render.Platforms.Win.Bootstrap;
using Render.TempFromVessel.Kernel;
using Render.Utilities;
using Splat;
using AutomationProperties = Microsoft.UI.Xaml.Automation.AutomationProperties;
using UnhandledExceptionEventArgs = Microsoft.UI.Xaml.UnhandledExceptionEventArgs;
using Version = Render.Kernel.Version;
using Window = Microsoft.UI.Xaml.Window;
using Microsoft.UI;
using Microsoft.Maui.Platform;
using Render.Native;

namespace Render.WinUI;

public partial class App : MauiWinUIApplication
{
    private const int GuidLength = 36;
    private const int CertLength = 13;

    public App()
    {
        SetSingleAppInstance();

        UnhandledException += OnUnhandledException;
        TaskScheduler.UnobservedTaskException += TaskSchedulerOnUnobservedTaskException;
        AppDomain.CurrentDomain.UnhandledException += OnAppDomainUnhandledException;

        RxApp.DefaultExceptionHandler = new MauiObservableExceptionHandler();

        InitializeComponent();
    }

    protected override MauiApp CreateMauiApp()
    {
#if DEMO
        InitializeDemoDatabase();
#endif
        Locator.CurrentMutable.RegisterConstant(
            new AppDirectory(FileSystem.Current.AppDataDirectory, ApplicationData.Current.TemporaryFolder.Path),
            typeof(IAppDirectory));

        var builder = MauiApp.CreateBuilder();

        AppendAutomationIdMapping();

        builder.ConfigureLifecycleEvents(events =>
        {
            events.AddWindows(lifecycleBuilder =>
            {
                lifecycleBuilder.OnWindowCreated((window) =>
                {
                    SetupWindowClosing(window);
                    SetFullScreen(window);

                    // hack: fix #20559
                    window.ExtendsContentIntoTitleBar = false;

                    // When window.ExtendsContentIntoTitleBar is false, the window title contains the default noname icon.
                    // Set 'Render.ico' resource for the window title.
                    window.AppWindow.SetIcon(@"Platforms\Windows\Shortcut\render.ico");
                    WndProcessService.StartProcess(window);
                });
            });
        });

        return Render.Program.BuildApp(
            builder: builder,
            addPlatfromHandlersAction: Bootstrapper.AddHandlers,
            addPlatformEffects: Bootstrapper.AddEffects,
            addPlatformDependencies: Bootstrapper.AddDependencies);
    }

    [Conditional("DEMO")]
	private void InitializeDemoDatabase()
	{
        DemoHelper.InitializeDatabase(FileSystem.Current.AppDataDirectory);
	}

	private void SetupWindowClosing(Window window)
    {
        window.GetAppWindow().Closing += async (s, a) =>
        {
            a.Cancel = true;

            var appLifecycleBus = Locator.Current.GetService<AppLifecycleBus>();
            var canExit = await appLifecycleBus.OnAppClosingAsync();
            if (canExit)
            {
                Microsoft.UI.Xaml.Application.Current.Exit();
            }
        };
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        base.OnLaunched(args);

        SetSoftwareVersion();
        CreateDesktopShortcut();
    }

    private void SetSoftwareVersion()
    {
        var version = Package.Current.Id.Version;
        var appSettings = Locator.Current.GetService<IAppSettings>();

        Version.SoftwareVersion = appSettings.Environment != "live"
            ? $"{appSettings.Environment}-{version.Major}.{version.Minor}.{version.Build}"
            : $"{version.Major}.{version.Minor}.{version.Build}";

        Version.SoftwareVersionWithFourthNumber = $"{version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
    }

    private static void SetSingleAppInstance()
    {
        var process = Process.GetCurrentProcess();
        var processes = Process.GetProcessesByName(process.ProcessName);

        if (processes.Length > 1)
        {
            var initial = processes.FirstOrDefault(p => p.Id != process.Id);

            var myWndId = Win32Interop.GetWindowIdFromWindow(initial.MainWindowHandle);
            var window = AppWindow.GetFromWindowId(myWndId);
            (window.Presenter as OverlappedPresenter)?.Maximize();

            process.Kill();
        }
    }

    [Conditional("RELEASE"), Conditional("TEST"), Conditional("AUTOTESTS")]
    private static void SetFullScreen(Window window)
    {
        (window.AppWindow.Presenter as OverlappedPresenter)?.Maximize();
    }

    [Conditional("RELEASE"), Conditional("TEST")]
    public static void CreateDesktopShortcut()
    {
        ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
        var shortcutCreated = localSettings.Values.ContainsKey("ShortcutCreated");
        if (shortcutCreated)
        {
            return;
        }

        var installedLocationPath = Package.Current.InstalledLocation.Path;
        var familyName = Package.Current.Id.FamilyName;
        var shortcutName = GetShortcutName(familyName);

        var shortcutDirectory = Path.Combine(installedLocationPath,
                                    "Platforms",
                                    "Windows",
                                    "Shortcut");

        var lnkSourcePath = Path.Combine(shortcutDirectory, shortcutName);
        var iconSourcePath = Path.Combine(shortcutDirectory, "render.ico");

        var lnkDestinationPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), shortcutName);
        var iconDestinationPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                                        "Microsoft",
                                        "WindowsApps",
                                        familyName,
                                        "render.ico");

        if (Directory.Exists(shortcutDirectory))
        {
            File.Copy(iconSourcePath, iconDestinationPath, true);
            File.Copy(lnkSourcePath, lnkDestinationPath, true);
            localSettings.Values["ShortcutCreated"] = true;
        }
    }

    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        RenderLogger.LogError(e.Exception);
    }

    private void TaskSchedulerOnUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
    {
        RenderLogger.LogError(e.Exception);
    }

    private void OnAppDomainUnhandledException(object sender, System.UnhandledExceptionEventArgs e)
    {
        if (e?.ExceptionObject is null)
        {
            RenderLogger.LogError(new RenderAppDomainUnhandledException("Unhandled exception occured without data."));

            return;
        }

        if (e.ExceptionObject is Exception systemException)
        {
            RenderLogger.LogError(systemException, new Dictionary<string, string>
            {
                { nameof(System.UnhandledExceptionEventArgs.IsTerminating), e.IsTerminating.ToString() },
            });

            return;
        }

        RenderLogger.LogError(new RenderAppDomainUnhandledException("Custom type exception"), new Dictionary<string, string>
        {
            { nameof(e.IsTerminating), e.IsTerminating.ToString() },
            { nameof(Type), e.ExceptionObject.GetType().ToString() },
            { nameof(e.ExceptionObject), e.ExceptionObject.ToString() },
        });
    }

    private static string GetShortcutName(string familyName)
    {
        // familyName staging example: BAA1E222-D1B3-4A73-ADB4-3500D19C1B37-staging_k7n2kdbkeecg2
        if (familyName.Length > GuidLength + 1 + CertLength)
        {
            //deploymentName = "staging"
            var deploymentName = familyName[(GuidLength + 1)..^(CertLength + 1)];
            return $"Render 3 ({deploymentName}).lnk";
        }
        else //familyName live example : BAA1E222-D1B3-4A73-ADB4-3500D19C1B37_k7n2kdbkeecg2
        {
            return "Render 3.lnk";
        }
    }

    private static void AppendAutomationIdMapping()
    {
        ViewHandler.ViewMapper.AppendToMapping(
            nameof(IView.AutomationId),
            (handler, view) =>
            {
                // Workaround for https://github.com/dotnet/maui/issues/4715; Layouts are not exposed in AutomationTree
                if (string.IsNullOrEmpty(view.AutomationId))
                {
                    return;
                }

                var platformView = (FrameworkElement)handler.PlatformView;
                AutomationProperties.SetName(platformView, view.AutomationId);
            });
    }

    class MauiObservableExceptionHandler : IObserver<Exception>
    {
        public void OnNext(Exception error)
        {
            if (Debugger.IsAttached) Debugger.Break();

            RenderLogger.LogError(error);

            RxApp.MainThreadScheduler.Schedule(() => throw error);
        }

        public void OnError(Exception error)
        {
            if (Debugger.IsAttached) Debugger.Break();

            RenderLogger.LogError(error);

            RxApp.MainThreadScheduler.Schedule(() => throw error);
        }

        public void OnCompleted()
        {
            if (Debugger.IsAttached) Debugger.Break();
        }
    }

    class RenderAppDomainUnhandledException : Exception
    {
        public RenderAppDomainUnhandledException(string message) :
            base(message)
        { }
    }
}