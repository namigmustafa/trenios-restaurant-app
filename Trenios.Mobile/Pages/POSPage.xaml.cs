using Trenios.Mobile.Services;
using Trenios.Mobile.ViewModels;

namespace Trenios.Mobile.Pages;

public partial class POSPage : ContentPage
{
    private const double TabletBreakpoint = 768;

    public POSPage(POSViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        UpdateLanguageLabel();
        LocalizationService.Instance.OnLanguageChanged += UpdateLanguageLabel;
    }

    private void UpdateLanguageLabel()
    {
        var currentLanguage = LocalizationService.Instance.CurrentLanguage;
        var languageCode = currentLanguage.ToUpper();

        // Update labels
        CurrentLanguageLabelTablet.Text = languageCode;
        CurrentLanguageLabelPhone.Text = languageCode;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is POSViewModel viewModel)
            await viewModel.LoadDataAsync(forceRefresh: false);
    }

    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);
        UpdateLayout(width);
    }

    private void UpdateLayout(double width)
    {
        // Switch between tablet and phone layouts based on width
        bool isTablet = width >= TabletBreakpoint;

        TabletLayout.IsVisible = isTablet;
        PhoneLayout.IsVisible = !isTablet;

        // Reset phone cart view when switching to tablet
        if (isTablet)
        {
            PhoneCartView.IsVisible = false;
            PhoneProductsView.IsVisible = true;
            PhoneHeader.IsVisible = true;
        }
    }

    private void OnCartTapped(object? sender, EventArgs e)
    {
        // Show cart view on phone
        PhoneProductsView.IsVisible = false;
        PhoneCartView.IsVisible = true;
        PhoneBottomBar.IsVisible = false;
        PhoneHeader.IsVisible = false;
    }

    private void OnBackToProducts(object? sender, EventArgs e)
    {
        // Show products view on phone
        PhoneCartView.IsVisible = false;
        PhoneProductsView.IsVisible = true;
        PhoneHeader.IsVisible = true;

        // Restore the binding by clearing the local value
        PhoneBottomBar.ClearValue(VisualElement.IsVisibleProperty);
    }

    private async void OnUserTapped(object? sender, TappedEventArgs e)
    {
        // Show logout popup
        var result = await DisplayActionSheet("User Menu", "Cancel", "Logout");

        if (result == "Logout" && BindingContext is POSViewModel viewModel)
        {
            if (viewModel.LogoutCommand is Command command && command.CanExecute(null))
            {
                command.Execute(null);
            }
        }
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
