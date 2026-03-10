using System.Globalization;

namespace Trenios.Mobile.Converters;

public class BoolToWidthConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // When true (has selected order), return 400
        // When false (no selected order), return 0
        if (value is bool boolValue && boolValue)
            return 400.0;
        return 0.0;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
