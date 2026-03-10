using System.Globalization;

namespace Trenios.Mobile.Converters;

public class BoolToMarginConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // When true (has selected order), add right margin for details panel
        // When false (no selected order), no margin
        if (value is bool boolValue && boolValue)
            return new Thickness(0, 0, 400, 12);
        return new Thickness(0, 0, 0, 12);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
