using Render.Sequencer.Core.Utils.Extensions;
using System.Globalization;

namespace Render.Sequencer.Core.Converters;

internal class IconKeyToGlyphConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return ResourceExtensions.GetResourceValue(value?.ToString() ?? string.Empty);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}