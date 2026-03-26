using System.Text.Json;
using Trenios.Mobile.Helpers;
using Trenios.Mobile.Models.Api;

namespace Trenios.Mobile.Services;

public class OrderService
{
    private readonly ApiService _apiService;
    private readonly AuthService _authService;

    private List<CartItem> _cartItems = new();
    private OrderResponse? _lastCompletedOrder;
    private readonly List<List<CartItem>> _heldOrders = new();

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public event Action? OnCartChanged;

    public IReadOnlyList<CartItem> CartItems => _cartItems.AsReadOnly();
    public OrderResponse? LastCompletedOrder => _lastCompletedOrder;
    public int HeldOrdersCount => _heldOrders.Count;

    public decimal Subtotal => _cartItems.Sum(i => i.TotalPrice);
    public decimal CartDiscountAmount => _cartItems.Sum(i => i.UnitDiscountAmount * i.Quantity);
    public decimal Tax => Subtotal * 0.08m; // 8% tax - adjust as needed
    public decimal Total => Subtotal + Tax;
    public int TotalItems => _cartItems.Sum(i => i.Quantity);
    public bool HasItems => _cartItems.Count > 0;
    public bool HasCartDiscount => CartDiscountAmount > 0;

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
                OriginalUnitPrice = menuItem.Price,
                DiscountedUnitPrice = menuItem.Price,
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
                _cartItems.Remove(item);
            else
                item.Quantity = quantity;

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
                HoldOrder();

