using Trenios.Mobile.ViewModels;

namespace Trenios.Mobile.Pages;

public partial class POSPagePhone : ContentPage
{
    public POSPagePhone(POSViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    private void OnCartTapped(object? sender, EventArgs e)
    {
        PhoneProductsView.IsVisible = false;
        PhoneCartView.IsVisible = true;
        PhoneBottomBar.IsVisible = false;
        PhoneHeader.IsVisible = false;
    }

    private void OnBackToProducts(object? sender, EventArgs e)
    {
        PhoneCartView.IsVisible = false;
        PhoneProductsView.IsVisible = true;
        PhoneHeader.IsVisible = true;
        PhoneBottomBar.ClearValue(VisualElement.IsVisibleProperty);
    }
}
