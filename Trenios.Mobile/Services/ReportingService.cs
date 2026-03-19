using Trenios.Mobile.Models.Api;

namespace Trenios.Mobile.Services;

public class ReportingService
{
    private readonly ApiService _apiService;

    public ReportingService(ApiService apiService)
    {
        _apiService = apiService;
    }

    public async Task<ApiResult<SalesSummaryReport>> GetSalesSummaryAsync(
        DateTime startDate, DateTime endDate,
        Guid? restaurantId = null, Guid? branchId = null)
    {
        var startStr = startDate.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
        var endStr = endDate.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
        var url = $"/api/reports/sales/summary?startDate={startStr}&endDate={endStr}&compareWithPrevious=true";

        if (restaurantId.HasValue)
            url += $"&restaurantId={restaurantId.Value}";
        if (branchId.HasValue)
            url += $"&branchId={branchId.Value}";

        return await _apiService.GetAsync<SalesSummaryReport>(url);
    }

    public async Task<ApiResult<List<RestaurantDto>>> GetRestaurantsAsync()
    {
        return await _apiService.GetAsync<List<RestaurantDto>>("/api/restaurants");
    }

    public async Task<ApiResult<List<BranchDto>>> GetBranchesAsync(Guid restaurantId)
    {
        return await _apiService.GetAsync<List<BranchDto>>($"/api/branches/restaurant/{restaurantId}");
    }

    public async Task<ApiResult<OrdersSummaryReport>> GetOrdersSummaryAsync(
        DateTime startDate, DateTime endDate,
        Guid? restaurantId = null, Guid? branchId = null)
    {
        var startStr = startDate.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
        var endStr = endDate.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
        var url = $"/api/reports/orders/summary?startDate={startStr}&endDate={endStr}&compareWithPrevious=true";

        if (restaurantId.HasValue)
            url += $"&restaurantId={restaurantId.Value}";
        if (branchId.HasValue)
            url += $"&branchId={branchId.Value}";

        return await _apiService.GetAsync<OrdersSummaryReport>(url);
    }

    public async Task<ApiResult<OrderTypeReport>> GetOrderTypeBreakdownAsync(
        DateTime startDate, DateTime endDate,
        Guid? restaurantId = null, Guid? branchId = null)
    {
        var startStr = startDate.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
        var endStr = endDate.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
        var url = $"/api/reports/sales/by-order-type?startDate={startStr}&endDate={endStr}&compareWithPrevious=true";

        if (restaurantId.HasValue)
            url += $"&restaurantId={restaurantId.Value}";
        if (branchId.HasValue)
            url += $"&branchId={branchId.Value}";

        return await _apiService.GetAsync<OrderTypeReport>(url);
    }

    public async Task<ApiResult<SalesByTimePeriodReport>> GetSalesByTimePeriodAsync(
        DateTime startDate, DateTime endDate,
        Guid? restaurantId = null, Guid? branchId = null, string? timezone = null)
    {
        var startStr = startDate.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
        var endStr = endDate.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
        var url = $"/api/reports/sales/by-time-period?startDate={startStr}&endDate={endStr}&compareWithPrevious=true";

        if (!string.IsNullOrEmpty(timezone))
            url += $"&timezone={Uri.EscapeDataString(timezone)}";
        if (restaurantId.HasValue)
            url += $"&restaurantId={restaurantId.Value}";
        if (branchId.HasValue)
            url += $"&branchId={branchId.Value}";

        return await _apiService.GetAsync<SalesByTimePeriodReport>(url);
    }

    public async Task<ApiResult<TopSellingReport>> GetTopSellingAsync(
        DateTime startDate, DateTime endDate,
        Guid? restaurantId = null, Guid? branchId = null, int top = 10)
    {
        var startStr = startDate.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
        var endStr = endDate.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
        var url = $"/api/reports/menu/top-selling?startDate={startStr}&endDate={endStr}&compareWithPrevious=true&top={top}";

        if (restaurantId.HasValue)
            url += $"&restaurantId={restaurantId.Value}";
        if (branchId.HasValue)
            url += $"&branchId={branchId.Value}";

        return await _apiService.GetAsync<TopSellingReport>(url);
    }

    public async Task<ApiResult<SalesTrendsReport>> GetSalesTrendsAsync(
        DateTime startDate, DateTime endDate,
        Guid? restaurantId = null, Guid? branchId = null, string period = "daily", string? timezone = null)
    {
        var startStr = startDate.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
        var endStr = endDate.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
        var url = $"/api/reports/sales/trends?startDate={startStr}&endDate={endStr}&compareWithPrevious=true&period={period}";

        if (!string.IsNullOrEmpty(timezone))
            url += $"&timezone={Uri.EscapeDataString(timezone)}";
        if (restaurantId.HasValue)
            url += $"&restaurantId={restaurantId.Value}";
        if (branchId.HasValue)
            url += $"&branchId={branchId.Value}";

        return await _apiService.GetAsync<SalesTrendsReport>(url);
    }

    public async Task<ApiResult<SalesByHourReport>> GetSalesByHourAsync(
        DateTime startDate, DateTime endDate,
        Guid? restaurantId = null, Guid? branchId = null, string? timezone = null)
    {
        var startStr = startDate.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
        var endStr = endDate.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
        var url = $"/api/reports/sales/by-hour?startDate={startStr}&endDate={endStr}&compareWithPrevious=true";

        if (!string.IsNullOrEmpty(timezone))
            url += $"&timezone={Uri.EscapeDataString(timezone)}";
        if (restaurantId.HasValue)
            url += $"&restaurantId={restaurantId.Value}";
        if (branchId.HasValue)
            url += $"&branchId={branchId.Value}";

        return await _apiService.GetAsync<SalesByHourReport>(url);
    }
}
