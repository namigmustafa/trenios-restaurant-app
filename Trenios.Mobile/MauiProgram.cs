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
                fonts.AddFont("FluentSystemIcons-Regular.ttf", "FluentIcons");
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
            client.DefaultRequestHeaders.CacheControl = new System.Net.Http.Headers.CacheControlHeaderValue
            {
                NoCache = true,
                NoStore = true
            };
            return client;
        });

        // Register Services
        builder.Services.AddSingleton<ApiService>();
        builder.Services.AddSingleton<AuthService>();
        builder.Services.AddSingleton<SelectionService>();
        builder.Services.AddSingleton<ProductService>();
        builder.Services.AddSingleton<OrderService>();
        builder.Services.AddSingleton<TableService>();
        builder.Services.AddSingleton<OrderHubService>();
        builder.Services.AddSingleton(_ => LocalizationService.Instance);

        // Register ViewModels
        builder.Services.AddTransient<LoginViewModel>();
        builder.Services.AddTransient<RestaurantSelectionViewModel>();
        builder.Services.AddTransient<BranchSelectionViewModel>();
        builder.Services.AddSingleton<POSViewModel>();
        builder.Services.AddSingleton<OrdersViewModel>();
        builder.Services.AddSingleton<KitchenDisplayViewModel>();
        builder.Services.AddSingleton<TablesViewModel>();

        // Register Pages
        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<RestaurantSelectionPage>();
        builder.Services.AddTransient<BranchSelectionPage>();
        builder.Services.AddSingleton<POSPage>();
        builder.Services.AddSingleton<POSPagePhone>();
        builder.Services.AddSingleton<OrdersPage>();
        builder.Services.AddSingleton<KitchenDisplayPage>();
        builder.Services.AddSingleton<TablesPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
