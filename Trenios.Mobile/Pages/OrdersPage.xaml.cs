using Trenios.Mobile.ViewModels;
using Trenios.Mobile.Services;

namespace Trenios.Mobile.Pages;

public partial class OrdersPage : ContentPage
{
    private readonly OrdersViewModel _viewModel;
    private IDispatcherTimer? _liveTimer;

    public OrdersPage(OrdersViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;

        UpdateLanguageLabels();
        LocalizationService.Instance.OnLanguageChanged += UpdateLanguageLabels;

        // MAUI grouped CollectionView does not re-render when the bound IReadOnlyList reference
        // is replaced — force an ItemsSource cycle on every GroupedOrders change.
        // Also reset on HasSelectedOrder change to close open SwipeViews.
        _viewModel.PropertyChanged += async (s, e) =>
        {
            if (e.PropertyName == nameof(OrdersViewModel.GroupedOrders) ||
                e.PropertyName == nameof(OrdersViewModel.HasSelectedOrder))
            {
                await Dispatcher.DispatchAsync(() =>
                {
                    // Always read from the ViewModel, not from ItemsSource.
                    // Reading ItemsSource after the first cycle returns a stale static
                    // reference (the data binding is broken by setting ItemsSource = null),
                    // so subsequent updates would silently restore the old list.
                    OrdersCollectionView.ItemsSource = null;
                    OrdersCollectionView.ItemsSource = _viewModel.GroupedOrders;
                });
            }
        };
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _ = _viewModel.InitializeAsync();

        _liveTimer = Dispatcher.CreateTimer();
        _liveTimer.Interval = TimeSpan.FromSeconds(1);
        _liveTimer.Tick += (s, e) =>
        {
            var sessions = _viewModel.SelectedOrder?.ActivitySessions;
            if (sessions == null) return;
            foreach (var session in sessions)
                session.Tick();
        };
        _liveTimer.Start();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _liveTimer?.Stop();
        _liveTimer = null;
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
