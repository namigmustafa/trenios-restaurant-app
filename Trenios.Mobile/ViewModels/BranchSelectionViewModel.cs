using System.Collections.ObjectModel;
using System.Windows.Input;
using Trenios.Mobile.Models.Api;
using Trenios.Mobile.Services;
using Trenios.Mobile;

namespace Trenios.Mobile.ViewModels;

public class BranchSelectionViewModel : BaseViewModel
{
    private readonly SelectionService _selectionService;
    private readonly AuthService _authService;

    private string _errorMessage = string.Empty;
    private bool _hasError;
    private string _restaurantName = string.Empty;
    private bool _isNavigating;

    public ObservableCollection<BranchDto> Branches { get; } = new();

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

    public string RestaurantName
    {
        get => _restaurantName;
        set => SetProperty(ref _restaurantName, value);
    }

    public string? RestaurantImageUrl =>
        _authService.SelectedRestaurant?.DisplayImageUrl
        ?? _authService.CurrentUser?.Restaurant?.LogoUrl
        ?? _authService.CurrentUser?.Restaurant?.ImageUrl;

    public bool IsNavigating
    {
        get => _isNavigating;
        set => SetProperty(ref _isNavigating, value);
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

    public bool CanGoBack => _authService.CurrentUser?.NeedsRestaurantSelection == true;

    public ICommand SelectBranchCommand { get; }
    public ICommand BackCommand { get; }
    public ICommand LogoutCommand { get; }
    public ICommand RefreshCommand { get; }

    public BranchSelectionViewModel(SelectionService selectionService, AuthService authService)
    {
        _selectionService = selectionService;
        _authService = authService;

        SelectBranchCommand = new Command<BranchDto>(async (branch) => await SelectBranchAsync(branch));
        BackCommand = new Command(async () => await GoBackAsync());
        LogoutCommand = new Command(async () => await LogoutAsync());
        RefreshCommand = new Command(async () => await LoadBranchesAsync());

        // Subscribe to language changes to update welcome message
        LocalizationService.Instance.OnLanguageChanged += () => OnPropertyChanged(nameof(WelcomeMessage));
    }

    public async Task LoadBranchesAsync()
    {
        if (IsBusy) return;

        var restaurantId = _authService.GetEffectiveRestaurantId();
        if (restaurantId == null)
        {
            ErrorMessage = "No restaurant selected";
            HasError = true;
            return;
        }

        // Set restaurant name
        var name = _authService.GetEffectiveRestaurantName() ?? string.Empty;
        RestaurantName = name.Length > 31 ? name[..31] + "..." : name;

        try
        {
            IsBusy = true;
            HasError = false;

            var (branches, error) = await _selectionService.GetBranchesAsync(restaurantId.Value);

            if (branches != null)
            {
                Branches.Clear();
                foreach (var branch in branches)
                {
                    Branches.Add(branch);
                }
            }
            else
            {
                ErrorMessage = error ?? "Failed to load branches";
                HasError = true;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = "Connection error. Please try again.";
            HasError = true;
            System.Diagnostics.Debug.WriteLine($"Load branches error: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task SelectBranchAsync(BranchDto branch)
    {
        if (branch == null || IsNavigating) return;

        IsNavigating = true;
        _authService.SetSelectedBranch(branch.Id, branch.Name, branch);

        await Shell.Current.GoToAsync("//MainTabs");

        if (Shell.Current is AppShell appShell)
            appShell.ApplyBranchSettings(_authService);
    }

    private async Task GoBackAsync()
    {
        if (CanGoBack)
        {
            await Shell.Current.GoToAsync("//RestaurantSelection");
        }
    }

    private async Task LogoutAsync()
    {
        await _authService.LogoutAsync();
        await Shell.Current.GoToAsync("//LoginPage");
    }
}
