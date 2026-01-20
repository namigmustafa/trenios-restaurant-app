using Trenios.Mobile.ViewModels;

namespace Trenios.Mobile.Pages;

public partial class BranchSelectionPage : ContentPage
{
    private readonly BranchSelectionViewModel _viewModel;

    public BranchSelectionPage(BranchSelectionViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadBranchesAsync();
    }
}
