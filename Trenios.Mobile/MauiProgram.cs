using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Plugin.Maui.Audio;
using Trenios.Mobile.Pages;
using Trenios.Mobile.Services;
using Trenios.Mobile.ViewModels;

namespace Trenios.Mobile;

public static class MauiProgram
{
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
            })
            .ConfigureMauiHandlers(handlers =>
            {
                Microsoft.Maui.Handlers.EntryHandler.Mapper.AppendToMapping("BorderlessEntry", (handler, view) =>
                {
#if ANDROID
                    handler.PlatformView.BackgroundTintList =
                        Android.Content.Res.ColorStateList.ValueOf(Android.Graphics.Color.Transparent);
#elif IOS
                    handler.PlatformView.BorderStyle = UIKit.UITextBorderStyle.None;
#endif
                });
            });

        // Load appsettings
        using var defaultSettings = FileSystem.OpenAppPackageFileAsync("appsettings.json").Result;
        var configBuilder = new ConfigurationBuilder().AddJsonStream(defaultSettings);

#if PROD
        using var prodSettings = FileSystem.OpenAppPackageFileAsync("appsettings.Production.json").Result;
        configBuilder.AddJsonStream(prodSettings);
#endif

        var config = configBuilder.Build();
        builder.Configuration.AddConfiguration(config);

        var apiBaseUrl = config["ApiBaseUrl"]!;

        // Register HttpClient
        builder.Services.AddSingleton(sp =>
        {
            var client = new HttpClient
            {
                BaseAddress = new Uri(apiBaseUrl),
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

        // Audio
        builder.Services.AddSingleton<IAudioManager>(AudioManager.Current);

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
