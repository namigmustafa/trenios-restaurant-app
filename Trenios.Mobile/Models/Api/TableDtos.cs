using System.Text.Json.Serialization;
using Trenios.Mobile.Helpers;
using Trenios.Mobile.Services;

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
    public string CapacityDisplay => Capacity.HasValue ? $"{Capacity} {LocalizationService.Instance["Seats"]}" : string.Empty;
}

/// <summary>
/// Table with current reservation status - returned from GET /api/tables?branchId={id}
/// </summary>
public class TableWithReservationDto
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

    [JsonPropertyName("isReserved")]
    public bool IsReserved { get; set; }

    [JsonPropertyName("currentReservation")]
    public TableReservationDto? CurrentReservation { get; set; }

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updatedAt")]
    public DateTime? UpdatedAt { get; set; }

    // Helper properties
    [JsonIgnore]
    public string CapacityDisplay => Capacity.HasValue ? $"{Capacity} {LocalizationService.Instance["Seats"]}" : string.Empty;

    [JsonIgnore]
    public string TableDisplay => Number;

    // Used in move table dialog to highlight selected target
    [JsonIgnore]
    public bool IsSelectedTarget { get; set; }

    [JsonIgnore]
    public Color StatusColor => IsReserved
        ? Color.FromRgb(231, 76, 60)   // Red - Occupied
        : Color.FromRgb(39, 174, 96);  // Green - Available

    [JsonIgnore]
    public string StatusText => IsReserved
        ? LocalizationService.Instance["Reserved"]
        : LocalizationService.Instance["Available"];

    [JsonIgnore]
    public decimal TotalAmount => CurrentReservation?.ActiveOrdersAmount ?? 0;

    [JsonIgnore]
    public int OrdersCount => CurrentReservation?.TotalOrdersCount ?? 0;

    [JsonIgnore]
    public Color StatusBadgeBackground => IsReserved
        ? Color.FromArgb("#FDE8E8")
        : Color.FromArgb("#E8F5E9");

    [JsonIgnore]
    public Color StatusBadgeTextColor => IsReserved
        ? Color.FromRgb(185, 28, 28)
        : Color.FromRgb(21, 128, 61);

    [JsonIgnore]
    public int TotalItemsCount => CurrentReservation?.Orders?
        .Where(o => (OrderStatus)o.Status != OrderStatus.Cancelled)
        .Sum(o => o.ItemCount) ?? 0;

    [JsonIgnore]
    public string TotalAmountDisplay => CurrencyFormatter.Format(TotalAmount);

    [JsonIgnore]
    public string DurationDisplay => CurrentReservation?.DurationDisplay ?? "";
}

/// <summary>
/// Active reservation details for a table
/// </summary>
public class TableReservationDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("tableId")]
    public Guid TableId { get; set; }

    [JsonPropertyName("tableNumber")]
    public string TableNumber { get; set; } = string.Empty;

    [JsonPropertyName("startedAt")]
    public DateTime StartedAt { get; set; }

    [JsonPropertyName("endedAt")]
    public DateTime? EndedAt { get; set; }

    [JsonPropertyName("endedByReason")]
    public string? EndedByReason { get; set; }

    [JsonPropertyName("duration")]
    public string Duration { get; set; } = string.Empty;

    [JsonPropertyName("orders")]
    public List<TableOrderSummaryDto> Orders { get; set; } = new();

    [JsonPropertyName("totalOrdersAmount")]
    public decimal TotalOrdersAmount { get; set; }

    [JsonPropertyName("totalOrdersCount")]
    public int TotalOrdersCount { get; set; }

    // Helper properties
    [JsonIgnore]
    public bool IsActive => EndedAt == null;

    [JsonIgnore]
    public string StartedAtDisplay => StartedAt.ToString("HH:mm");

    [JsonIgnore]
    public string StartedAtFullDisplay => StartedAt.ToLocalTime().ToString("MMM dd, hh:mm tt");

    [JsonIgnore]
    public string DurationDisplay
    {
        get
        {
            if (TimeSpan.TryParse(Duration, out var ts))
            {
                if (ts.TotalHours >= 1)
                    return $"{(int)ts.TotalHours}h {ts.Minutes}m";
                return $"{ts.Minutes}m";
            }
            return Duration;
        }
    }

    [JsonIgnore]
    public decimal ActiveOrdersAmount => Orders
        .Where(o => (OrderStatus)o.Status != OrderStatus.Cancelled)
        .Sum(o => o.TotalAmount);

    [JsonIgnore]
    public string TotalAmountDisplay => CurrencyFormatter.Format(ActiveOrdersAmount);
}

