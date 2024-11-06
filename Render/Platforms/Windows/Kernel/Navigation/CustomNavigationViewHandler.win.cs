using Microsoft.Maui.Handlers;
using Frame = Microsoft.UI.Xaml.Controls.Frame;

namespace Render.Platforms.Kernel
{
	/// <summary>
	/// Handler for navigation page, to use CustomStackNavigationManager
	/// </summary>
    public class CustomNavigationViewHandler : NavigationViewHandler
	{
		public static new CommandMapper<IStackNavigationView, INavigationViewHandler> CommandMapper = new(ViewCommandMapper)
		{
			[nameof(IStackNavigation.RequestNavigation)] = RequestNavigation
		};

		private CustomStackNavigationManager _navigationManager;

		public CustomNavigationViewHandler() 
			: base(Mapper, CommandMapper) { }

		public CustomNavigationViewHandler(IPropertyMapper mapper)
			: base(mapper ?? Mapper, CommandMapper) { }

		public CustomNavigationViewHandler(IPropertyMapper mapper, CommandMapper commandMapper)
			: base(mapper ?? Mapper, commandMapper ?? CommandMapper) { }

		protected override Frame CreatePlatformView()
		{
			_navigationManager = new CustomStackNavigationManager(MauiContext ?? throw new InvalidOperationException("MauiContext cannot be null"));

			return new Frame();
		}

		protected override void ConnectHandler(Frame platformView)
		{
			_navigationManager?.Connect(VirtualView, platformView);

			base.ConnectHandler(platformView);
		}

        protected override void DisconnectHandler(Frame platformView)
        {
			_navigationManager?.Disconnect(VirtualView, platformView);

            base.DisconnectHandler(platformView);
        }

		public static new void RequestNavigation(INavigationViewHandler arg1, IStackNavigation arg2, object? arg3)
		{
			if (arg1 is CustomNavigationViewHandler platformHandler && arg3 is NavigationRequest nr)
			{
				platformHandler._navigationManager?.NavigateTo(nr);
			}
			else
			{
				throw new InvalidOperationException("Args must be NavigationRequest");
			}
		}
	}
}