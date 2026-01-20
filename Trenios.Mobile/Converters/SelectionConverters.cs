using System.Globalization;

namespace Trenios.Mobile.Converters;

/// <summary>
/// Converts bool to border stroke color for selection state
/// </summary>
public class BoolToColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isSelected && isSelected)
            return Application.Current?.Resources["Primary"] ?? Colors.Orange;

        return Application.Current?.Resources["Gray300"] ?? Colors.LightGray;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts bool to background color for selection state
/// </summary>
public class BoolToBackgroundConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isSelected && isSelected)
        {
            var primary = Application.Current?.Resources["Primary"] as Color ?? Colors.Orange;
            return primary.WithAlpha(0.15f);
        }

        return Colors.Transparent;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts bool to text color for selection state
/// </summary>
public class BoolToTextColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isSelected && isSelected)
            return Application.Current?.Resources["Primary"] ?? Colors.Orange;

        return Application.Current?.Resources["Gray700"] ?? Colors.DarkGray;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Extracts first letter from string for placeholder display
/// </summary>
public class FirstLetterConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string str && !string.IsNullOrEmpty(str))
            return str[0].ToString().ToUpper();

        return "?";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Returns true if quantity is greater than 0, or color if parameter is "Border"
/// </summary>
public class QuantityToBoolConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var hasQuantity = value is int quantity && quantity > 0;

        // If parameter is "Border", return a color
        if (parameter?.ToString() == "Border")
        {
            return hasQuantity
                ? Application.Current?.Resources["Primary"] ?? Colors.Orange
                : Application.Current?.Resources["Gray200"] ?? Colors.LightGray;
        }

        return hasQuantity;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts bool to stroke color (Primary when selected, Gray300 when not)
/// Alias for BoolToColorConverter - used for border strokes
/// </summary>
public class BoolToStrokeConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isSelected && isSelected)
            return Application.Current?.Resources["Primary"] ?? Colors.Orange;

        return Application.Current?.Resources["Gray300"] ?? Colors.LightGray;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Converts bool to fill background for radio/checkbox circles
/// (Primary when selected, Transparent when not)
/// </summary>
public class BoolToCircleBackgroundConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isSelected && isSelected)
            return Application.Current?.Resources["Primary"] ?? Colors.Orange;

        return Colors.Transparent;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Converts item count to height for single-select CollectionView
/// Each item is ~54px (12+12 padding + 26 radio + 4 margins) + 8px spacing
/// </summary>
public class CountToHeightConverter : IValueConverter
{
    private const double ItemHeight = 54;
    private const double ItemSpacing = 8;

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int count && count > 0)
        {
            return (count * ItemHeight) + ((count - 1) * ItemSpacing);
        }
        return 0;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Converts item count to height for multi-select CollectionView
/// Each item is ~70px (12+12 padding + content with subtitle) + 8px spacing
/// </summary>
public class CountToHeightMultiConverter : IValueConverter
{
    private const double ItemHeight = 70;
    private const double ItemSpacing = 8;

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int count && count > 0)
        {
            return (count * ItemHeight) + ((count - 1) * ItemSpacing);
        }
        return 0;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
