using System.Globalization;

namespace Render.Sequencer.Core.Converters
{
    public enum CompareOperator
    {
        Equal,
        GreaterThan,
        LessThan,
    }

    internal class WidthToBoolConverter : IValueConverter
    {
        public double TargetValue { get; set; }

        public CompareOperator Operator { get; set; }

        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is double width)
            {
                return Operator switch
                {
                    CompareOperator.Equal => width == TargetValue,
                    CompareOperator.GreaterThan => width >= TargetValue,
                    CompareOperator.LessThan => width <= TargetValue,
                    _ => throw new NotImplementedException(),
                };
            }

            return false;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return !boolValue;
            }

            return false;
        }
    }
}
