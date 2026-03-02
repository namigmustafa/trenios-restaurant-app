using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Trenios.Mobile.Models.Api;
using Trenios.Mobile.Services;

namespace Trenios.Mobile.ViewModels;

public class KitchenDisplayViewModel : INotifyPropertyChanged
{
    private readonly OrderService _orderService;
    private readonly OrderHubService _orderHubService;
    private readonly AuthService _authService;

    public ObservableCollection<OrderResponse> ActiveOrders { get; } = new();

    private bool _isLoading;
    public bool IsLoading
    {
        get => _isLoading;
        set { _isLoading = value; OnPropertyChanged(); }
    }

    private bool _isConnected;
    public bool IsConnected
    {
        get => _isConnected;
        set { _isConnected = value; OnPropertyChanged(); OnPropertyChanged(nameof(ConnectionStatusText)); OnPropertyChanged(nameof(ConnectionStatusColor)); }
    }

    private string _connectionStatus = "Disconnected";
    public string ConnectionStatus
    {
        get => _connectionStatus;
        set { _connectionStatus = value; OnPropertyChanged(); OnPropertyChanged(nameof(ConnectionStatusText)); }
    }

    public string ConnectionStatusText => IsConnected ? "Live" : ConnectionStatus;
    public Color ConnectionStatusColor => IsConnected ? Colors.Green : Colors.Orange;

    private OrderResponse? _selectedOrder;
    public OrderResponse? SelectedOrder
    {
        get => _selectedOrder;
        set { _selectedOrder = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasSelectedOrder)); }
    }

    public bool HasSelectedOrder => SelectedOrder != null;

    public ICommand RefreshCommand { get; }
    public ICommand SelectOrderCommand { get; }
    public ICommand StartPreparingCommand { get; }
    public ICommand MarkCompletedCommand { get; }
    public ICommand LogoutCommand { get; }
    public ICommand ClosePopupCommand { get; }

    public KitchenDisplayViewModel(OrderService orderService, OrderHubService orderHubService, AuthService authService)
    {
        _orderService = orderService;
        _orderHubService = orderHubService;
        _authService = authService;

        RefreshCommand = new Command(async () => await LoadOrdersAsync());
        SelectOrderCommand = new Command<OrderResponse>(SelectOrder);
        StartPreparingCommand = new Command(async () => await UpdateStatusAsync(OrderStatus.Preparing));
        MarkCompletedCommand = new Command(async () => await UpdateStatusAsync(OrderStatus.Completed));
        LogoutCommand = new Command(async () => await LogoutAsync());
        ClosePopupCommand = new Command(() => SelectedOrder = null);

        // Subscribe to SignalR events
        _orderHubService.OnOrderCreated += HandleOrderCreated;
        _orderHubService.OnOrderStatusUpdated += HandleOrderStatusUpdated;
        _orderHubService.OnConnectionStateChanged += HandleConnectionStateChanged;
        _orderHubService.OnError += HandleHubError;
    }

    public async Task InitializeAsync()
    {
        var branchId = _authService.GetEffectiveBranchId();
        if (branchId.HasValue)
        {
            await _orderHubService.ConnectAsync(branchId.Value);
            await LoadOrdersAsync();
        }
    }

    public async Task LoadOrdersAsync()
    {
        if (IsLoading) return;

        IsLoading = true;
        try
        {
            var branchId = _authService.GetEffectiveBranchId();
            if (!branchId.HasValue) return;

            var result = await _orderService.GetBranchOrdersAsync(branchId.Value);
            if (result.IsSuccess && result.Data != null)
            {
                ActiveOrders.Clear();
                // Show only orders that need kitchen attention (Created, Confirmed, Preparing)
                var kitchenOrders = result.Data
                    .Where(o => o.OrderStatus == OrderStatus.Created ||
                                o.OrderStatus == OrderStatus.Confirmed ||
                                o.OrderStatus == OrderStatus.Preparing)
                    .OrderBy(o => o.PlacedAt)
                    .ToList();

                foreach (var order in kitchenOrders)
                {
                    ActiveOrders.Add(order);
                }
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void SelectOrder(OrderResponse? order)
    {
        SelectedOrder = order;
    }

    private async Task UpdateStatusAsync(OrderStatus newStatus)
    {
        if (SelectedOrder == null) return;

        var result = await _orderService.UpdateOrderStatusAsync(SelectedOrder.Id, newStatus);
        if (result.Success)
        {
            // Close popup and refresh
            SelectedOrder = null;
            await LoadOrdersAsync();
        }
    }

    private void HandleOrderCreated(OrderResponse order)
    {
        // Add new order to the beginning if it's a kitchen-relevant status
        if (order.OrderStatus == OrderStatus.Created ||
            order.OrderStatus == OrderStatus.Confirmed ||
            order.OrderStatus == OrderStatus.Preparing)
        {
            // Insert at position based on PlacedAt to maintain order
            var index = ActiveOrders.ToList().FindIndex(o => o.PlacedAt > order.PlacedAt);
            if (index < 0)
                ActiveOrders.Add(order);
            else
                ActiveOrders.Insert(index, order);
        }
    }

    private void HandleOrderStatusUpdated(OrderResponse order)
    {
        // Find and update existing order
        var existingOrder = ActiveOrders.FirstOrDefault(o => o.Id == order.Id);

        if (order.OrderStatus == OrderStatus.Completed || order.OrderStatus == OrderStatus.Cancelled)
        {
            // Remove completed/cancelled orders
            if (existingOrder != null)
            {
                ActiveOrders.Remove(existingOrder);
                if (SelectedOrder?.Id == order.Id)
                {
                    SelectedOrder = null; // Close popup
                }
            }
        }
        else
        {
            // Update or add the order
            if (existingOrder != null)
            {
                var index = ActiveOrders.IndexOf(existingOrder);
                ActiveOrders[index] = order;
                if (SelectedOrder?.Id == order.Id)
                {
                    SelectedOrder = order;
                }
            }
            else
            {
                HandleOrderCreated(order);
            }
        }
    }

    private void HandleConnectionStateChanged(string status)
    {
        ConnectionStatus = status;
        IsConnected = status == "Connected";
    }

    private void HandleHubError(Exception ex)
    {
        // Could show error to user
        System.Diagnostics.Debug.WriteLine($"SignalR Error: {ex.Message}");
    }

    private async Task LogoutAsync()
    {
        await _authService.LogoutAsync();
        await Shell.Current.GoToAsync("//LoginPage");
    }

    public async Task DisconnectAsync()
    {
        await _orderHubService.DisconnectAsync();
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
