using System.Globalization;

namespace Trenios.Mobile.Converters;

public class BoolToColumnSpanConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // When true (has selected order), span 1 column
        // When false (no selected order), span 2 columns to fill full width
        if (value is bool boolValue && boolValue)
            return 1;
        return 2;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
