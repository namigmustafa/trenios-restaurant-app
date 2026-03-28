using System.Text.Json.Serialization;
using Trenios.Mobile.Helpers;

namespace Trenios.Mobile.Models.Api;

// ── Activity Type DTOs ──────────────────────────────────────────────────────

public class ActivityTypeDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("restaurantId")]
    public Guid RestaurantId { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; }
}

public class CreateActivityTypeRequest
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; } = true;
}

// ── Activity DTOs ──────────────────────────────────────────────────────────

public class ActivityDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("restaurantId")]
    public Guid RestaurantId { get; set; }

    [JsonPropertyName("branchId")]
    public Guid BranchId { get; set; }

    [JsonPropertyName("activityTypeId")]
    public Guid ActivityTypeId { get; set; }

    [JsonPropertyName("activityTypeName")]
    public string ActivityTypeName { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("pricingUnit")]
    public int PricingUnit { get; set; }

    [JsonPropertyName("unitPrice")]
    public decimal UnitPrice { get; set; }

    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; }

    [JsonIgnore]
    public ActivityPricingUnit PricingUnitEnum => (ActivityPricingUnit)PricingUnit;

    [JsonIgnore]
    public string PricingUnitDisplay => PricingUnitEnum == ActivityPricingUnit.PerHour ? "/hr" : "/min";

    [JsonIgnore]
    public string UnitPriceDisplay => $"{CurrencyFormatter.Format(UnitPrice)}{PricingUnitDisplay}";
}

public class CreateActivityRequest
{
    [JsonPropertyName("activityTypeId")]
    public Guid ActivityTypeId { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("pricingUnit")]
    public int PricingUnit { get; set; }

    [JsonPropertyName("unitPrice")]
    public decimal UnitPrice { get; set; }

    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; } = true;
}

public class UpdateActivityRequest
{
    [JsonPropertyName("activityTypeId")]
    public Guid ActivityTypeId { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("pricingUnit")]
    public int PricingUnit { get; set; }

    [JsonPropertyName("unitPrice")]
    public decimal UnitPrice { get; set; }

    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; }
}

// ── Activity Board DTOs ────────────────────────────────────────────────────

public class ActiveSessionBoardInfo
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("startedAt")]
    public DateTime StartedAt { get; set; }

    [JsonPropertyName("elapsedMinutes")]
    public int ElapsedMinutes { get; set; }

    [JsonPropertyName("orderId")]
    public Guid OrderId { get; set; }

    [JsonPropertyName("orderNumber")]
    public string OrderNumber { get; set; } = string.Empty;

    [JsonIgnore]
    public string ElapsedDisplay
    {
        get
        {
            var minutes = (int)(DateTime.UtcNow - StartedAt).TotalMinutes;
            if (minutes < 1) return "< 1 min";
            if (minutes < 60) return $"{minutes} min";
            var h = minutes / 60;
            var m = minutes % 60;
            return m > 0 ? $"{h}h {m}m" : $"{h}h";
        }
    }
}

public class ActivityBoardItemDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("pricingUnit")]
    public int PricingUnit { get; set; }

    [JsonPropertyName("unitPrice")]
    public decimal UnitPrice { get; set; }

    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; }

    [JsonPropertyName("activeSession")]
    public ActiveSessionBoardInfo? ActiveSession { get; set; }

    [JsonIgnore]
    public bool IsOccupied => ActiveSession != null;

    [JsonIgnore]
    public ActivityPricingUnit PricingUnitEnum => (ActivityPricingUnit)PricingUnit;

    [JsonIgnore]
    public string PricingUnitDisplay => PricingUnitEnum == ActivityPricingUnit.PerHour ? "/hr" : "/min";

    [JsonIgnore]
    public string UnitPriceDisplay => $"{CurrencyFormatter.Format(UnitPrice)}{PricingUnitDisplay}";

    [JsonIgnore]
    public string StatusDisplay => IsOccupied ? "ACTIVE" : "FREE";

    [JsonIgnore]
    public Color StatusColor => IsOccupied ? Colors.Green : Colors.Gray;

    [JsonIgnore]
    public Color StatusBackgroundColor => IsOccupied
        ? Color.FromRgb(220, 252, 231)   // light green
        : Color.FromRgb(243, 244, 246);  // light gray
}

public class ActivityBoardGroupDto
{
    [JsonPropertyName("activityTypeName")]
    public string ActivityTypeName { get; set; } = string.Empty;

    [JsonPropertyName("activities")]
    public List<ActivityBoardItemDto> Activities { get; set; } = new();
}

// ── Activity Session DTOs ──────────────────────────────────────────────────

