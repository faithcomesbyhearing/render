using System.Globalization;

namespace Render.Sequencer.Core.Converters;

internal class RigthToLeftToDegreeConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isRightToLeft)
        {
            return isRightToLeft ? 180d : 0d;
        }

        return 0d;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}