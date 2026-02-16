using Trenios.Mobile.ViewModels;
using Trenios.Mobile.Services;
using Trenios.Mobile.Models.Api;

namespace Trenios.Mobile.Pages;

public partial class POSPagePhone : ContentPage
{
    private readonly POSViewModel _viewModel;
    private readonly OrderService _orderService;
    private Border? _selectedTableBorder;

    public POSPagePhone(POSViewModel viewModel, OrderService orderService)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _orderService = orderService;
        BindingContext = viewModel;

        // Subscribe to cart changes for button visibility
        _orderService.OnCartChanged += OnCartChanged;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Force layout refresh on iOS so categories ScrollView sizes correctly
        if (DeviceInfo.Platform == DevicePlatform.iOS)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                PhoneProductsView.InvalidateMeasure();
            });
        }
    }

    private void OnCartChanged()
    {
        // On iOS, the binding doesn't always update properly, so manually refresh button visibility
        MainThread.BeginInvokeOnMainThread(() =>
        {
            System.Diagnostics.Debug.WriteLine($"[POSPagePhone.OnCartChanged] Cart changed - HasItems: {_viewModel.HasItems}, TotalItems: {_viewModel.TotalItems}");
            PhoneBottomBar.IsVisible = _viewModel.HasItems;
        });
    }

    private async void OnCartTapped(object? sender, EventArgs e)
    {
        // Switch views first
        PhoneProductsView.IsVisible = false;
        PhoneBottomBar.IsVisible = false;
        PhoneHeader.IsVisible = false;
        PhoneCartView.IsVisible = true;

        if (DeviceInfo.Platform == DevicePlatform.iOS)
        {
            // Wait for UICollectionView to layout with non-zero frame before populating
            await Task.Delay(100);
        }

        // Now populate cart items - CollectionView is visible and laid out
        var tempItems = _orderService.CartItems.ToList();
        _viewModel.CartItems.Clear();
        foreach (var item in tempItems)
        {
            _viewModel.CartItems.Add(item);
        }
    }

    private void OnTableTapped(object? sender, EventArgs e)
    {
        if (sender is not Border tappedBorder) return;
        if (tappedBorder.BindingContext is not TableDto table) return;

        var primary = (Color)Application.Current!.Resources["Primary"];
        var defaultStroke = (Color)Application.Current.Resources["Gray200"];
        var defaultBg = (Color)Application.Current.Resources["White"];

        // Deselect previous
        if (_selectedTableBorder != null)
        {
            _selectedTableBorder.BackgroundColor = defaultBg;
            _selectedTableBorder.Stroke = new SolidColorBrush(defaultStroke);
        }

        // Highlight tapped
        tappedBorder.BackgroundColor = primary;
        tappedBorder.Stroke = new SolidColorBrush(primary);
        _selectedTableBorder = tappedBorder;

        // Update ViewModel
        _viewModel.SelectedTable = table;
    }

    private void OnBackToProducts(object? sender, EventArgs e)
    {
        PhoneCartView.IsVisible = false;
        PhoneProductsView.IsVisible = true;
        PhoneHeader.IsVisible = true;
        PhoneBottomBar.ClearValue(VisualElement.IsVisibleProperty);
    }
}
