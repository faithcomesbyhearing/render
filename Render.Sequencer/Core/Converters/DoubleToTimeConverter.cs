using System.Globalization;

namespace Render.Sequencer.Core.Converters;

internal class DoubleToTimeConverter : IValueConverter
{
    private static TimeSpan _oneHour = TimeSpan.FromHours(1);

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null || value is not double)
        {
            return "";
        }

        var span = TimeSpan.FromSeconds((double)value);

        return span < _oneHour ? 
            span.ToString(@"mm\:ss") : 
            span.ToString(@"hh\:mm\:ss");
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}