using Render.Sequencer.Contracts.Enums;
using System.Globalization;

namespace Render.Sequencer.Core.Converters;

internal class NegativeFlagDirectionConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is FlagDirection flagDirection)
        {
            return -(double)flagDirection;
        }

        return 0;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}