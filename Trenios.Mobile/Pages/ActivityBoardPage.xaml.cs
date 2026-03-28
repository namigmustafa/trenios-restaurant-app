using Trenios.Mobile.ViewModels;
using Trenios.Mobile.Services;

namespace Trenios.Mobile.Pages;

public partial class ActivityBoardPage : ContentPage
{
    private readonly ActivityBoardViewModel _viewModel;

    public ActivityBoardPage(ActivityBoardViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;

        UpdateLanguageLabels();
        LocalizationService.Instance.OnLanguageChanged += UpdateLanguageLabels;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _ = _viewModel.LoadBoardAsync();
        _viewModel.StartElapsedTimer(Dispatcher);
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _viewModel.StopElapsedTimer();
    }

    private void UpdateLanguageLabels()
    {
        var lang = LocalizationService.Instance.CurrentLanguage.ToUpper();
        CurrentLanguageLabel.Text = lang;
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