            _cartItems = _heldOrders[index];
            _heldOrders.RemoveAt(index);
            OnCartChanged?.Invoke();
        }
    }

    // ── Validate-Cart ───────────────────────────────────────────────────────

    /// <summary>
    /// Calls POST /api/orders/validate-cart for the current cart items.
    /// Returns null and a user-facing error string on failure.
    /// </summary>
    public async Task<(ValidateCartResponse? Response, string? Error)> ValidateCartAsync()
    {
        var branchId = _authService.GetEffectiveBranchId();
        System.Diagnostics.Debug.WriteLine($"[ValidateCart] BranchId: {branchId}, CartItems: {_cartItems.Count}");

        if (branchId == null) return (null, "No branch selected");
        if (_cartItems.Count == 0) return (null, "Cart is empty");

        var request = new ValidateCartRequest
        {
            BranchId = branchId.Value,
            Items = _cartItems.Select(i => new ValidateCartItemRequest
            {
                MenuItemId = i.MenuItemId,
                Quantity = i.Quantity
            }).ToList()
        };

        System.Diagnostics.Debug.WriteLine($"[ValidateCart] Sending {request.Items.Count} items to /api/orders/validate-cart");
        foreach (var item in _cartItems)
            System.Diagnostics.Debug.WriteLine($"[ValidateCart]   Item: {item.MenuItemName}, DiscountedPrice: {item.DiscountedUnitPrice}, OriginalPrice: {item.OriginalUnitPrice}");

        var result = await _apiService.PostAsync<ValidateCartResponse>("/api/orders/validate-cart", request);

        System.Diagnostics.Debug.WriteLine($"[ValidateCart] Response: IsSuccess={result.IsSuccess}, StatusCode={result.StatusCode}, Error={result.ErrorMessage}");

        if (result.IsSuccess && result.Data != null)
        {
            System.Diagnostics.Debug.WriteLine($"[ValidateCart] Success! Items returned: {result.Data.Items.Count}, CartTotal={result.Data.CartTotal}, CartDiscountTotal={result.Data.CartDiscountTotal}");
            foreach (var item in result.Data.Items)
                System.Diagnostics.Debug.WriteLine($"[ValidateCart]   Response Item: {item.MenuItemName}, Original={item.OriginalUnitPrice}, UnitPrice={item.UnitPrice}, DiscountPerUnit={item.DiscountAmountPerUnit}, HasDiscount={item.HasDiscount}, DisplayDiscount={item.DisplayDiscount}");
            return (result.Data, null);
        }

        System.Diagnostics.Debug.WriteLine($"[ValidateCart] FAILED! StatusCode={result.StatusCode}, Error={result.ErrorMessage}");
        return (null, result.ErrorMessage);
    }

    /// <summary>
    /// Updates every CartItem with the discount info returned by validate-cart,
    /// then fires OnCartChanged so the UI refreshes.
    /// Prices are only updated when the backend returns a positive value — zero
    /// is treated as a resolution failure and the existing price is kept.
    /// </summary>
    public void ApplyValidationResult(ValidateCartResponse response)
    {
        foreach (var item in _cartItems)
        {
            var match = response.Items.FirstOrDefault(r => r.MenuItemId == item.MenuItemId);
            if (match == null) continue;

            if (match.OriginalUnitPrice > 0)
                item.OriginalUnitPrice = match.OriginalUnitPrice;

            // unitPrice is always the final price (with or without discount)
            if (match.UnitPrice > 0)
                item.DiscountedUnitPrice = match.UnitPrice;
            else
                item.DiscountedUnitPrice = item.OriginalUnitPrice;

            item.UnitDiscountAmount = match.DiscountAmountPerUnit;
            item.HasDiscount = match.HasDiscount;
            item.DiscountName = match.DiscountName;
            item.DisplayDiscount = match.DisplayDiscount;
        }

        OnCartChanged?.Invoke();
    }

    // ── Submit Order ────────────────────────────────────────────────────────

    /// <summary>
    /// Submits the current cart as an order.
    /// Always calls validate-cart first to ensure prices are fresh at submission time.
    /// Returns:
    ///   - (order, null, null)         on HTTP 201 success
    ///   - (null, priceChanged, null)  on HTTP 409 — caller must show price-change warning
    ///   - (null, null, errorMsg)      on any other failure
    /// </summary>
    public async Task<(OrderResponse? Order, PriceChangedResponse? PriceChanged, string? Error)> SubmitOrderAsync(
        OrderType orderType = OrderType.TakeAway, Guid? tableId = null, string? notes = null)
    {
        var branchId = _authService.GetEffectiveBranchId();
        if (branchId == null)
            return (null, null, "No branch selected");

        if (_cartItems.Count == 0)
            return (null, null, "Cart is empty");

        // Always refresh prices from the backend right before submitting so
        // ExpectedUnitPrice matches exactly what the backend will calculate.
        System.Diagnostics.Debug.WriteLine("[SubmitOrder] Calling ValidateCart before submit...");
        var (validateResponse, validateError) = await ValidateCartAsync();
        System.Diagnostics.Debug.WriteLine($"[SubmitOrder] ValidateCart result: success={validateResponse != null}, error={validateError}");
        if (validateResponse != null)
            ApplyValidationResult(validateResponse);
        else
            System.Diagnostics.Debug.WriteLine($"[SubmitOrder] WARNING: ValidateCart failed ({validateError}), sending catalog prices as ExpectedUnitPrice — 409 likely!");

        System.Diagnostics.Debug.WriteLine("[SubmitOrder] Cart state going into CreateOrderRequest:");
        foreach (var item in _cartItems)
            System.Diagnostics.Debug.WriteLine($"[SubmitOrder]   {item.MenuItemName}: DiscountedUnitPrice={item.DiscountedUnitPrice}, OriginalUnitPrice={item.OriginalUnitPrice}");

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
                ExpectedUnitPrice = item.DiscountedUnitPrice,
                Notes = item.Notes,
                Additions = item.Additions.Select(a => new CreateOrderItemAdditionRequest
                {
                    AdditionId = a.AdditionId,
                    UnitPrice = a.UnitPrice,
                    Quantity = a.Quantity
                }).ToList()
            }).ToList()
        };

        System.Diagnostics.Debug.WriteLine("[SubmitOrder] Sending POST /api/orders...");
        foreach (var item in request.Items)
            System.Diagnostics.Debug.WriteLine($"[SubmitOrder]   Request Item: menuItemId={item.MenuItemId}, qty={item.Quantity}, expectedUnitPrice={item.ExpectedUnitPrice}");

        var result = await _apiService.PostAsync<OrderResponse>("/api/orders", request);

        System.Diagnostics.Debug.WriteLine($"[SubmitOrder] POST /api/orders response: IsSuccess={result.IsSuccess}, StatusCode={result.StatusCode}, Error={result.ErrorMessage}");

        if (result.IsSuccess && result.Data != null)
        {
            var o = result.Data;
            System.Diagnostics.Debug.WriteLine($"[SubmitOrder] Order created: #{o.OrderNumber}, SubTotal={o.SubTotal}, DiscountAmount={o.DiscountAmount}, TaxAmount={o.TaxAmount}, TotalAmount={o.TotalAmount}");
            foreach (var item in o.Items)
                System.Diagnostics.Debug.WriteLine($"[SubmitOrder]   Item: {item.MenuItemName} x{item.Quantity}, UnitPrice={item.UnitPrice}, OriginalUnitPrice={item.OriginalUnitPrice}, DiscountAmount={item.DiscountAmount}, TotalPrice={item.TotalPrice}");

            _lastCompletedOrder = result.Data;
            _cartItems.Clear();
            OnCartChanged?.Invoke();
            return (result.Data, null, null);
        }

        // Handle HTTP 409 — price changed between validate-cart and order creation
        if (result.StatusCode == 409 && result.RawContent != null)
        {
            System.Diagnostics.Debug.WriteLine($"[SubmitOrder] 409 raw body: {result.RawContent}");
            try
            {
                var priceChanged = JsonSerializer.Deserialize<PriceChangedResponse>(result.RawContent, _jsonOptions);
                if (priceChanged != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[SubmitOrder] 409 PriceChangedResponse: {priceChanged.Changes?.Count ?? 0} changes");
                    foreach (var change in priceChanged.Changes ?? new())
                        System.Diagnostics.Debug.WriteLine($"[SubmitOrder]   Change: {change.MenuItemName}, Expected={change.ExpectedPrice}, Current={change.CurrentPrice}, Reason={change.ChangeReason}");

                    // Apply the fresh cart from the 409 body so CartState is up-to-date
                    ApplyValidationResult(priceChanged.UpdatedCart);
                    return (null, priceChanged, null);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SubmitOrder] Failed to parse 409 body: {ex.Message}");
                // Fall through to generic error
            }
        }

        return (null, null, result.ErrorMessage);
    }

    // ── Utility ─────────────────────────────────────────────────────────────

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
                    OriginalUnitPrice = item.OriginalUnitPrice > 0 ? item.OriginalUnitPrice : item.UnitPrice,
                    DiscountedUnitPrice = item.UnitPrice,
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

    /// <summary>Base price before discount (for strikethrough display).</summary>
    public decimal OriginalUnitPrice { get; set; }

    /// <summary>Price after discount — this is what gets sent as ExpectedUnitPrice.</summary>
    public decimal DiscountedUnitPrice { get; set; }

    public decimal UnitDiscountAmount { get; set; }
    public bool HasDiscount { get; set; }
    public string? DiscountName { get; set; }

    /// <summary>Human-readable badge text, e.g. "20% OFF" or "5.00 OFF".</summary>
    public string? DisplayDiscount { get; set; }

    public int Quantity { get; set; }
    public List<SelectedAddition> Additions { get; set; } = new();
    public string? Notes { get; set; }

    public decimal AdditionsTotal => Additions.Sum(a => a.TotalPrice);
    public decimal ItemPrice => DiscountedUnitPrice + AdditionsTotal;
    public decimal TotalPrice => ItemPrice * Quantity;
    public decimal OriginalTotalPrice => (OriginalUnitPrice + AdditionsTotal) * Quantity;
    public string TotalPriceDisplay => CurrencyFormatter.Format(TotalPrice);
    public string OriginalTotalPriceDisplay => CurrencyFormatter.Format(OriginalTotalPrice);
    public string OriginalUnitPriceDisplay => CurrencyFormatter.Format(OriginalUnitPrice);
    public string DiscountedUnitPriceDisplay => CurrencyFormatter.Format(DiscountedUnitPrice);

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
    public string TotalPriceDisplay => CurrencyFormatter.Format(TotalPrice);

    // Alias for backward compatibility
    public decimal Price => TotalPrice;
}
