using Trenios.Mobile.Models.Api;

namespace Trenios.Mobile.Services;

public class SelectionService
{
    private readonly ApiService _apiService;

    public SelectionService(ApiService apiService)
    {
        _apiService = apiService;
    }

    public async Task<(List<RestaurantDto>? Restaurants, string? Error)> GetRestaurantsAsync()
    {
        var result = await _apiService.GetAsync<List<RestaurantDto>>("/api/restaurants");

        if (result.IsSuccess && result.Data != null)
        {
            var active = result.Data.Where(r => r.IsActive).ToList();
            return (active, null);
        }

        return (null, result.ErrorMessage);
    }

    public async Task<(List<BranchDto>? Branches, string? Error)> GetBranchesAsync(Guid restaurantId)
    {
        var result = await _apiService.GetAsync<List<BranchDto>>($"/api/branches/restaurant/{restaurantId}");

        if (result.IsSuccess && result.Data != null)
        {
            var active = result.Data.Where(b => b.IsActive).ToList();
            return (active, null);
        }

        return (null, result.ErrorMessage);
    }
}
