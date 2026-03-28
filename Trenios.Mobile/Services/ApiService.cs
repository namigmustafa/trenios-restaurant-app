using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Trenios.Mobile.Models.Api;

namespace Trenios.Mobile.Services;

public class ApiService
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;
    private string? _token;

    public ApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    public void SetToken(string? token)
    {
        _token = token;
        if (!string.IsNullOrEmpty(token))
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
        }
        else
        {
            _httpClient.DefaultRequestHeaders.Authorization = null;
        }
    }

    public void ClearToken()
    {
        _token = null;
        _httpClient.DefaultRequestHeaders.Authorization = null;
    }

    public bool HasToken => !string.IsNullOrEmpty(_token);

    public string? GetToken() => _token;

    public async Task<ApiResult<T>> GetAsync<T>(string endpoint)
    {
        try
        {
            var response = await _httpClient.GetAsync(endpoint);
            return await HandleResponse<T>(response);
        }
        catch (HttpRequestException ex)
        {
            return ApiResult<T>.Failure($"Network error: {ex.Message}");
        }
        catch (Exception ex)
        {
            return ApiResult<T>.Failure($"Error: {ex.Message}");
        }
    }

    public async Task<ApiResult<T>> PostAsync<T>(string endpoint, object? data = null)
    {
        try
        {
            var content = data != null
                ? new StringContent(JsonSerializer.Serialize(data, _jsonOptions), Encoding.UTF8, "application/json")
                : null;

            var response = await _httpClient.PostAsync(endpoint, content);
            return await HandleResponse<T>(response);
        }
        catch (HttpRequestException ex)
        {
            return ApiResult<T>.Failure($"Network error: {ex.Message}");
        }
        catch (Exception ex)
        {
            return ApiResult<T>.Failure($"Error: {ex.Message}");
        }
    }

    public async Task<ApiResult<T>> PutAsync<T>(string endpoint, object? data = null)
    {
        try
        {
            var content = data != null
                ? new StringContent(JsonSerializer.Serialize(data, _jsonOptions), Encoding.UTF8, "application/json")
                : null;

            var response = await _httpClient.PutAsync(endpoint, content);
            return await HandleResponse<T>(response);
        }
        catch (HttpRequestException ex)
        {
            return ApiResult<T>.Failure($"Network error: {ex.Message}");
        }
        catch (Exception ex)
        {
            return ApiResult<T>.Failure($"Error: {ex.Message}");
        }
    }

    public async Task<ApiResult<T>> PatchAsync<T>(string endpoint, object? data = null)
    {
        try
        {
            var content = data != null
                ? new StringContent(JsonSerializer.Serialize(data, _jsonOptions), Encoding.UTF8, "application/json")
                : null;

            var request = new HttpRequestMessage(HttpMethod.Patch, endpoint) { Content = content };
            var response = await _httpClient.SendAsync(request);
            return await HandleResponse<T>(response);
        }
        catch (HttpRequestException ex)
        {
            return ApiResult<T>.Failure($"Network error: {ex.Message}");
        }
        catch (Exception ex)
        {
            return ApiResult<T>.Failure($"Error: {ex.Message}");
        }
    }

    public async Task<ApiResult<T>> DeleteAsync<T>(string endpoint)
    {
        try
        {
            var response = await _httpClient.DeleteAsync(endpoint);
            return await HandleResponse<T>(response);
        }
        catch (HttpRequestException ex)
        {
            return ApiResult<T>.Failure($"Network error: {ex.Message}");
        }
        catch (Exception ex)
        {
            return ApiResult<T>.Failure($"Error: {ex.Message}");
        }
    }

    public async Task<ApiResult<bool>> DeleteAsync(string endpoint)
    {
        try
        {
            var response = await _httpClient.DeleteAsync(endpoint);
            if (response.IsSuccessStatusCode) return ApiResult<bool>.Success(true, (int)response.StatusCode);
            var content = await response.Content.ReadAsStringAsync();
            try
            {
                var error = System.Text.Json.JsonSerializer.Deserialize<ApiError>(content, _jsonOptions);
                if (error != null && !string.IsNullOrWhiteSpace(error.Message))
                    return ApiResult<bool>.Failure(error.Message, error.Code, (int)response.StatusCode);
            }
            catch { }
            return ApiResult<bool>.Failure($"HTTP {(int)response.StatusCode}", null, (int)response.StatusCode);
        }
        catch (HttpRequestException ex)
        {
            return ApiResult<bool>.Failure($"Network error: {ex.Message}");
        }
        catch (Exception ex)
        {
            return ApiResult<bool>.Failure($"Error: {ex.Message}");
        }
    }

    private async Task<ApiResult<T>> HandleResponse<T>(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        var statusCode = (int)response.StatusCode;

        if (response.IsSuccessStatusCode)
        {
            if (string.IsNullOrEmpty(content))
            {
                return ApiResult<T>.Success(default!, statusCode);
            }

            var result = JsonSerializer.Deserialize<T>(content, _jsonOptions);
            return ApiResult<T>.Success(result!, statusCode);
        }

        // Try to parse API error
        try
        {
            var error = JsonSerializer.Deserialize<ApiError>(content, _jsonOptions);
            if (error != null && !string.IsNullOrWhiteSpace(error.Message))
            {
                return ApiResult<T>.Failure(error.Message, error.Code, statusCode, content);
            }
        }
        catch
        {
            // Ignore parse error
        }

        // If we have content but couldn't parse it as ApiError, show the raw content (truncated)
        if (!string.IsNullOrWhiteSpace(content))
        {
            var truncated = content.Length > 200 ? content.Substring(0, 200) + "..." : content;
            return ApiResult<T>.Failure($"HTTP {statusCode}: {truncated}", null, statusCode, content);
        }

        return ApiResult<T>.Failure($"HTTP {statusCode}: {response.ReasonPhrase}", null, statusCode);
    }
}

public class ApiResult<T>
{
    public bool IsSuccess { get; private set; }
    public T? Data { get; private set; }
    public string? ErrorMessage { get; private set; }
    public string? ErrorCode { get; private set; }
    public int StatusCode { get; private set; }

    /// <summary>Raw response body — populated on non-success responses for custom parsing (e.g. HTTP 409).</summary>
    public string? RawContent { get; private set; }

    public static ApiResult<T> Success(T data, int statusCode = 200) => new()
    {
        IsSuccess = true,
        Data = data,
        StatusCode = statusCode
    };

    public static ApiResult<T> Failure(string message, string? code = null, int statusCode = 0, string? rawContent = null) => new()
    {
        IsSuccess = false,
        ErrorMessage = message,
        ErrorCode = code,
        StatusCode = statusCode,
        RawContent = rawContent
    };
}
