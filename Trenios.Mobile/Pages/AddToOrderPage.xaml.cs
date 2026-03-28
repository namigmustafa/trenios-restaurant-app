using Trenios.Mobile.ViewModels;

namespace Trenios.Mobile.Pages;

public partial class AddToOrderPage : ContentPage
{
    private readonly AddToOrderViewModel _viewModel;
    private bool _isInitialized;

    public AddToOrderPage(AddToOrderViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        ShowProducts();
        if (!_isInitialized)
        {
            _isInitialized = true;
            _ = _viewModel.InitializeAsync();
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _isInitialized = false;
    }

    // Bottom bar tapped: switch to cart view
    private void OnBottomBarTapped(object? sender, EventArgs e)
    {
        ProductsView.IsVisible = false;
        CartView.IsVisible = true;
        BottomBar.IsVisible = false;
        SubmitBar.IsVisible = true;
    }

    // Back button inside cart view: return to products
    private void OnBackToProducts(object? sender, EventArgs e) => ShowProducts();

    private void ShowProducts()
    {
        ProductsView.IsVisible = true;
        CartView.IsVisible = false;
        SubmitBar.IsVisible = false;
        // BottomBar visibility is data-bound to HasItems — no need to set here
    }
}
