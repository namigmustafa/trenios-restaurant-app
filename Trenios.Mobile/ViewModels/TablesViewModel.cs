using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using Trenios.Mobile.Helpers;
using Trenios.Mobile.Models.Api;
using Trenios.Mobile.Services;

namespace Trenios.Mobile.ViewModels;

public class TablesViewModel : INotifyPropertyChanged
{
    private readonly TableService _tableService;
    private readonly AuthService _authService;
    private readonly OrderService _orderService;

    private bool _isLoading;
    private bool _isRefreshing;
    private TableWithReservationDto? _selectedTable;
    private bool _showMoveTableDialog;
    private TableWithReservationDto? _targetTable;
    private string _statusFilter = "All";

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<TableWithReservationDto> Tables { get; } = new();

    private List<TableWithReservationDto> _availableTables = new();
    public List<TableWithReservationDto> AvailableTables
    {
        get => _availableTables;
        private set { _availableTables = value; OnPropertyChanged(nameof(AvailableTables)); }
    }

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

    public TableWithReservationDto? SelectedTable
    {
        get => _selectedTable;
        set
        {
            _selectedTable = value;
            OnPropertyChanged(nameof(SelectedTable));
            OnPropertyChanged(nameof(HasSelectedTable));
            OnPropertyChanged(nameof(CanCheckout));
            OnPropertyChanged(nameof(CanMoveTable));
            OnPropertyChanged(nameof(CanRelease));
        }
    }

    public bool HasSelectedTable => SelectedTable != null;
    public bool CanCheckout => SelectedTable?.IsReserved == true;
    public bool CanMoveTable => SelectedTable?.IsReserved == true;
    public bool CanRelease => SelectedTable?.IsReserved == true;

    public string RestaurantName => _authService.GetEffectiveRestaurantName() ?? "";
    public string BranchName => _authService.GetEffectiveBranchName() ?? "";
    public string? RestaurantLogoUrl => _authService.SelectedRestaurant?.DisplayImageUrl
        ?? _authService.CurrentUser?.Restaurant?.DisplayImageUrl;
    public bool HasRestaurantLogo => !string.IsNullOrEmpty(RestaurantLogoUrl);

    public int AvailableCount => Tables.Count(t => !t.IsReserved);
    public int OccupiedCount => Tables.Count(t => t.IsReserved);

    public string StatusFilter
    {
        get => _statusFilter;
        set
        {
            _statusFilter = value;
            OnPropertyChanged(nameof(StatusFilter));
            OnPropertyChanged(nameof(FilterAll));
            OnPropertyChanged(nameof(FilterAvailable));
            OnPropertyChanged(nameof(FilterOccupied));
            UpdateFilteredTables();
        }
    }

    public bool FilterAll => StatusFilter == "All";
    public bool FilterAvailable => StatusFilter == "Available";
    public bool FilterOccupied => StatusFilter == "Occupied";

    public ObservableCollection<TableWithReservationDto> FilteredTables { get; } = new();

    public bool ShowMoveTableDialog
    {
        get => _showMoveTableDialog;
        set { _showMoveTableDialog = value; OnPropertyChanged(nameof(ShowMoveTableDialog)); }
    }

    public TableWithReservationDto? TargetTable
    {
        get => _targetTable;
        set { _targetTable = value; OnPropertyChanged(nameof(TargetTable)); }
    }

    public ICommand LoadTablesCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand SelectTableCommand { get; }
    public ICommand CloseDetailsCommand { get; }
    public ICommand CheckoutCommand { get; }
    public ICommand ShowMoveDialogCommand { get; }
    public ICommand CloseMoveDialogCommand { get; }
    public ICommand SelectTargetTableCommand { get; }
    public ICommand ConfirmMoveCommand { get; }
    public ICommand ReleaseCommand { get; }
    public ICommand LogoutCommand { get; }
    public ICommand SetStatusFilterCommand { get; }
    public ICommand CancelOrderCommand { get; }

