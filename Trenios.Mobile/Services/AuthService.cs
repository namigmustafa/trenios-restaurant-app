using Trenios.Mobile.Helpers;
using Trenios.Mobile.Models.Api;

namespace Trenios.Mobile.Services;

public class AuthService
{
    private readonly ApiService _apiService;
    private const string TokenKey = "auth_token";
    private const string TokenExpiryKey = "auth_token_expiry";
    private const string SelectedRestaurantKey = "selected_restaurant_id";
    private const string SelectedRestaurantNameKey = "selected_restaurant_name";
    private const string SelectedBranchKey = "selected_branch_id";
    private const string SelectedBranchNameKey = "selected_branch_name";

    public UserDto? CurrentUser { get; private set; }
    public Guid? SelectedRestaurantId { get; private set; }
    public string? SelectedRestaurantName { get; private set; }
    public RestaurantDto? SelectedRestaurant { get; private set; }
    public Guid? SelectedBranchId { get; private set; }
    public string? SelectedBranchName { get; private set; }
    public BranchDto? CurrentBranch { get; private set; }

    public bool IsLoggedIn => CurrentUser != null && _apiService.HasToken;

    public event Action? OnAuthStateChanged;

    public AuthService(ApiService apiService)
    {
        _apiService = apiService;
    }

    /// <summary>
    /// Gets the effective branch ID based on user role.
    /// BranchManager/Cashier: from user's assigned branch
    /// SuperAdmin/RestaurantOwner: from manually selected branch
    /// </summary>
    public Guid? GetEffectiveBranchId()
    {
        // BranchManager/Cashier have branch assigned
        if (CurrentUser?.CanGoDirectlyToPOS == true)
            return CurrentUser.EffectiveBranchId;

        // SuperAdmin/RestaurantOwner select branch manually
        return SelectedBranchId;
    }

    /// <summary>
    /// Gets the effective restaurant ID based on user role.
    /// SuperAdmin: from SelectedRestaurantId (after selection)
    /// RestaurantOwner: from user's restaurant
    /// BranchManager/Cashier: from user's restaurant
    /// </summary>
    public Guid? GetEffectiveRestaurantId()
    {
        // Check user's restaurant (for RestaurantOwner, BranchManager, Cashier)
        if (CurrentUser?.EffectiveRestaurantId != null)
            return CurrentUser.EffectiveRestaurantId;

        // Check manually selected restaurant (for SuperAdmin)
        return SelectedRestaurantId;
    }

    /// <summary>
    /// Gets the effective restaurant name based on user role.
    /// </summary>
    public string? GetEffectiveRestaurantName()
    {
        // Check user's restaurant name
        if (CurrentUser?.Restaurant != null)
            return CurrentUser.Restaurant.Name;

        // Check manually selected restaurant name (for SuperAdmin)
        return SelectedRestaurantName;
    }

    public void SetSelectedRestaurant(Guid restaurantId, string? restaurantName = null, RestaurantDto? restaurant = null)
    {
        SelectedRestaurantId = restaurantId;
        SelectedRestaurantName = restaurantName;
        SelectedRestaurant = restaurant;
        Preferences.Set(SelectedRestaurantKey, restaurantId.ToString());
        if (restaurantName != null)
            Preferences.Set(SelectedRestaurantNameKey, restaurantName);
    }

    public void SetSelectedBranch(Guid branchId, string? branchName = null, BranchDto? branchDto = null)
    {
        SelectedBranchId = branchId;
        SelectedBranchName = branchName;
        CurrentBranch = branchDto;
        Preferences.Set(SelectedBranchKey, branchId.ToString());
        if (branchName != null)
            Preferences.Set(SelectedBranchNameKey, branchName);
        OnAuthStateChanged?.Invoke();
    }

    public BranchDto? GetCurrentBranch() => CurrentBranch;

    public bool IsActivityEnabled => CurrentBranch?.IsActivityEnabled == true;

