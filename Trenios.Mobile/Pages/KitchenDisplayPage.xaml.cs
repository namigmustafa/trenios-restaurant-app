using Trenios.Mobile.ViewModels;

namespace Trenios.Mobile.Pages;

public partial class KitchenDisplayPage : ContentPage
{
    private readonly KitchenDisplayViewModel _viewModel;

    public KitchenDisplayPage(KitchenDisplayViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        // Fire-and-forget: page appears immediately, data loads in background
        _ = _viewModel.InitializeAsync();
    }

    protected override async void OnDisappearing()
    {
        base.OnDisappearing();
        await _viewModel.DisconnectAsync();
    }
}
