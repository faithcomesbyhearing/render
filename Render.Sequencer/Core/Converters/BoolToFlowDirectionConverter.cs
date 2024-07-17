using System.Globalization;

namespace Render.Sequencer.Core.Converters;

internal class BoolToFlowDirectionConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isRightToLeft)
        {
            return isRightToLeft ? FlowDirection.RightToLeft : FlowDirection.MatchParent;
        }

        return FlowDirection.MatchParent;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is FlowDirection flowDirection && flowDirection is FlowDirection.RightToLeft;
    }
}