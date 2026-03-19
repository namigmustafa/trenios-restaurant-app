using Trenios.Mobile.Services;
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
        UpdateLanguageDisplay();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        UpdateLanguageDisplay();
        await _viewModel.LoadBranchesAsync();
    }

    private void UpdateLanguageDisplay()
    {
        CurrentLanguageLabelHeader.Text = LocalizationService.Instance.CurrentLanguage.ToUpper();
    }

    private void OnLanguageTapped(object? sender, EventArgs e)
    {
        LanguagePopup.IsVisible = true;
    }

    private void OnCloseLanguagePopup(object? sender, EventArgs e)
    {
        LanguagePopup.IsVisible = false;
    }

    private void OnLanguageSelected(object? sender, EventArgs e)
    {
        if (sender is Button button && !string.IsNullOrEmpty(button.AutomationId))
        {
            LocalizationService.Instance.SetLanguage(button.AutomationId);
            UpdateLanguageDisplay();
            LanguagePopup.IsVisible = false;
        }
    }

    private async void OnLogoutTapped(object? sender, EventArgs e)
    {
        var yes = LocalizationService.Instance.Get("Yes");
        var no = LocalizationService.Instance.Get("No");
        var message = LocalizationService.Instance.Get("AreYouSureLogout");

        bool confirm = await DisplayAlert("", message, yes, no);
        if (confirm)
            _viewModel.LogoutCommand.Execute(null);
    }
}
