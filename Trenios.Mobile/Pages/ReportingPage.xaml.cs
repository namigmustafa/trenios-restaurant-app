using Trenios.Mobile.ViewModels;
using Trenios.Mobile.Services;

namespace Trenios.Mobile.Pages;

public partial class ReportingPage : ContentPage
{
    private readonly ReportingViewModel _viewModel;

    public ReportingPage(ReportingViewModel viewModel)
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
        if (_viewModel.IsAuthorized && !_viewModel.HasData && !_viewModel.IsLoading)
            _ = _viewModel.LoadReportAsync();
    }

    private void UpdateLanguageLabels()
    {
        LanguageLabel.Text = LocalizationService.Instance.CurrentLanguage.ToUpper();
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
