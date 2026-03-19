using Trenios.Mobile.Models.Api;
using Trenios.Mobile.ViewModels;
using Trenios.Mobile.Services;

namespace Trenios.Mobile.Pages;

public partial class TablesPage : ContentPage
{
    private readonly TablesViewModel _viewModel;

    public TablesPage(TablesViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;

        UpdateLanguageLabels();
        LocalizationService.Instance.OnLanguageChanged += UpdateLanguageLabels;

        _viewModel.PropertyChanged += async (s, e) =>
        {
            if (e.PropertyName == nameof(TablesViewModel.HasSelectedTable))
            {
                await Dispatcher.DispatchAsync(() =>
                {
                    var items = TablesCollectionView.ItemsSource;
                    TablesCollectionView.ItemsSource = null;
                    TablesCollectionView.ItemsSource = items;
                });
            }
            else if (e.PropertyName == nameof(TablesViewModel.ShowMoveTableDialog) && !_viewModel.ShowMoveTableDialog)
            {
                MoveTargetCollectionView.SelectedItem = null;
            }
        };
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _ = _viewModel.LoadTablesAsync();
    }

    private void UpdateLanguageLabels()
    {
        var lang = LocalizationService.Instance.CurrentLanguage.ToUpper();
        CurrentLanguageLabel.Text = lang;
        if (DetailsCurrentLanguageLabel != null)
            DetailsCurrentLanguageLabel.Text = lang;
    }

    private void OnMoveTargetSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        var selected = e.CurrentSelection.FirstOrDefault() as TableWithReservationDto;
        _viewModel.SelectTargetTableCommand.Execute(selected);
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
