using Trenios.Mobile.Services;
using Trenios.Mobile.ViewModels;

namespace Trenios.Mobile.Pages;

public partial class LoginPage : ContentPage
{
    public LoginPage(LoginViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;

        // Set initial language display
        UpdateLanguageDisplay();
    }

    private void UpdateLanguageDisplay()
    {
        CurrentLanguageLabel.Text = LocalizationService.Instance.CurrentLanguage switch
        {
            "en" => "English",
            "az" => "Azərbaycan",
            "ru" => "Русский",
            "tr" => "Türkçe",
            "lv" => "Latviešu",
            _ => "English"
        };
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
}
