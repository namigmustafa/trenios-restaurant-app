using Trenios.Mobile.ViewModels;

namespace Trenios.Mobile.Pages;

public partial class OrdersPage : ContentPage
{
    private readonly OrdersViewModel _viewModel;

    public OrdersPage(OrdersViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;

        // Listen for HasSelectedOrder changes to force CollectionView layout refresh
        _viewModel.PropertyChanged += async (s, e) =>
        {
            if (e.PropertyName == nameof(OrdersViewModel.HasSelectedOrder))
            {
                // Force complete refresh of CollectionView items
                await Dispatcher.DispatchAsync(() =>
                {
                    var items = OrdersCollectionView.ItemsSource;
                    OrdersCollectionView.ItemsSource = null;
                    OrdersCollectionView.ItemsSource = items;
                });
            }
            else if (e.PropertyName == nameof(OrdersViewModel.Orders))
            {
                // When orders list changes (after cancel/complete), force refresh to close swipes
                await Dispatcher.DispatchAsync(() =>
                {
                    var items = OrdersCollectionView.ItemsSource;
                    OrdersCollectionView.ItemsSource = null;
                    OrdersCollectionView.ItemsSource = items;
                });
            }
        };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadOrdersAsync();
    }
}
