using Microsoft.Extensions.DependencyInjection;
using Trenios.Mobile.Pages;
using Trenios.Mobile.Services;

namespace Trenios.Mobile
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            // Set device-appropriate page for Create Order tab
            var services = IPlatformApplication.Current?.Services;
            if (services != null)
            {
                CreateOrderShellContent.ContentTemplate = DeviceInfo.Idiom == DeviceIdiom.Phone
                    ? new DataTemplate(() => services.GetRequiredService<POSPagePhone>())
                    : new DataTemplate(() => services.GetRequiredService<POSPage>());
            }

            Routing.RegisterRoute(nameof(OrderBreakdownPage), typeof(OrderBreakdownPage));
            Routing.RegisterRoute(nameof(AddToOrderPage), typeof(AddToOrderPage));
        }

        /// <summary>
        /// Call this after branch is set to show/hide activity-dependent tabs.
        /// </summary>
        public void ApplyBranchSettings(AuthService authService)
        {
            ActivitiesTab.IsVisible = authService.IsActivityEnabled;
        }
    }
}
