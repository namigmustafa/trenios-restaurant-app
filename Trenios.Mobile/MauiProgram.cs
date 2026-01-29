using Microsoft.Extensions.Logging;
using Trenios.Mobile.Pages;
using Trenios.Mobile.Services;
using Trenios.Mobile.ViewModels;

namespace Trenios.Mobile;

public static class MauiProgram
{
    // Configure your API base URL here
    private const string ApiBaseUrl = "https://app-trenios-test.azurewebsites.net";

    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // Register HttpClient
        builder.Services.AddSingleton(sp =>
        {
            var client = new HttpClient
            {
                BaseAddress = new Uri(ApiBaseUrl),
                Timeout = TimeSpan.FromSeconds(30)
            };
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            return client;
        });

        // Register Services
        builder.Services.AddSingleton<ApiService>();
        builder.Services.AddSingleton<AuthService>();
        builder.Services.AddSingleton<SelectionService>();
        builder.Services.AddSingleton<ProductService>();
        builder.Services.AddSingleton<OrderService>();
        builder.Services.AddSingleton<OrderHubService>();
        builder.Services.AddSingleton(_ => LocalizationService.Instance);

        // Register ViewModels
        builder.Services.AddTransient<LoginViewModel>();
        builder.Services.AddTransient<RestaurantSelectionViewModel>();
        builder.Services.AddTransient<BranchSelectionViewModel>();
        builder.Services.AddTransient<POSViewModel>();
        builder.Services.AddTransient<OrdersViewModel>();
        builder.Services.AddTransient<KitchenDisplayViewModel>();

        // Register Pages
        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<RestaurantSelectionPage>();
        builder.Services.AddTransient<BranchSelectionPage>();
        builder.Services.AddTransient<POSPage>();
        builder.Services.AddTransient<OrdersPage>();
        builder.Services.AddTransient<KitchenDisplayPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