public class ActivitySessionDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("activityId")]
    public Guid ActivityId { get; set; }

    [JsonPropertyName("activityName")]
    public string ActivityName { get; set; } = string.Empty;

    [JsonPropertyName("activityTypeName")]
    public string ActivityTypeName { get; set; } = string.Empty;

    [JsonPropertyName("orderId")]
    public Guid OrderId { get; set; }

    [JsonPropertyName("orderNumber")]
    public string OrderNumber { get; set; } = string.Empty;

    [JsonPropertyName("orderAutoCreated")]
    public bool OrderAutoCreated { get; set; }

    [JsonPropertyName("pricingUnit")]
    public int PricingUnit { get; set; }

    [JsonPropertyName("unitPrice")]
    public decimal UnitPrice { get; set; }

    [JsonPropertyName("startedAt")]
    public DateTime StartedAt { get; set; }

    [JsonPropertyName("endedAt")]
    public DateTime? EndedAt { get; set; }

    [JsonPropertyName("durationMinutes")]
    public int? DurationMinutes { get; set; }

    [JsonPropertyName("totalAmount")]
    public decimal? TotalAmount { get; set; }

    [JsonPropertyName("status")]
    public int Status { get; set; }

    [JsonPropertyName("startedByUserName")]
    public string StartedByUserName { get; set; } = string.Empty;

    [JsonPropertyName("endedByUserName")]
    public string? EndedByUserName { get; set; }

    [JsonIgnore]
    public ActivitySessionStatus SessionStatus => (ActivitySessionStatus)Status;

    [JsonIgnore]
    public ActivityPricingUnit PricingUnitEnum => (ActivityPricingUnit)PricingUnit;

    [JsonIgnore]
    public string DurationDisplay
    {
        get
        {
            TimeSpan? span = EndedAt.HasValue
                ? EndedAt.Value - StartedAt
                : DurationMinutes.HasValue
                    ? TimeSpan.FromMinutes(DurationMinutes.Value)
                    : null;
            if (span == null) return string.Empty;
            var h = (int)span.Value.TotalHours;
            return $"{h}:{span.Value.Minutes:D2}:{span.Value.Seconds:D2}";
        }
    }

    [JsonIgnore]
    public string TotalAmountDisplay => TotalAmount.HasValue ? CurrencyFormatter.Format(TotalAmount.Value) : string.Empty;
}

// embedded in OrderResponse
public class ActivitySessionSummaryDto : System.ComponentModel.INotifyPropertyChanged
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("activityName")]
    public string ActivityName { get; set; } = string.Empty;

    [JsonPropertyName("activityTypeName")]
    public string ActivityTypeName { get; set; } = string.Empty;

    [JsonPropertyName("pricingUnit")]
    public int PricingUnit { get; set; }

    [JsonPropertyName("unitPrice")]
    public decimal UnitPrice { get; set; }

    [JsonPropertyName("startedAt")]
    public DateTime StartedAt { get; set; }

    [JsonPropertyName("endedAt")]
    public DateTime? EndedAt { get; set; }

    [JsonPropertyName("durationMinutes")]
    public int? DurationMinutes { get; set; }

    [JsonPropertyName("totalAmount")]
    public decimal? TotalAmount { get; set; }

    [JsonPropertyName("status")]
    public int Status { get; set; }

    [JsonPropertyName("startedByUserName")]
    public string StartedByUserName { get; set; } = string.Empty;

    [JsonPropertyName("endedByUserName")]
    public string? EndedByUserName { get; set; }

    [JsonIgnore]
    public ActivitySessionStatus SessionStatus => (ActivitySessionStatus)Status;

    [JsonIgnore]
    public bool IsActive => SessionStatus == ActivitySessionStatus.Active;

    [JsonIgnore]
    public ActivityPricingUnit PricingUnitEnum => (ActivityPricingUnit)PricingUnit;

    [JsonIgnore]
    public string PricingUnitDisplay => PricingUnitEnum == ActivityPricingUnit.PerHour ? "/hr" : "/min";

    [JsonIgnore]
    public string StatusDisplay => SessionStatus switch
    {
        ActivitySessionStatus.Active => "Active",
        ActivitySessionStatus.Completed => "Completed",
        ActivitySessionStatus.Cancelled => "Cancelled",
        _ => "Unknown"
    };

    [JsonIgnore]
    public Color StatusColor => SessionStatus switch
    {
        ActivitySessionStatus.Active => Colors.Green,
        ActivitySessionStatus.Completed => Color.FromRgb(37, 99, 235),
        ActivitySessionStatus.Cancelled => Colors.Red,
        _ => Colors.Gray
    };

    [JsonIgnore]
    public string DurationDisplay
    {
        get
        {
            if (SessionStatus == ActivitySessionStatus.Active)
            {
                var span = DateTime.UtcNow - StartedAt;
                return $"{(int)span.TotalHours}:{span.Minutes:D2}:{span.Seconds:D2}";
            }
            var duration = EndedAt.HasValue
                ? EndedAt.Value - StartedAt
                : DurationMinutes.HasValue
                    ? TimeSpan.FromMinutes(DurationMinutes.Value)
                    : (TimeSpan?)null;
            if (duration == null) return string.Empty;
            return $"{(int)duration.Value.TotalHours}:{duration.Value.Minutes:D2}:{duration.Value.Seconds:D2}";
        }
    }

    [JsonIgnore]
    public string TotalAmountDisplay => TotalAmount.HasValue ? CurrencyFormatter.Format(TotalAmount.Value) : string.Empty;

    public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;

    public void Tick()
    {
        if (SessionStatus == ActivitySessionStatus.Active)
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(DurationDisplay)));
    }
}

public class StartActivitySessionRequest
{
    [JsonPropertyName("activityId")]
    public Guid ActivityId { get; set; }

    [JsonPropertyName("orderId")]
    public Guid? OrderId { get; set; }
}

public class CancelActivitySessionRequest
{
    [JsonPropertyName("reason")]
    public string? Reason { get; set; }
}
