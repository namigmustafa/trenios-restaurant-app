using System.Text.Json.Serialization;
using Trenios.Mobile.Helpers;
using Trenios.Mobile.Services;

namespace Trenios.Mobile.Models.Api;

public class CreateOrderRequest
{
    [JsonPropertyName("branchId")]
    public Guid BranchId { get; set; }

    [JsonPropertyName("type")]
    public int Type { get; set; } = (int)OrderType.TakeAway;

    [JsonPropertyName("tableId")]
    public Guid? TableId { get; set; }

    [JsonPropertyName("notes")]
    public string? Notes { get; set; }

    [JsonPropertyName("items")]
    public List<CreateOrderItemRequest> Items { get; set; } = new();
}

public class CreateOrderItemRequest
{
    [JsonPropertyName("menuItemId")]
    public Guid MenuItemId { get; set; }

    [JsonPropertyName("quantity")]
    public int Quantity { get; set; }

    [JsonPropertyName("unitPrice")]
    public decimal UnitPrice { get; set; }

    [JsonPropertyName("notes")]
    public string? Notes { get; set; }

    [JsonPropertyName("additions")]
    public List<CreateOrderItemAdditionRequest>? Additions { get; set; }
}

public class CreateOrderItemAdditionRequest
{
    [JsonPropertyName("additionId")]
    public Guid AdditionId { get; set; }

    [JsonPropertyName("unitPrice")]
    public decimal UnitPrice { get; set; }

    [JsonPropertyName("quantity")]
    public int Quantity { get; set; } = 1;
}

public class OrderResponse
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("orderNumber")]
    public string OrderNumber { get; set; } = string.Empty;

    [JsonPropertyName("branchId")]
    public Guid BranchId { get; set; }

    [JsonPropertyName("branchName")]
    public string BranchName { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public int Type { get; set; }

    [JsonPropertyName("tableId")]
    public Guid? TableId { get; set; }

    [JsonPropertyName("tableNumber")]
    public string? TableNumber { get; set; }

    [JsonPropertyName("status")]
    public int Status { get; set; }

    [JsonPropertyName("subTotal")]
    public decimal SubTotal { get; set; }

    [JsonPropertyName("taxAmount")]
    public decimal TaxAmount { get; set; }

    [JsonPropertyName("discountAmount")]
    public decimal DiscountAmount { get; set; }

    [JsonPropertyName("totalAmount")]
    public decimal TotalAmount { get; set; }

    [JsonPropertyName("items")]
    public List<OrderItemResponse> Items { get; set; } = new();

    [JsonPropertyName("placedAt")]
    public DateTime PlacedAt { get; set; }

    // Helper properties for display
    [JsonIgnore]
    public OrderType OrderType => (OrderType)Type;

    [JsonIgnore]
    public OrderStatus OrderStatus => (OrderStatus)Status;

    [JsonIgnore]
    public string OrderTypeIcon => OrderType switch
    {
        OrderType.DineIn => "🍽",
        OrderType.TakeAway => "🛍",
        OrderType.Delivery => "🚗",
        _ => "📦"
    };

    [JsonIgnore]
    public string OrderTypeDisplay
    {
        get
        {
            var loc = LocalizationService.Instance;
            return OrderType switch
            {
                OrderType.DineIn => loc["DineIn"],
                OrderType.TakeAway => loc["TakeAway"],
                OrderType.Delivery => loc["Delivery"],
                _ => "Unknown"
            };
        }
    }

    [JsonIgnore]
    public bool HasTable => !string.IsNullOrEmpty(TableNumber);

    [JsonIgnore]
    public string TableDisplay => HasTable ? $"{LocalizationService.Instance["Table"]} {TableNumber}" : string.Empty;

    [JsonIgnore]
    public string StatusDisplay
    {
        get
        {
            var loc = LocalizationService.Instance;
            return OrderStatus switch
            {
                OrderStatus.Created => loc["Created"],
                OrderStatus.Confirmed => loc["Confirmed"],
                OrderStatus.Preparing => loc["Preparing"],
                OrderStatus.Completed => loc["Completed"],
                OrderStatus.Cancelled => loc["Cancelled"],
                _ => "Unknown"
            };
        }
    }

    [JsonIgnore]
    public Color StatusColor => OrderStatus switch
    {
        OrderStatus.Created => Colors.Orange,
        OrderStatus.Confirmed => Colors.Blue,
        OrderStatus.Preparing => Colors.Purple,
        OrderStatus.Completed => Colors.Green,
        OrderStatus.Cancelled => Colors.Red,
        _ => Colors.Gray
    };

    [JsonIgnore]
    public string TotalAmountDisplay => CurrencyFormatter.Format(TotalAmount);

    [JsonIgnore]
    public string SubTotalDisplay => CurrencyFormatter.Format(SubTotal);

    [JsonIgnore]
    public string DiscountAmountDisplay => $"-{CurrencyFormatter.Format(DiscountAmount)}";

    [JsonIgnore]
    public string PlacedAtDisplay => PlacedAt.ToLocalTime().ToString("HH:mm");

    [JsonIgnore]
    public string PlacedAtFullDisplay => PlacedAt.ToLocalTime().ToString("dd MMM yyyy HH:mm");

    [JsonIgnore]
    public int TotalItems => Items.Sum(i => i.Quantity);

    // Kitchen-optimized urgency properties
    [JsonIgnore]
    public int AgeInMinutes => (int)(DateTime.UtcNow - PlacedAt).TotalMinutes;

    [JsonIgnore]
    public string AgeDisplay
    {
        get
        {
            var minutes = AgeInMinutes;
            if (minutes < 1) return "Just now";
            if (minutes == 1) return "1 min";
            if (minutes < 60) return $"{minutes} mins";
            var hours = minutes / 60;
            var remainingMins = minutes % 60;
            return remainingMins > 0 ? $"{hours}h {remainingMins}m" : $"{hours}h";
        }
    }

    [JsonIgnore]
    public Color UrgencyColor
    {
        get
        {
            var minutes = AgeInMinutes;
            if (minutes >= 15) return Color.FromRgb(220, 38, 38);      // Red - URGENT
            if (minutes >= 10) return Color.FromRgb(251, 146, 60);     // Orange - Soon
            if (minutes >= 5) return Color.FromRgb(250, 204, 21);      // Yellow - Watch
            return Color.FromRgb(34, 197, 94);                          // Green - Fresh
        }
    }

    [JsonIgnore]
    public Color UrgencyBackgroundColor
    {
        get
        {
            var minutes = AgeInMinutes;
            if (minutes >= 15) return Color.FromRgb(254, 242, 242);    // Light red
            if (minutes >= 10) return Color.FromRgb(255, 247, 237);    // Light orange
            if (minutes >= 5) return Color.FromRgb(254, 252, 232);     // Light yellow
            return Color.FromRgb(240, 253, 244);                        // Light green
        }
    }

    [JsonIgnore]
    public string UrgencyText
    {
        get
        {
            var minutes = AgeInMinutes;
            if (minutes >= 15) return "⚠ URGENT";
            if (minutes >= 10) return "⏰ SOON";
            if (minutes >= 5) return "⏱ WATCH";
            return "✓ FRESH";
        }
    }

    // Helper for kitchen display
    [JsonIgnore]
    public bool CanStartPreparing => OrderStatus == OrderStatus.Created || OrderStatus == OrderStatus.Confirmed;

    // Helper for orders page - only allow swiping for Created and Preparing statuses
    [JsonIgnore]
    public bool CanSwipe => OrderStatus == OrderStatus.Created || OrderStatus == OrderStatus.Preparing;

    // Helper for order details buttons - disable only when Cancelled or Completed
    [JsonIgnore]
    public bool CanChangeStatus => OrderStatus != OrderStatus.Cancelled && OrderStatus != OrderStatus.Completed;
}