    public TablesViewModel(TableService tableService, AuthService authService, OrderService orderService)
    {
        _tableService = tableService;
        _authService = authService;
        _orderService = orderService;

        LoadTablesCommand = new Command(async () => await LoadTablesAsync());
        RefreshCommand = new Command(async () => await RefreshAsync());
        SelectTableCommand = new Command<TableWithReservationDto>(async (t) => await SelectTableAsync(t));
        CloseDetailsCommand = new Command(() => SelectedTable = null);
        CheckoutCommand = new Command(async () => await CheckoutAsync());
        ShowMoveDialogCommand = new Command(ShowMoveDialogAsync);
        CloseMoveDialogCommand = new Command(CloseMoveDialog);
        SelectTargetTableCommand = new Command<TableWithReservationDto>(SelectTargetTable);
        ConfirmMoveCommand = new Command(async () => await ConfirmMoveAsync());
        ReleaseCommand = new Command(async () => await ReleaseAsync());
        LogoutCommand = new Command(async () => await LogoutAsync());
        SetStatusFilterCommand = new Command<string>(filter => StatusFilter = filter ?? "All");
        CancelOrderCommand = new Command<TableOrderSummaryDto>(async (order) => await CancelOrderAsync(order));
    }

    public async Task LoadTablesAsync()
    {
        var branchId = _authService.GetEffectiveBranchId();
        if (branchId == null) return;

        IsLoading = true;

        try
        {
            var (tables, error) = await _tableService.GetTablesWithReservationsAsync(branchId.Value);

            if (tables != null)
            {
                Tables.Clear();
                // Sort: reserved tables first, then by table number
                foreach (var table in tables.OrderByDescending(t => t.IsReserved).ThenBy(t => t.Number))
                {
                    Tables.Add(table);
                }
                UpdateFilteredTables();
                OnPropertyChanged(nameof(AvailableCount));
                OnPropertyChanged(nameof(OccupiedCount));
            }
            else if (!string.IsNullOrEmpty(error))
            {
                var loc = LocalizationService.Instance;
                await ShowAlertAsync(loc["Error"], error, loc["OK"]);
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
        await LoadTablesAsync();
        IsRefreshing = false;
    }

    private async Task SelectTableAsync(TableWithReservationDto? table)
    {
        SelectedTable = table;

        if (table?.IsReserved == true && table.CurrentReservation?.Orders?.Count > 0)
        {
            await LoadOrderItemsAsync(table);
        }
    }

    private async Task LoadOrderItemsAsync(TableWithReservationDto table)
    {
        var branchId = _authService.GetEffectiveBranchId();
        if (branchId == null) return;

        var result = await _orderService.GetBranchOrdersAsync(branchId.Value);

        if (result.IsSuccess && result.Data != null)
        {
            var orderLookup = result.Data.ToDictionary(o => o.Id);

            foreach (var orderSummary in table.CurrentReservation!.Orders)
            {
                if (orderLookup.TryGetValue(orderSummary.Id, out var fullOrder))
                {
                    orderSummary.Items = fullOrder.Items;
                }
            }

            // Force UI rebind by re-setting SelectedTable
            SelectedTable = null;
            SelectedTable = table;
        }
    }

    private async Task CheckoutAsync()
    {
        if (SelectedTable == null || !SelectedTable.IsReserved) return;

        var loc = LocalizationService.Instance;

        // Confirm checkout
        var confirm = await ShowAlertAsync(
            loc["Checkout"],
            loc["ConfirmCheckout"],
            loc["Confirm"],
            loc["Cancel"]
        );

        if (confirm != true) return;

        IsLoading = true;

        try
        {
            var (response, error) = await _tableService.CheckoutTableAsync(SelectedTable.Id);

            if (response != null)
            {
                await ShowAlertAsync(
                    loc["Checkout"],
                    $"{loc["CheckoutSuccess"]}\n{loc["TotalAmount"]}: {CurrencyFormatter.Format(response.TotalAmount)}",
                    loc["OK"]
                );

                SelectedTable = null;
                await LoadTablesAsync();
            }
            else if (!string.IsNullOrEmpty(error))
            {
                await ShowAlertAsync(loc["Error"], error, loc["OK"]);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Checkout] Error: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void ShowMoveDialogAsync()
    {
        if (SelectedTable == null || !SelectedTable.IsReserved) return;

        AvailableTables = Tables
            .Where(t => !t.IsReserved && t.Id != SelectedTable.Id)
            .OrderBy(t => t.Number)
            .ToList();

        TargetTable = null;
        ShowMoveTableDialog = true;
    }

    private void CloseMoveDialog()
    {
        ShowMoveTableDialog = false;
        TargetTable = null;
    }

    private void SelectTargetTable(TableWithReservationDto? table)
    {
        TargetTable = table;
    }

    private async Task ConfirmMoveAsync()
    {
        if (SelectedTable == null || TargetTable == null) return;

        var loc = LocalizationService.Instance;

        IsLoading = true;

        try
        {
            var (reservation, error) = await _tableService.MoveTableAsync(SelectedTable.Id, TargetTable.Id);

            if (reservation != null)
            {
                await ShowAlertAsync(
                    loc["MoveTable"],
                    loc["MoveSuccess"],
                    loc["OK"]
                );

                ShowMoveTableDialog = false;
                TargetTable = null;
                SelectedTable = null;
                await LoadTablesAsync();
            }
            else if (!string.IsNullOrEmpty(error))
            {
                await ShowAlertAsync(loc["Error"], error, loc["OK"]);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ConfirmMove] Error: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task ReleaseAsync()
    {
        if (SelectedTable == null || !SelectedTable.IsReserved) return;

        var loc = LocalizationService.Instance;

        // Confirm release
        var confirm = await ShowAlertAsync(
            loc["Release"],
            loc["ConfirmRelease"],
            loc["Confirm"],
            loc["Cancel"]
        );

        if (confirm != true) return;

        // Prompt for reason
        var reason = await ShowPromptAsync(
            loc["Release"],
            loc["EnterReleaseReason"],
            loc["Confirm"],
            loc["Cancel"],
            placeholder: loc["ReleasePlaceholder"]
        );

        // If user cancelled the prompt, don't proceed
        if (reason == null) return;

        IsLoading = true;

        try
        {
            var (reservation, error) = await _tableService.ReleaseTableAsync(SelectedTable.Id, reason);

            if (reservation != null)
            {
                await ShowAlertAsync(
                    loc["Release"],
                    loc["ReleaseSuccess"],
                    loc["OK"]
                );

                SelectedTable = null;
                await LoadTablesAsync();
            }
            else if (!string.IsNullOrEmpty(error))
            {
                await ShowAlertAsync(loc["Error"], error, loc["OK"]);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Release] Error: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task CancelOrderAsync(TableOrderSummaryDto? order)
    {
        if (order == null || SelectedTable == null) return;

        var loc = LocalizationService.Instance;

        var confirm = await ShowAlertAsync(
            loc["CancelOrderTitle"],
            loc["ConfirmCancelOrder"],
            loc["Confirm"],
            loc["Cancel"]
        );

        if (confirm != true) return;

        var reason = await ShowPromptAsync(
            loc["CancelOrderTitle"],
            loc["EnterCancellationReason"],
            loc["Confirm"],
            loc["Cancel"],
            placeholder: loc["CancellationPlaceholder"]
        );

        if (reason == null) return;

        IsLoading = true;

        try
        {
            var (success, error) = await _orderService.UpdateOrderStatusAsync(order.Id, OrderStatus.Cancelled, reason);

            if (success)
            {
                // Check if this was the last non-cancelled order
                var activeOrders = SelectedTable.CurrentReservation?.Orders
                    .Where(o => o.Id != order.Id && (OrderStatus)o.Status != OrderStatus.Cancelled)
                    .ToList();

                if (activeOrders == null || activeOrders.Count == 0)
                {
                    // Last order — release the table silently
                    await _tableService.ReleaseTableAsync(SelectedTable.Id);
                    SelectedTable = null;
                }

                await LoadTablesAsync();
            }
            else if (!string.IsNullOrEmpty(error))
            {
                await ShowAlertAsync(loc["Error"], error, loc["OK"]);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[CancelOrder] Error: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LogoutAsync()
    {
        await _authService.LogoutAsync();
        await Shell.Current.GoToAsync("//LoginPage");
    }

    private void UpdateFilteredTables()
    {
        FilteredTables.Clear();
        var source = StatusFilter switch
        {
            "Available" => Tables.Where(t => !t.IsReserved),
            "Occupied" => Tables.Where(t => t.IsReserved),
            _ => Tables.AsEnumerable()
        };
        foreach (var t in source)
            FilteredTables.Add(t);
    }

    private static Task ShowAlertAsync(string title, string message, string cancel)
        => Shell.Current?.DisplayAlert(title, message, cancel) ?? Task.CompletedTask;

    private static Task<bool> ShowAlertAsync(string title, string message, string accept, string cancel)
        => Shell.Current?.DisplayAlert(title, message, accept, cancel) ?? Task.FromResult(false);

    private static Task<string?> ShowPromptAsync(string title, string message, string accept, string cancel, string? placeholder = null)
        => Shell.Current?.DisplayPromptAsync(title, message, accept, cancel, placeholder: placeholder) ?? Task.FromResult<string?>(null);

    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
