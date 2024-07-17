using CommunityToolkit.Maui;
using Render.Bootstrap;
using Render.Sequencer.Bootstrap;
using SkiaSharp.Views.Maui.Controls.Hosting;

namespace Render;

public static class Program
{
	public static MauiApp BuildApp(
		MauiAppBuilder builder, 
		Action<IMauiHandlersCollection> addPlatfromHandlersAction,
		Action<IEffectsBuilder> addPlatformEffects,
		Action addPlatformDependencies)
	{
		return builder.UseMauiApp<App>()
			.UseSkiaSharp()
			.AddHandlers(addPlatfromHandlersAction)
			.AddEffects(addPlatformEffects)
			.AddDependencies(addPlatformDependencies)
			.AddFonts()
			.AddLogging()
			.UseSequencer()
			.UseMauiCommunityToolkit()
			.Build();
	} 
}