public class OrderItemResponse
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("menuItemId")]
    public Guid MenuItemId { get; set; }

    [JsonPropertyName("menuItemName")]
    public string MenuItemName { get; set; } = string.Empty;

    [JsonPropertyName("quantity")]
    public int Quantity { get; set; }

    [JsonPropertyName("unitPrice")]
    public decimal UnitPrice { get; set; }

    [JsonPropertyName("totalPrice")]
    public decimal TotalPrice { get; set; }

    [JsonPropertyName("additions")]
    public List<OrderItemAdditionResponse>? Additions { get; set; }

    // Helper properties
    [JsonIgnore]
    public string TotalPriceDisplay => CurrencyFormatter.Format(TotalPrice);

    [JsonIgnore]
    public bool HasAdditions => Additions?.Count > 0;

    [JsonIgnore]
    public string AdditionsSummary => HasAdditions
        ? string.Join(", ", Additions!.Select(a => a.Quantity > 1 ? $"{a.Quantity}x {a.AdditionName}" : a.AdditionName))
        : string.Empty;

    [JsonIgnore]
    public string QuantityDisplay => $"{Quantity}x";
}

public class OrderItemAdditionResponse
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("additionName")]
    public string AdditionName { get; set; } = string.Empty;

    [JsonPropertyName("unitPrice")]
    public decimal UnitPrice { get; set; }

    [JsonPropertyName("quantity")]
    public int Quantity { get; set; }

    [JsonPropertyName("totalPrice")]
    public decimal TotalPrice { get; set; }

    [JsonIgnore]
    public string DisplayText => Quantity > 1 ? $"** {AdditionName} x{Quantity}" : $"** {AdditionName}";
}

public class UpdateOrderStatusRequest
{
    [JsonPropertyName("status")]
    public int Status { get; set; }

    [JsonPropertyName("cancellationReason")]
    public string? CancellationReason { get; set; }
}

public class ApiError
{
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
}
