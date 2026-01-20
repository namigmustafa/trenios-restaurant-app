using System.Text.Json.Serialization;

namespace Trenios.Mobile.Models.Api;

public class LoginRequest
{
    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;

    [JsonPropertyName("password")]
    public string Password { get; set; } = string.Empty;
}

public class LoginResponse
{
    [JsonPropertyName("token")]
    public string Token { get; set; } = string.Empty;

    [JsonPropertyName("expiresAt")]
    public DateTime ExpiresAt { get; set; }

    [JsonPropertyName("user")]
    public UserDto User { get; set; } = null!;
}

public class UserDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("firstName")]
    public string FirstName { get; set; } = string.Empty;

    [JsonPropertyName("lastName")]
    public string LastName { get; set; } = string.Empty;

    [JsonPropertyName("fullName")]
    public string FullName { get; set; } = string.Empty;

    [JsonPropertyName("role")]
    public int Role { get; set; }

    // Direct IDs (in case API returns them directly)
    [JsonPropertyName("restaurantId")]
    public Guid? RestaurantId { get; set; }

    [JsonPropertyName("branchId")]
    public Guid? BranchId { get; set; }

    // Nested objects (in case API returns full objects)
    [JsonPropertyName("restaurant")]
    public RestaurantDto? Restaurant { get; set; }

    [JsonPropertyName("branch")]
    public BranchDto? Branch { get; set; }

    [JsonIgnore]
    public UserRole UserRole => (UserRole)Role;

    // SuperAdmin: no restaurant, no branch
    [JsonIgnore]
    public bool NeedsRestaurantSelection => UserRole == UserRole.SuperAdmin;

    // SuperAdmin and RestaurantOwner need to select branch
    [JsonIgnore]
    public bool NeedsBranchSelection => UserRole == UserRole.SuperAdmin || UserRole == UserRole.RestaurantOwner;

    // BranchManager and Cashier have both restaurant and branch
    [JsonIgnore]
    public bool CanGoDirectlyToPOS => UserRole == UserRole.BranchManager || UserRole == UserRole.Cashier;

    // Helper to get restaurant ID from either direct field or nested object
    [JsonIgnore]
    public Guid? EffectiveRestaurantId => RestaurantId ?? Restaurant?.Id;

    // Helper to get branch ID from either direct field or nested object
    [JsonIgnore]
    public Guid? EffectiveBranchId => BranchId ?? Branch?.Id;
}

public class RestaurantDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; } = true;
}

public class BranchDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("restaurantId")]
    public Guid? RestaurantId { get; set; }

    [JsonPropertyName("restaurantName")]
    public string? RestaurantName { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("code")]
    public string? Code { get; set; }

    [JsonPropertyName("address")]
    public string? Address { get; set; }

    [JsonPropertyName("phone")]
    public string? Phone { get; set; }

    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; } = true;
}