    /// <summary>
    /// Gets the effective branch name based on user role.
    /// </summary>
    public string? GetEffectiveBranchName()
    {
        // Check user's branch name
        if (CurrentUser?.Branch != null)
            return CurrentUser.Branch.Name;

        // Check manually selected branch name (for SuperAdmin/RestaurantOwner)
        return SelectedBranchName;
    }

    public async Task<(bool Success, string? ErrorMessage)> LoginAsync(string username, string password)
    {
        var request = new LoginRequest
        {
            Username = username,
            Password = password
        };

        var result = await _apiService.PostAsync<LoginResponse>("/api/auth/login", request);

        if (!result.IsSuccess)
        {
            return (false, "Invalid username or password.");
        }

        if (result.Data == null)
        {
            return (false, "Invalid response from server");
        }

        // Store token
        _apiService.SetToken(result.Data.Token);
        CurrentUser = result.Data.User;

        // Save to secure storage
        await SecureStorage.SetAsync(TokenKey, result.Data.Token);
        await SecureStorage.SetAsync(TokenExpiryKey, result.Data.ExpiresAt.ToString("O"));

        // Clear previous selections when logging in as different user
        ClearSelections();

        // Set currency for any user with a known restaurant (SuperAdmin has none at login — set later in RestaurantSelectionViewModel)
        CurrencyFormatter.Current = CurrentUser.Restaurant?.Currency ?? Currency.EUR;

        // For BranchManager/Cashier, pre-populate branch from JWT so POSViewModel works immediately
        if (CurrentUser.CanGoDirectlyToPOS)
        {
            CurrentBranch = CurrentUser.Branch;
            SelectedBranchId = CurrentUser.EffectiveBranchId;
            SelectedRestaurant = CurrentUser.Restaurant;
        }

        OnAuthStateChanged?.Invoke();

        return (true, null);
    }

    public async Task LogoutAsync()
    {
        CurrentUser = null;
        _apiService.ClearToken();

        SecureStorage.Remove(TokenKey);
        SecureStorage.Remove(TokenExpiryKey);

        ClearSelections();

        OnAuthStateChanged?.Invoke();

        await Task.CompletedTask;
    }

    private void ClearSelections()
    {
        SelectedRestaurantId = null;
        SelectedRestaurantName = null;
        SelectedRestaurant = null;
        SelectedBranchId = null;
        SelectedBranchName = null;
        CurrentBranch = null;
        Preferences.Remove(SelectedRestaurantKey);
        Preferences.Remove(SelectedRestaurantNameKey);
        Preferences.Remove(SelectedBranchKey);
        Preferences.Remove(SelectedBranchNameKey);
    }

    public async Task<bool> TryRestoreSessionAsync()
    {
        try
        {
            var token = await SecureStorage.GetAsync(TokenKey);
            var expiryStr = await SecureStorage.GetAsync(TokenExpiryKey);

            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(expiryStr))
            {
                return false;
            }

            if (DateTime.TryParse(expiryStr, out var expiry) && expiry <= DateTime.UtcNow)
            {
                await LogoutAsync();
                return false;
            }

            _apiService.SetToken(token);

            // Restore selections
            var restaurantStr = Preferences.Get(SelectedRestaurantKey, string.Empty);
            var branchStr = Preferences.Get(SelectedBranchKey, string.Empty);
            SelectedRestaurantName = Preferences.Get(SelectedRestaurantNameKey, string.Empty);
            SelectedBranchName = Preferences.Get(SelectedBranchNameKey, string.Empty);

            if (Guid.TryParse(restaurantStr, out var restaurantId))
                SelectedRestaurantId = restaurantId;

            if (Guid.TryParse(branchStr, out var branchId))
                SelectedBranchId = branchId;

            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Determines the navigation route after login based on user role.
    /// </summary>
    public string GetPostLoginRoute()
    {
        if (CurrentUser == null)
            return "//LoginPage";

        if (CurrentUser.NeedsRestaurantSelection)
            return "//RestaurantSelection";

        if (CurrentUser.NeedsBranchSelection)
            return "//BranchSelection";

        return "//MainTabs";
    }
}
