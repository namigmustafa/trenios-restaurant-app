using Trenios.Mobile.Models.Api;

namespace Trenios.Mobile.Services;

public class ProductService
{
    private readonly ApiService _apiService;
    private readonly AuthService _authService;

    private List<CategoryDto>? _cachedCategories;
    private List<BranchMenuItemDto>? _cachedMenuItems;
    private Guid? _cachedBranchId;
    private DateTime _categoriesCachedAt;
    private DateTime _menuItemsCachedAt;

    // Cache expires after 1 hour
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromHours(1);

    public ProductService(ApiService apiService, AuthService authService)
    {
        _apiService = apiService;
        _authService = authService;
    }

    private bool IsCategoriesCacheValid => _cachedCategories != null &&
        DateTime.UtcNow - _categoriesCachedAt < CacheExpiration;

    private bool IsMenuItemsCacheValid(Guid branchId) => _cachedMenuItems != null &&
        _cachedBranchId == branchId &&
        DateTime.UtcNow - _menuItemsCachedAt < CacheExpiration;

    public async Task<(List<CategoryDto>? Categories, string? Error)> GetCategoriesAsync(bool forceRefresh = false)
    {
        if (!forceRefresh && IsCategoriesCacheValid)
        {
            return (_cachedCategories!, null);
        }

        var restaurantId = _authService.GetEffectiveRestaurantId();
        if (restaurantId == null)
        {
            return (null, "No restaurant selected");
        }

        var result = await _apiService.GetAsync<List<CategoryDto>>($"/api/categories?restaurantId={restaurantId}");

        if (result.IsSuccess && result.Data != null)
        {
            _cachedCategories = result.Data
                .Where(c => c.IsActive)
                .OrderBy(c => c.DisplayOrder)
                .ToList();
            _categoriesCachedAt = DateTime.UtcNow;
            return (_cachedCategories, null);
        }

        return (null, result.ErrorMessage);
    }

    public async Task<(List<BranchMenuItemDto>? Items, string? Error)> GetMenuItemsAsync(bool forceRefresh = false)
    {
        var branchId = _authService.GetEffectiveBranchId();

        if (branchId == null)
        {
            return (null, "No branch selected");
        }

        // Return cached if same branch, not forcing refresh, and cache is valid
        if (!forceRefresh && IsMenuItemsCacheValid(branchId.Value))
        {
            return (_cachedMenuItems!, null);
        }

        var result = await _apiService.GetAsync<List<BranchMenuItemDto>>($"/api/branchmenuitems/branch/{branchId}");

        if (result.IsSuccess && result.Data != null)
        {
            _cachedMenuItems = result.Data
                .Where(m => m.IsActive && m.MenuItemIsAvailable)
                .ToList();
            _cachedBranchId = branchId;
            _menuItemsCachedAt = DateTime.UtcNow;
            return (_cachedMenuItems, null);
        }

        return (null, result.ErrorMessage);
    }

    public async Task<(List<BranchMenuItemDto>? Items, string? Error)> GetMenuItemsByCategoryAsync(
        Guid categoryId, bool forceRefresh = false)
    {
        var (items, error) = await GetMenuItemsAsync(forceRefresh);

        if (items == null)
        {
            return (null, error);
        }

        var filtered = items.Where(m => m.CategoryId == categoryId).ToList();
        return (filtered, null);
    }

    public BranchMenuItemDto? GetCachedMenuItem(Guid menuItemId)
    {
        return _cachedMenuItems?.FirstOrDefault(m => m.MenuItemId == menuItemId);
    }

    public void ClearCache()
    {
        _cachedCategories = null;
        _cachedMenuItems = null;
        _cachedBranchId = null;
    }
}
