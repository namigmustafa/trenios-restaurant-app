namespace Trenios.Mobile.Pages;

public partial class SplashPage : ContentPage
{
    public SplashPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await RunAnimationsAsync();
    }

    private async Task RunAnimationsAsync()
    {
        // Run all animations together
        await Task.WhenAll(
            AnimateIcons(),
            AnimateLogoText()
        );

        // Navigate to main app
        await NavigateToMainApp();
    }

    private async Task AnimateIcons()
    {
        // Animate icons sliding from outside to their positions
        var icon1Task = Task.WhenAll(
            Icon1.TranslateTo(0, 0, 800, Easing.CubicOut),
            Icon1.FadeTo(1, 800, Easing.CubicOut)
        );

        var icon2Task = Task.WhenAll(
            Icon2.TranslateTo(0, 0, 800, Easing.CubicOut),
            Icon2.FadeTo(1, 800, Easing.CubicOut)
        );

        var icon3Task = Task.WhenAll(
            Icon3.TranslateTo(0, 0, 800, Easing.CubicOut),
            Icon3.FadeTo(1, 800, Easing.CubicOut)
        );

        var icon4Task = Task.WhenAll(
            Icon4.TranslateTo(0, 0, 800, Easing.CubicOut),
            Icon4.FadeTo(1, 800, Easing.CubicOut)
        );

        await Task.WhenAll(icon1Task, icon2Task, icon3Task, icon4Task);

        // Bounce animation after they arrive
        await Task.WhenAll(
            BounceIcon(Icon1),
            BounceIcon(Icon2),
            BounceIcon(Icon3),
            BounceIcon(Icon4)
        );
    }

    private async Task BounceIcon(View icon)
    {
        await icon.TranslateTo(0, -10, 200, Easing.CubicOut);
        await icon.TranslateTo(0, 0, 200, Easing.BounceOut);
    }

    private async Task AnimateLogoText()
    {
        // Fade in and scale up
        await Task.WhenAll(
            LogoImage.FadeTo(1, 800, Easing.CubicOut),
            LogoImage.ScaleTo(1, 800, Easing.SpringOut)
        );

        // Subtle pulse
        await LogoImage.ScaleTo(1.05, 200, Easing.CubicInOut);
        await LogoImage.ScaleTo(1, 200, Easing.CubicInOut);
    }

    private async Task NavigateToMainApp()
    {
        if (Application.Current != null)
        {
            Application.Current.Windows[0].Page = new AppShell();
        }
    }
}
