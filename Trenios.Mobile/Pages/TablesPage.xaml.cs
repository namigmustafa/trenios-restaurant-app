using Trenios.Mobile.ViewModels;

namespace Trenios.Mobile.Pages;

public partial class TablesPage : ContentPage
{
    private readonly TablesViewModel _viewModel;

    public TablesPage(TablesViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;

        // Listen for HasSelectedTable changes to force CollectionView layout refresh
        _viewModel.PropertyChanged += async (s, e) =>
        {
            if (e.PropertyName == nameof(TablesViewModel.HasSelectedTable))
            {
                // Force complete refresh of CollectionView items
                await Dispatcher.DispatchAsync(() =>
                {
                    var items = TablesCollectionView.ItemsSource;
                    TablesCollectionView.ItemsSource = null;
                    TablesCollectionView.ItemsSource = items;
                });
            }
        };
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        // Fire-and-forget: page appears immediately, data loads in background
        _ = _viewModel.LoadTablesAsync();
    }
}
