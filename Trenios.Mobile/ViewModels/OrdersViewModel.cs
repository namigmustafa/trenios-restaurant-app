using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using Trenios.Mobile.Models.Api;
using Trenios.Mobile.Services;

namespace Trenios.Mobile.ViewModels;

public class OrdersViewModel : INotifyPropertyChanged
{
    private readonly ApiService _apiService;
    private readonly AuthService _authService;

    private bool _isLoading;
    private bool _isRefreshing;
    private OrderResponse? _selectedOrder;
    private OrderStatus? _statusFilter;

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<OrderResponse> Orders { get; } = new();

    public bool IsLoading
    {
        get => _isLoading;
        set { _isLoading = value; OnPropertyChanged(nameof(IsLoading)); }
    }

    public bool IsRefreshing
    {
        get => _isRefreshing;
        set { _isRefreshing = value; OnPropertyChanged(nameof(IsRefreshing)); }
    }

    public OrderResponse? SelectedOrder
    {
        get => _selectedOrder;
        set
        {
            _selectedOrder = value;
            OnPropertyChanged(nameof(SelectedOrder));
            OnPropertyChanged(nameof(HasSelectedOrder));
        }
    }

    public bool HasSelectedOrder => SelectedOrder != null;

    public OrderStatus? StatusFilter
    {
        get => _statusFilter;
        set
        {
            _statusFilter = value;
            OnPropertyChanged(nameof(StatusFilter));
            OnPropertyChanged(nameof(FilterAll));
            OnPropertyChanged(nameof(FilterCreated));
            OnPropertyChanged(nameof(FilterConfirmed));
            OnPropertyChanged(nameof(FilterPreparing));
            OnPropertyChanged(nameof(FilterCompleted));
            _ = LoadOrdersAsync();
        }
    }

    public bool FilterAll => StatusFilter == null;
    public bool FilterCreated => StatusFilter == OrderStatus.Created;
    public bool FilterConfirmed => StatusFilter == OrderStatus.Confirmed;
    public bool FilterPreparing => StatusFilter == OrderStatus.Preparing;
    public bool FilterCompleted => StatusFilter == OrderStatus.Completed;

    public ICommand LoadOrdersCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand SelectOrderCommand { get; }
    public ICommand CloseDetailsCommand { get; }
    public ICommand SetStatusFilterCommand { get; }
    public ICommand UpdateStatusCommand { get; }
    public ICommand BackCommand { get; }

    public OrdersViewModel(ApiService apiService, AuthService authService)
    {
        _apiService = apiService;
        _authService = authService;

        LoadOrdersCommand = new Command(async () => await LoadOrdersAsync());
        RefreshCommand = new Command(async () => await RefreshAsync());
        SelectOrderCommand = new Command<OrderResponse>(SelectOrder);
        CloseDetailsCommand = new Command(() => SelectedOrder = null);
        SetStatusFilterCommand = new Command<string>(SetStatusFilter);
        UpdateStatusCommand = new Command<string>(async (status) => await UpdateOrderStatusAsync(status));
        BackCommand = new Command(async () => await Shell.Current.GoToAsync(".."));
    }

    public async Task LoadOrdersAsync()
    {
        var branchId = _authService.GetEffectiveBranchId();
        if (branchId == null) return;

        IsLoading = true;

        try
        {
            var url = $"/api/orders?branchId={branchId}";
            if (StatusFilter.HasValue)
            {
                url += $"&status={(int)StatusFilter.Value}";
            }

            var result = await _apiService.GetAsync<List<OrderResponse>>(url);

            if (result.IsSuccess && result.Data != null)
            {
                Orders.Clear();
                foreach (var order in result.Data.OrderByDescending(o => o.PlacedAt))
                {
                    Orders.Add(order);
                }
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task RefreshAsync()
    {
        IsRefreshing = true;
        await LoadOrdersAsync();
        IsRefreshing = false;
    }

    private void SelectOrder(OrderResponse? order)
    {
        SelectedOrder = order;
    }

    private void SetStatusFilter(string? status)
    {
        if (string.IsNullOrEmpty(status) || status == "All")
        {
            StatusFilter = null;
        }
        else if (Enum.TryParse<OrderStatus>(status, out var parsed))
        {
            StatusFilter = parsed;
        }
    }

    private async Task UpdateOrderStatusAsync(string statusStr)
    {
        if (SelectedOrder == null) return;
        if (!Enum.TryParse<OrderStatus>(statusStr, out var newStatus)) return;

        var request = new UpdateOrderStatusRequest
        {
            Status = (int)newStatus
        };

        var result = await _apiService.PatchAsync<object>($"/api/orders/{SelectedOrder.Id}/status", request);

        if (result.IsSuccess)
        {
            // Reload orders to get updated data
            await LoadOrdersAsync();

            // Update selected order
            SelectedOrder = Orders.FirstOrDefault(o => o.Id == SelectedOrder?.Id);
        }
    }

    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
