using Trenios.Mobile.ViewModels;
using Trenios.Mobile.Services;

namespace Trenios.Mobile.Pages;

public partial class OrdersPage : ContentPage
{
    private readonly OrdersViewModel _viewModel;

    public OrdersPage(OrdersViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;

        UpdateLanguageLabels();
        LocalizationService.Instance.OnLanguageChanged += UpdateLanguageLabels;

        // On tablet, force CollectionView to re-layout when the side panel appears/disappears
        // (column span changes from 2 to 1). GroupedOrders replacement already closes open SwipeViews.
        _viewModel.PropertyChanged += async (s, e) =>
        {
            if (e.PropertyName == nameof(OrdersViewModel.HasSelectedOrder))
            {
                await Dispatcher.DispatchAsync(() =>
                {
                    var items = OrdersCollectionView.ItemsSource;
                    OrdersCollectionView.ItemsSource = null;
                    OrdersCollectionView.ItemsSource = items;
                });
            }
        };
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        // Fire-and-forget: page appears immediately, data loads in background
        _ = _viewModel.LoadOrdersAsync();
    }

    private void UpdateLanguageLabels()
    {
        var lang = LocalizationService.Instance.CurrentLanguage.ToUpper();
        CurrentLanguageLabel.Text = lang;
        DetailsCurrentLanguageLabel.Text = lang;
    }

    private void OnLogoutTapped(object? sender, EventArgs e)
    {
        if (_viewModel.LogoutCommand is Command command && command.CanExecute(null))
            command.Execute(null);
    }

    private async void OnLanguageTapped(object? sender, EventArgs e)
    {
        var localization = LocalizationService.Instance;
        var languages = localization.AvailableLanguages;
        var options = languages.Select(l => l.Name).ToArray();

        var result = await DisplayActionSheet("Select Language", "Cancel", null, options);

        if (result != null && result != "Cancel")
        {
            var selected = languages.FirstOrDefault(l => l.Name == result);
            if (selected != default)
            {
                localization.SetLanguage(selected.Code);
                UpdateLanguageLabels();
            }
        }
    }
}
