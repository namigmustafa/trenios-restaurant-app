using System.Collections.ObjectModel;
using System.Windows.Input;
using Trenios.Mobile.Models.Api;
using Trenios.Mobile.Services;

namespace Trenios.Mobile.ViewModels;

public class RestaurantSelectionViewModel : BaseViewModel
{
    private readonly SelectionService _selectionService;
    private readonly AuthService _authService;
    private readonly ProductService _productService;

    private string _errorMessage = string.Empty;
    private bool _hasError;

    public ObservableCollection<RestaurantDto> Restaurants { get; } = new();

    public string ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public bool HasError
    {
        get => _hasError;
        set => SetProperty(ref _hasError, value);
    }

    public string UserDisplayName => _authService.CurrentUser?.FullName ?? "User";

    public string WelcomeMessage => $"{LocalizationService.Instance["Welcome"]}, {UserDisplayName}";

    public string UserRoleName => _authService.CurrentUser?.UserRole switch
    {
        UserRole.SuperAdmin => "Super Admin",
        UserRole.RestaurantOwner => "Restaurant Owner",
        UserRole.BranchManager => "Branch Manager",
        UserRole.Cashier => "Cashier",
        _ => ""
    };

    public ICommand SelectRestaurantCommand { get; }
    public ICommand LogoutCommand { get; }
    public ICommand RefreshCommand { get; }

    public RestaurantSelectionViewModel(SelectionService selectionService, AuthService authService, ProductService productService)
    {
        _selectionService = selectionService;
        _authService = authService;
        _productService = productService;

        SelectRestaurantCommand = new Command<RestaurantDto>(async (restaurant) => await SelectRestaurantAsync(restaurant));
        LogoutCommand = new Command(async () => await LogoutAsync());
        RefreshCommand = new Command(async () => await LoadRestaurantsAsync());

        // Subscribe to language changes to update welcome message
        LocalizationService.Instance.OnLanguageChanged += () => OnPropertyChanged(nameof(WelcomeMessage));
    }

    public async Task LoadRestaurantsAsync()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;
            HasError = false;

            var (restaurants, error) = await _selectionService.GetRestaurantsAsync();

            if (restaurants != null)
            {
                Restaurants.Clear();
                foreach (var restaurant in restaurants)
                {
                    Restaurants.Add(restaurant);
                }
            }
            else
            {
                ErrorMessage = error ?? "Failed to load restaurants";
                HasError = true;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = "Connection error. Please try again.";
            HasError = true;
            System.Diagnostics.Debug.WriteLine($"Load restaurants error: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task SelectRestaurantAsync(RestaurantDto restaurant)
    {
        if (restaurant == null) return;

        _productService.ClearCache();
        _authService.SetSelectedRestaurant(restaurant.Id, restaurant.Name, restaurant);
        await Shell.Current.GoToAsync("//BranchSelection");
    }

    private async Task LogoutAsync()
    {
        await _authService.LogoutAsync();
        await Shell.Current.GoToAsync("//LoginPage");
    }
}
