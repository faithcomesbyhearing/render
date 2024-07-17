using Android.App;
using Android.Runtime;
using Splat;
using Render.Interfaces;
using Render.Platforms.Droid.Bootstrap;

namespace Render;

[Application]
public class MainApplication : MauiApplication
{
	public MainApplication(IntPtr handle, JniHandleOwnership ownership)
		: base(handle, ownership) {	}

    protected override MauiApp CreateMauiApp()
    {
        Locator.CurrentMutable.RegisterConstant(
            new AppDirectory(FileSystem.Current.AppDataDirectory, Path.GetTempPath()),
            typeof(IAppDirectory));

        Couchbase.Lite.Support.Droid.Activate(this);
	    
        var builder = MauiApp.CreateBuilder();
        return Program.BuildApp(
            builder: builder,
            addPlatfromHandlersAction: Bootstrapper.AddHandlers,
            addPlatformEffects: Bootstrapper.AddEffects,
            addPlatformDependencies: Bootstrapper.AddDependencies);
    }
}