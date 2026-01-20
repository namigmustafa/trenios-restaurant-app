using System.Text.Json.Serialization;

namespace Trenios.Mobile.Models.Api;

public class CreateOrderRequest
{
    [JsonPropertyName("branchId")]
    public Guid BranchId { get; set; }

    [JsonPropertyName("type")]
    public int Type { get; set; } = (int)OrderType.TakeAway;

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
    public string OrderTypeDisplay => OrderType switch
    {
        OrderType.DineIn => "Dine In",
        OrderType.TakeAway => "Take Away",
        OrderType.Delivery => "Delivery",
        _ => "Unknown"
    };

    [JsonIgnore]
    public string StatusDisplay => OrderStatus switch
    {
        OrderStatus.Created => "Created",
        OrderStatus.Confirmed => "Confirmed",
        OrderStatus.Preparing => "Preparing",
        OrderStatus.Completed => "Completed",
        OrderStatus.Cancelled => "Cancelled",
        _ => "Unknown"
    };

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
    public string PlacedAtDisplay => PlacedAt.ToString("HH:mm");

    [JsonIgnore]
    public string PlacedAtFullDisplay => PlacedAt.ToString("dd MMM yyyy HH:mm");

    [JsonIgnore]
    public int TotalItems => Items.Sum(i => i.Quantity);
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
