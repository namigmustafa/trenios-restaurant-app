using System.Windows.Input;
using Trenios.Mobile.Services;

namespace Trenios.Mobile.ViewModels;

public class MobileMenuViewModel : BaseViewModel
{
    private readonly AuthService _authService;
    private bool _isNavigating;

    public string UserDisplayName => _authService.CurrentUser?.FullName ?? "User";
    public string BranchName => _authService.GetEffectiveBranchName() ?? "Branch";
    public string RestaurantName => _authService.GetEffectiveRestaurantName() ?? "Restaurant";

    public bool IsNavigating
    {
        get => _isNavigating;
        set => SetProperty(ref _isNavigating, value);
    }

    public ICommand CreateOrderCommand { get; }
    public ICommand ShowOrdersCommand { get; }
    public ICommand KitchenCommand { get; }
    public ICommand LogoutCommand { get; }
    public ICommand BackCommand { get; }

    public bool CanGoBack => _authService.CurrentUser?.NeedsBranchSelection == true;

    public MobileMenuViewModel(AuthService authService)
    {
        _authService = authService;

        CreateOrderCommand = new Command(() => NavigateToAsync("//POSPhone"));
        ShowOrdersCommand = new Command(() => NavigateToAsync("orders"));
        KitchenCommand = new Command(() => NavigateToAsync("kitchen"));
        LogoutCommand = new Command(() => NavigateWithSpinner(async () =>
        {
            await _authService.LogoutAsync();
            await Shell.Current.GoToAsync("//LoginPage");
        }));
        BackCommand = new Command(() => NavigateWithSpinner(async () =>
        {
            if (CanGoBack)
            {
                await Shell.Current.GoToAsync("//BranchSelection");
            }
        }));
    }

    private async void NavigateToAsync(string route)
    {
        if (IsNavigating) return;
        IsNavigating = true;

        // Navigate immediately - destination page handles its own loading
        await Shell.Current.GoToAsync(route);
        IsNavigating = false;
    }

    private async void NavigateWithSpinner(Func<Task> action)
    {
        if (IsNavigating) return;
        IsNavigating = true;

        await action();
        IsNavigating = false;
    }
}
