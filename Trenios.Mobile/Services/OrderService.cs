using Trenios.Mobile.Models.Api;

namespace Trenios.Mobile.Services;

public class OrderService
{
    private readonly ApiService _apiService;
    private readonly AuthService _authService;

    private List<CartItem> _cartItems = new();
    private OrderResponse? _lastCompletedOrder;
    private readonly List<List<CartItem>> _heldOrders = new();

    public event Action? OnCartChanged;

    public IReadOnlyList<CartItem> CartItems => _cartItems.AsReadOnly();
    public OrderResponse? LastCompletedOrder => _lastCompletedOrder;
    public int HeldOrdersCount => _heldOrders.Count;

    public decimal Subtotal => _cartItems.Sum(i => i.TotalPrice);
    public decimal Tax => Subtotal * 0.08m; // 8% tax - adjust as needed
    public decimal Total => Subtotal + Tax;
    public int TotalItems => _cartItems.Sum(i => i.Quantity);
    public bool HasItems => _cartItems.Count > 0;

    public OrderService(ApiService apiService, AuthService authService)
    {
        _apiService = apiService;
        _authService = authService;
    }

    public void AddItem(BranchMenuItemDto menuItem, int quantity = 1,
        List<SelectedAddition>? additions = null, string? notes = null)
    {
        // Check if same item with same additions exists
        var existing = _cartItems.FirstOrDefault(i =>
            i.MenuItemId == menuItem.MenuItemId &&
            i.Notes == notes &&
            AdditionsMatch(i.Additions, additions));

        if (existing != null)
        {
            existing.Quantity += quantity;
        }
        else
        {
            var cartItem = new CartItem
            {
                Id = Guid.NewGuid(),
                MenuItemId = menuItem.MenuItemId,
                MenuItemName = menuItem.MenuItemName,
                UnitPrice = menuItem.Price,
                Quantity = quantity,
                Additions = additions ?? new(),
                Notes = notes
            };
            _cartItems.Add(cartItem);
        }

        OnCartChanged?.Invoke();
    }

    private bool AdditionsMatch(List<SelectedAddition> a, List<SelectedAddition>? b)
    {
        if (b == null || b.Count == 0) return a.Count == 0;
        if (a.Count != b.Count) return false;

        // Match by AdditionId and Quantity
        var aPairs = a.Select(x => (x.AdditionId, x.Quantity)).OrderBy(x => x.AdditionId);
        var bPairs = b.Select(x => (x.AdditionId, x.Quantity)).OrderBy(x => x.AdditionId);
        return aPairs.SequenceEqual(bPairs);
    }

    public void UpdateQuantity(Guid cartItemId, int quantity)
    {
        var item = _cartItems.FirstOrDefault(i => i.Id == cartItemId);
        if (item != null)
        {
            if (quantity <= 0)
            {
                _cartItems.Remove(item);
            }
            else
            {
                item.Quantity = quantity;
            }
            OnCartChanged?.Invoke();
        }
    }

    public void RemoveItem(Guid cartItemId)
    {
        var item = _cartItems.FirstOrDefault(i => i.Id == cartItemId);
        if (item != null)
        {
            _cartItems.Remove(item);
            OnCartChanged?.Invoke();
        }
    }

    public void ClearCart()
    {
        _cartItems.Clear();
        OnCartChanged?.Invoke();
    }

    public void HoldOrder()
    {
        if (_cartItems.Count > 0)
        {
            _heldOrders.Add(new List<CartItem>(_cartItems));
            _cartItems.Clear();
            OnCartChanged?.Invoke();
        }
    }

    public void ResumeOrder(int index)
    {
        if (index >= 0 && index < _heldOrders.Count)
        {
            if (_cartItems.Count > 0)
            {
                HoldOrder();
            }
            _cartItems = _heldOrders[index];
            _heldOrders.RemoveAt(index);
            OnCartChanged?.Invoke();
        }
    }

    public async Task<(OrderResponse? Order, string? Error)> SubmitOrderAsync(
        OrderType orderType = OrderType.TakeAway, Guid? tableId = null, string? notes = null)
    {
        var branchId = _authService.GetEffectiveBranchId();
        if (branchId == null)
        {
            return (null, "No branch selected");
        }

        if (_cartItems.Count == 0)
        {
            return (null, "Cart is empty");
        }

        var request = new CreateOrderRequest
        {
            BranchId = branchId.Value,
            Type = (int)orderType,
            TableId = tableId,
            Notes = notes,
            Items = _cartItems.Select(item => new CreateOrderItemRequest
            {
                MenuItemId = item.MenuItemId,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                Notes = item.Notes,
                Additions = item.Additions.Select(a => new CreateOrderItemAdditionRequest
                {
                    AdditionId = a.AdditionId,
                    UnitPrice = a.UnitPrice,
                    Quantity = a.Quantity
                }).ToList()
            }).ToList()
        };

        var result = await _apiService.PostAsync<OrderResponse>("/api/orders", request);

        if (result.IsSuccess && result.Data != null)
        {
            _lastCompletedOrder = result.Data;
            _cartItems.Clear();
            OnCartChanged?.Invoke();
            return (result.Data, null);
        }

        return (null, result.ErrorMessage);
    }

    public void RepeatLastOrder()
    {
        if (_lastCompletedOrder != null)
        {
            foreach (var item in _lastCompletedOrder.Items)
            {
                var cartItem = new CartItem
                {
                    Id = Guid.NewGuid(),
                    MenuItemId = item.MenuItemId,
                    MenuItemName = item.MenuItemName,
                    UnitPrice = item.UnitPrice,
                    Quantity = item.Quantity,
                    Additions = item.Additions?.Select(a => new SelectedAddition
                    {
                        AdditionId = a.Id,
                        AdditionName = a.AdditionName,
                        UnitPrice = a.UnitPrice,
                        Quantity = a.Quantity
                    }).ToList() ?? new()
                };
                _cartItems.Add(cartItem);
            }
            OnCartChanged?.Invoke();
        }
    }

    public async Task<(bool Success, string? Error)> UpdateOrderStatusAsync(
        Guid orderId, OrderStatus status, string? cancellationReason = null)
    {
        var request = new UpdateOrderStatusRequest
        {
            Status = (int)status,
            CancellationReason = cancellationReason
        };

        var result = await _apiService.PatchAsync<object>($"/api/orders/{orderId}/status", request);

        return (result.IsSuccess, result.ErrorMessage);
    }

    public async Task<ApiResult<List<OrderResponse>>> GetBranchOrdersAsync(Guid branchId)
    {
        return await _apiService.GetAsync<List<OrderResponse>>($"/api/orders?branchId={branchId}");
    }
}

public class CartItem
{
    public Guid Id { get; set; }
    public Guid MenuItemId { get; set; }
    public string MenuItemName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public List<SelectedAddition> Additions { get; set; } = new();
    public string? Notes { get; set; }

    public decimal AdditionsTotal => Additions.Sum(a => a.TotalPrice);
    public decimal ItemPrice => UnitPrice + AdditionsTotal;
    public decimal TotalPrice => ItemPrice * Quantity;

    public string? AdditionsSummary => Additions.Count > 0
        ? string.Join(", ", Additions.Select(a => a.Quantity > 1 ? $"{a.AdditionName} x{a.Quantity}" : a.AdditionName))
        : null;
}

public class SelectedAddition
{
    public Guid AdditionId { get; set; }
    public string AdditionName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; } = 1;

    public decimal TotalPrice => UnitPrice * Quantity;

    // Alias for backward compatibility
    public decimal Price => TotalPrice;
}
