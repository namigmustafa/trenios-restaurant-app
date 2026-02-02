using Trenios.Mobile.Services;
using Trenios.Mobile.ViewModels;

namespace Trenios.Mobile.Pages;

public partial class MobileMenuPage : ContentPage
{
    public MobileMenuPage(MobileMenuViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        UpdateLanguageLabel();
        LocalizationService.Instance.OnLanguageChanged += UpdateLanguageLabel;
    }

    private void UpdateLanguageLabel()
    {
        var currentLanguage = LocalizationService.Instance.CurrentLanguage;
        CurrentLanguageLabel.Text = currentLanguage.ToUpper();
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
                UpdateLanguageLabel();
            }
        }
    }
}
