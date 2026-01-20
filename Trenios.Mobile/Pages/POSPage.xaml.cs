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
            PhoneBottomBar.IsVisible = true;
        }
    }

    private void OnCartTapped(object? sender, TappedEventArgs e)
    {
        // Show cart view on phone
        PhoneProductsView.IsVisible = false;
        PhoneCartView.IsVisible = true;
        PhoneBottomBar.IsVisible = false;
    }

    private void OnBackToProducts(object? sender, EventArgs e)
    {
        // Show products view on phone
        PhoneCartView.IsVisible = false;
        PhoneProductsView.IsVisible = true;
        PhoneBottomBar.IsVisible = true;
    }

    private void OnUserTapped(object? sender, TappedEventArgs e)
    {
        // Toggle user dropdown menu visibility
        if (TabletLayout.IsVisible)
        {
            UserMenuTablet.IsVisible = !UserMenuTablet.IsVisible;
        }
        else
        {
            UserMenuPhone.IsVisible = !UserMenuPhone.IsVisible;
        }
    }

    private async void OnLanguageTapped(object? sender, EventArgs e)
    {
        // Hide dropdowns
        UserMenuTablet.IsVisible = false;
        UserMenuPhone.IsVisible = false;

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
            }
        }
    }
}
