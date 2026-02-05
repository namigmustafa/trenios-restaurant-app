using Trenios.Mobile.Models.Api;

namespace Trenios.Mobile.Services;

public class TableService
{
    private readonly ApiService _apiService;
    private List<TableDto>? _cachedTables;
    private Guid? _cachedBranchId;

    public TableService(ApiService apiService)
    {
        _apiService = apiService;
    }

    public async Task<(List<TableDto>? Tables, string? Error)> GetTablesAsync(Guid branchId, bool forceRefresh = false)
    {
        // Return cached tables if available and same branch
        if (!forceRefresh && _cachedTables != null && _cachedBranchId == branchId)
        {
            return (_cachedTables, null);
        }

        var result = await _apiService.GetAsync<List<TableDto>>($"/api/tables/branch/{branchId}");

        if (result.IsSuccess && result.Data != null)
        {
            // Filter only active tables and cache
            _cachedTables = result.Data.Where(t => t.IsActive).ToList();
            _cachedBranchId = branchId;
            return (_cachedTables, null);
        }

        return (null, result.ErrorMessage);
    }

    public void ClearCache()
    {
        _cachedTables = null;
        _cachedBranchId = null;
    }
}
