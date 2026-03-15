using System.Globalization;

namespace Trenios.Mobile.Converters;

public class ImageFallbackConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string url && !string.IsNullOrWhiteSpace(url))
            return ImageSource.FromUri(new Uri(url));

        return ImageSource.FromFile("no_image.png");
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
