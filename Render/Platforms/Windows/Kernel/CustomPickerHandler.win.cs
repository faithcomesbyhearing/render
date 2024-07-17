using Microsoft.Maui.Handlers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Render.Kernel.CustomRenderer;
using Grid = Microsoft.UI.Xaml.Controls.Grid;

namespace Render.Platforms.Kernel
{
	public class CustomPickerHandler : PickerHandler
	{
		private AnimatedIcon? _pickerIcon;

		static CustomPickerHandler()
		{
			Mapper.AppendToMapping(nameof(CustomPicker.IconSize), CustomPickerIconSizeChanged);
		}

		protected override void ConnectHandler(ComboBox platformView)
		{
			base.ConnectHandler(platformView);

			platformView.Loaded += PickerLoaded;
		}

		private void PickerLoaded(object sender, RoutedEventArgs e)
		{
			PlatformView.ApplyTemplate();

			var comboBoxRoot = (Grid)VisualTreeHelper.GetChild(PlatformView, 0);
			_pickerIcon = (AnimatedIcon)comboBoxRoot.FindName("DropDownGlyph");
			UpdatePickerIconSize((CustomPicker)VirtualView);
			
			PlatformView.BorderThickness = new Microsoft.UI.Xaml.Thickness(0);
			PlatformView.BorderBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent);	
		}

		private static void CustomPickerIconSizeChanged(IPickerHandler handler, IPicker picker)
		{
			if (picker is CustomPicker customPicker && handler is CustomPickerHandler pickerHandler)
			{
				pickerHandler.UpdatePickerIconSize(customPicker);
			}
		}

		private void UpdatePickerIconSize(CustomPicker customPicker)
		{
			if (customPicker is not null && _pickerIcon is not null)
			{
				_pickerIcon.Width = customPicker.IconSize;
				_pickerIcon.Height = customPicker.IconSize;
			}
		}
	}
}