/// <summary>
/// Order summary within a table reservation
/// </summary>
public class TableOrderSummaryDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("orderNumber")]
    public string OrderNumber { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public int Status { get; set; }

    [JsonPropertyName("totalAmount")]
    public decimal TotalAmount { get; set; }

    [JsonPropertyName("placedAt")]
    public DateTime PlacedAt { get; set; }

    [JsonPropertyName("itemCount")]
    public int ItemCount { get; set; }

    [JsonPropertyName("items")]
    public List<OrderItemResponse> Items { get; set; } = new();

    [JsonPropertyName("activitySessions")]
    public List<ActivitySessionSummaryDto> ActivitySessions { get; set; } = new();

    // Helper properties
    [JsonIgnore]
    public bool HasItems => Items?.Count > 0;

    [JsonIgnore]
    public bool HasActivitySessions => ActivitySessions?.Count > 0;

    [JsonIgnore]
    public bool CanChangeStatus => OrderStatus != Api.OrderStatus.Cancelled && OrderStatus != Api.OrderStatus.Completed;
    [JsonIgnore]
    public OrderStatus OrderStatus => (OrderStatus)Status;
    [JsonIgnore]
    public bool IsNotCancelled => OrderStatus != Api.OrderStatus.Cancelled;

    [JsonIgnore]
    public string StatusDisplay
    {
        get
        {
            var loc = LocalizationService.Instance;
            return OrderStatus switch
            {
                Api.OrderStatus.Created => loc["Created"],
                Api.OrderStatus.Confirmed => loc["Confirmed"],
                Api.OrderStatus.Preparing => loc["Preparing"],
                Api.OrderStatus.Completed => loc["Completed"],
                Api.OrderStatus.Cancelled => loc["Cancelled"],
                _ => Status.ToString()
            };
        }
    }

    [JsonIgnore]
    public Color StatusColor => OrderStatus switch
    {
        Api.OrderStatus.Created => Colors.Orange,
        Api.OrderStatus.Confirmed => Colors.Blue,
        Api.OrderStatus.Preparing => Colors.Purple,
        Api.OrderStatus.Completed => Colors.Green,
        Api.OrderStatus.Cancelled => Colors.Red,
        _ => Colors.Gray
    };

    [JsonIgnore]
    public string PlacedAtDisplay => PlacedAt.ToString("HH:mm");

    [JsonIgnore]
    public string PlacedAtFullDisplay => PlacedAt.ToLocalTime().ToString("MMM dd, HH:mm");

    [JsonIgnore]
    public string TotalAmountDisplay => CurrencyFormatter.Format(TotalAmount);

    [JsonIgnore]
    public string ItemCountDisplay => $"{ItemCount} {LocalizationService.Instance["Items"]}";
}

/// <summary>
/// Response from checkout table endpoint
/// </summary>
public class CheckoutTableResponse
{
    [JsonPropertyName("reservationId")]
    public Guid ReservationId { get; set; }

    [JsonPropertyName("tableId")]
    public Guid TableId { get; set; }

    [JsonPropertyName("tableNumber")]
    public string TableNumber { get; set; } = string.Empty;

    [JsonPropertyName("startedAt")]
    public DateTime StartedAt { get; set; }

    [JsonPropertyName("endedAt")]
    public DateTime EndedAt { get; set; }

    [JsonPropertyName("duration")]
    public string Duration { get; set; } = string.Empty;

    [JsonPropertyName("ordersCompleted")]
    public int OrdersCompleted { get; set; }

    [JsonPropertyName("totalAmount")]
    public decimal TotalAmount { get; set; }

    [JsonPropertyName("orders")]
    public List<TableOrderSummaryDto> Orders { get; set; } = new();
}

/// <summary>
/// Request to move table reservation
/// </summary>
public class MoveTableRequest
{
    [JsonPropertyName("targetTableId")]
    public Guid TargetTableId { get; set; }
}

/// <summary>
/// Request to release table
/// </summary>
public class ReleaseTableRequest
{
    [JsonPropertyName("reason")]
    public string? Reason { get; set; }
}

/// <summary>
/// Request to checkout table
/// </summary>
public class CheckoutTableRequest
{
    [JsonPropertyName("notes")]
    public string? Notes { get; set; }
}
