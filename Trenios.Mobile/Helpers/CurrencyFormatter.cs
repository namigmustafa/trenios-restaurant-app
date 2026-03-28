using Trenios.Mobile.Models.Api;

namespace Trenios.Mobile.Helpers;

public static class CurrencyFormatter
{
    public static Currency Current { get; set; } = Currency.EUR;

    public static string Format(decimal value, Currency? currency = null)
    {
        return (currency ?? Current) switch
        {
            Currency.EUR => $"€{value:F2}",
            Currency.USD => $"${value:F2}",
            Currency.AZN => $"{value:F2} ₼",
            Currency.GBP => $"£{value:F2}",
            Currency.TRY => $"₺{value:F2}",
            Currency.RUB => $"{value:F2} ₽",
            Currency.DKK => $"{value:F2} kr",
            _ => $"€{value:F2}"
        };
    }
}
