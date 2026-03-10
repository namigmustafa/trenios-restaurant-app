using Trenios.Mobile.Pages;

namespace Trenios.Mobile
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            // Register routes
            Routing.RegisterRoute("orders", typeof(OrdersPage));
            Routing.RegisterRoute("kitchen", typeof(KitchenDisplayPage));
            Routing.RegisterRoute("tables", typeof(TablesPage));
        }
    }
}
