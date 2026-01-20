using Trenios.Mobile.ViewModels;

namespace Trenios.Mobile.Pages;

public partial class RestaurantSelectionPage : ContentPage
{
    private readonly RestaurantSelectionViewModel _viewModel;

    public RestaurantSelectionPage(RestaurantSelectionViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadRestaurantsAsync();
    }
}
