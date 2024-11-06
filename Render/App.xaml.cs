using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using ReactiveUI;
using Render.Kernel;
using Render.TempFromVessel.Kernel;
using Splat;
using System.Globalization;
using Couchbase.Lite.Logging;
using Render.Interfaces;
using Render.Services.SyncService.DbFolder;

namespace Render;

public partial class App : Application, IScreen
{
    public AppLifecycleBus AppLifecycleBus { get; }
    public RoutingState Router { get; } = new RoutingState();

    public App()
    {
        InitializeComponent();

        AppLifecycleBus = Locator
            .Current
            .GetService<AppLifecycleBus>();

        MainPage = Locator
            .Current
            .GetService<IViewModelContextProvider>()
            .CreateLaunchPage(Router);
    }

    protected override Window CreateWindow(IActivationState activationState)
    {
        var window = base.CreateWindow(activationState);

#if DEMO
        window.Title = "Render Demo";
#else
        window.Title = "Render";
#endif

        return window;
    }

    protected override void OnStart()
    {
        // Handle when your app starts
        var appSettings = Locator.Current.GetService<IAppSettings>();
        var appDirectory = Locator.Current.GetService<IAppDirectory>();
        var directoryHelper = Locator.Current.GetService<IDbBackupService>();
        
        if (Directory.Exists(appDirectory.TempAudio) is false)
        {
            Directory.CreateDirectory(appDirectory.TempAudio);
        }

        if (Directory.Exists(appDirectory.DbBackup))
        {
            directoryHelper.RemoveAllDatabaseBackupsAsync();
        }

        EnableCouchbaseLogging(appDirectory.TempAudio);

        AppCenter.SetCountryCode(CultureInfo.CurrentCulture.Name);
        AppCenter.Start(appSettings.AppCenterUwpKey, typeof(Analytics), typeof(Crashes));

        RemoveTempAudios(appDirectory.TempAudio);

        base.OnStart();
    }

    private void RemoveTempAudios(string tempAudioDir)
    {
        Task.Run(() =>
        {
            try
            {
                var tempAudioFilesPaths = Directory.GetFiles(tempAudioDir);

                foreach (var path in tempAudioFilesPaths)
                {
                    File.Delete(path);
                }
            }
            catch (Exception exception)
            {
                Locator.Current
                    .GetService<IViewModelContextProvider>()
                    .GetLogger(typeof(App))
                    .LogError(exception);
            }
        });
    }

    private static void EnableCouchbaseLogging(string tempAudioDir)
    {
#if DEBUG || TEST
		var appSettings = Locator.Current.GetService<IAppSettings>();
        var isDevEnvironment = appSettings.Environment is "dev";

        if(isDevEnvironment is false)
        {
            return;
		}

		var tempFolder = Path.Combine(tempAudioDir, "cbllog");
		var cbConfig = new LogFileConfiguration(tempFolder)
		{
			MaxRotateCount = 5,
			MaxSize = 10000000, // 10 MB
			UsePlaintext = true
		};
		Couchbase.Lite.Database.Log.File.Config = cbConfig; // Apply configuration
		Couchbase.Lite.Database.Log.File.Level = Couchbase.Lite.Logging.LogLevel.Debug;
#endif
    }
}