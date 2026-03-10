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

    /// <summary>
    /// Get tables for POS table selection (available tables only)
    /// </summary>
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

    /// <summary>
    /// Get all tables with their reservation status for Tables page
    /// </summary>
    public async Task<(List<TableWithReservationDto>? Tables, string? Error)> GetTablesWithReservationsAsync(Guid branchId)
    {
        var result = await _apiService.GetAsync<List<TableWithReservationDto>>($"/api/tables?branchId={branchId}");

        if (result.IsSuccess && result.Data != null)
        {
            // Filter only active tables
            var activeTables = result.Data.Where(t => t.IsActive).ToList();
            return (activeTables, null);
        }

        return (null, result.ErrorMessage);
    }

    /// <summary>
    /// Get available (not reserved) tables only
    /// </summary>
    public async Task<(List<TableWithReservationDto>? Tables, string? Error)> GetAvailableTablesAsync(Guid branchId)
    {
        var result = await _apiService.GetAsync<List<TableWithReservationDto>>($"/api/tables/available?branchId={branchId}");

        if (result.IsSuccess && result.Data != null)
        {
            return (result.Data, null);
        }

        return (null, result.ErrorMessage);
    }

    /// <summary>
    /// Get active reservation details for a table
    /// </summary>
    public async Task<(TableReservationDto? Reservation, string? Error)> GetReservationAsync(Guid tableId)
    {
        var result = await _apiService.GetAsync<TableReservationDto>($"/api/tables/{tableId}/reservation");

        if (result.IsSuccess && result.Data != null)
        {
            return (result.Data, null);
        }

        return (null, result.ErrorMessage);
    }

    /// <summary>
    /// Checkout table - completes all orders and releases table
    /// </summary>
    public async Task<(CheckoutTableResponse? Response, string? Error)> CheckoutTableAsync(Guid tableId, string? notes = null)
    {
        var request = new CheckoutTableRequest { Notes = notes };
        var result = await _apiService.PostAsync<CheckoutTableResponse>($"/api/tables/{tableId}/checkout", request);

        if (result.IsSuccess && result.Data != null)
        {
            ClearCache();
            return (result.Data, null);
        }

        return (null, result.ErrorMessage);
    }

    /// <summary>
    /// Move table reservation to another table
    /// </summary>
    public async Task<(TableReservationDto? Reservation, string? Error)> MoveTableAsync(Guid sourceTableId, Guid targetTableId)
    {
        var request = new MoveTableRequest { TargetTableId = targetTableId };
        var result = await _apiService.PostAsync<TableReservationDto>($"/api/tables/{sourceTableId}/move", request);

        if (result.IsSuccess && result.Data != null)
        {
            ClearCache();
            return (result.Data, null);
        }

        return (null, result.ErrorMessage);
    }

    /// <summary>
    /// Release table - cancels all orders and releases table
    /// </summary>
    public async Task<(TableReservationDto? Reservation, string? Error)> ReleaseTableAsync(Guid tableId, string? reason = null)
    {
        var request = new ReleaseTableRequest { Reason = reason };
        var result = await _apiService.PostAsync<TableReservationDto>($"/api/tables/{tableId}/release", request);

        if (result.IsSuccess && result.Data != null)
        {
            ClearCache();
            return (result.Data, null);
        }

        return (null, result.ErrorMessage);
    }

    public void ClearCache()
    {
        _cachedTables = null;
        _cachedBranchId = null;
    }
}
