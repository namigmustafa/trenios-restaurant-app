using Trenios.Mobile.Models.Api;

namespace Trenios.Mobile.Services;

public class ActivityService
{
    private readonly ApiService _apiService;

    public ActivityService(ApiService apiService)
    {
        _apiService = apiService;
    }

    // Activity Types
    public async Task<(List<ActivityTypeDto>? Types, string? Error)> GetActivityTypesAsync()
    {
        var result = await _apiService.GetAsync<List<ActivityTypeDto>>("/api/activity-types");
        return result.IsSuccess ? (result.Data, null) : (null, result.ErrorMessage);
    }

    public async Task<(ActivityTypeDto? Type, string? Error)> CreateActivityTypeAsync(CreateActivityTypeRequest request)
    {
        var result = await _apiService.PostAsync<ActivityTypeDto>("/api/activity-types", request);
        return result.IsSuccess ? (result.Data, null) : (null, result.ErrorMessage);
    }

    public async Task<(ActivityTypeDto? Type, string? Error)> UpdateActivityTypeAsync(Guid id, CreateActivityTypeRequest request)
    {
        var result = await _apiService.PutAsync<ActivityTypeDto>($"/api/activity-types/{id}", request);
        return result.IsSuccess ? (result.Data, null) : (null, result.ErrorMessage);
    }

    public async Task<(bool Success, string? Error)> DeleteActivityTypeAsync(Guid id)
    {
        var result = await _apiService.DeleteAsync($"/api/activity-types/{id}");
        return (result.IsSuccess, result.IsSuccess ? null : result.ErrorMessage);
    }

    // Activities
    public async Task<(List<ActivityDto>? Activities, string? Error)> GetActivitiesAsync()
    {
        var result = await _apiService.GetAsync<List<ActivityDto>>("/api/activities");
        return result.IsSuccess ? (result.Data, null) : (null, result.ErrorMessage);
    }

    public async Task<(List<ActivityBoardGroupDto>? Board, string? Error)> GetActivityBoardAsync()
    {
        var result = await _apiService.GetAsync<List<ActivityBoardGroupDto>>("/api/activities/board");
        return result.IsSuccess ? (result.Data, null) : (null, result.ErrorMessage);
    }

    public async Task<(ActivityDto? Activity, string? Error)> CreateActivityAsync(CreateActivityRequest request)
    {
        var result = await _apiService.PostAsync<ActivityDto>("/api/activities", request);
        return result.IsSuccess ? (result.Data, null) : (null, result.ErrorMessage);
    }

    public async Task<(ActivityDto? Activity, string? Error)> UpdateActivityAsync(Guid id, UpdateActivityRequest request)
    {
        var result = await _apiService.PutAsync<ActivityDto>($"/api/activities/{id}", request);
        return result.IsSuccess ? (result.Data, null) : (null, result.ErrorMessage);
    }

    public async Task<(bool Success, string? Error)> DeleteActivityAsync(Guid id)
    {
        var result = await _apiService.DeleteAsync($"/api/activities/{id}");
        return (result.IsSuccess, result.IsSuccess ? null : result.ErrorMessage);
    }

    // Sessions
    public async Task<(ActivitySessionDto? Session, string? Error)> StartSessionAsync(StartActivitySessionRequest request)
    {
        var result = await _apiService.PostAsync<ActivitySessionDto>("/api/activity-sessions/start", request);
        return result.IsSuccess ? (result.Data, null) : (null, result.ErrorMessage);
    }

    public async Task<(ActivitySessionDto? Session, string? Error)> StopSessionAsync(Guid sessionId)
    {
        var result = await _apiService.PutAsync<ActivitySessionDto>($"/api/activity-sessions/{sessionId}/stop", new { });
        return result.IsSuccess ? (result.Data, null) : (null, result.ErrorMessage);
    }

    public async Task<(ActivitySessionDto? Session, string? Error)> CancelSessionAsync(Guid sessionId, string? reason = null)
    {
        var request = new CancelActivitySessionRequest { Reason = reason };
        var result = await _apiService.PutAsync<ActivitySessionDto>($"/api/activity-sessions/{sessionId}/cancel", request);
        return result.IsSuccess ? (result.Data, null) : (null, result.ErrorMessage);
    }

    public async Task<(ActivitySessionDto? Session, string? Error)> GetSessionAsync(Guid sessionId)
    {
        var result = await _apiService.GetAsync<ActivitySessionDto>($"/api/activity-sessions/{sessionId}");
        return result.IsSuccess ? (result.Data, null) : (null, result.ErrorMessage);
    }
}
