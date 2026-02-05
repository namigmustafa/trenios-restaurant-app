using System.Text.Json.Serialization;

namespace Trenios.Mobile.Models.Api;

public class TableDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("branchId")]
    public Guid BranchId { get; set; }

    [JsonPropertyName("branchName")]
    public string? BranchName { get; set; }

    [JsonPropertyName("number")]
    public string Number { get; set; } = string.Empty;

    [JsonPropertyName("capacity")]
    public int? Capacity { get; set; }

    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; } = true;

    // Helper for display
    [JsonIgnore]
    public string CapacityDisplay => Capacity.HasValue ? $"{Capacity} seats" : string.Empty;
}
