using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
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

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<TableWithReservationDto> Tables { get; } = new();
    public ObservableCollection<TableWithReservationDto> AvailableTables { get; } = new();

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
    public ICommand BackCommand { get; }

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
        ShowMoveDialogCommand = new Command(async () => await ShowMoveDialogAsync());
        CloseMoveDialogCommand = new Command(CloseMoveDialog);
        SelectTargetTableCommand = new Command<TableWithReservationDto>(SelectTargetTable);
        ConfirmMoveCommand = new Command(async () => await ConfirmMoveAsync());
        ReleaseCommand = new Command(async () => await ReleaseAsync());
        BackCommand = new Command(async () => await Shell.Current.GoToAsync(".."));
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
            }
            else if (!string.IsNullOrEmpty(error))
            {
                var loc = LocalizationService.Instance;
                await Application.Current?.MainPage?.DisplayAlert(loc["Error"], error, loc["OK"]);
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
        var confirm = await Application.Current?.MainPage?.DisplayAlert(
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
                await Application.Current?.MainPage?.DisplayAlert(
                    loc["Checkout"],
                    $"{loc["CheckoutSuccess"]}\n{loc["TotalAmount"]}: €{response.TotalAmount:F2}",
                    loc["OK"]
                );

                SelectedTable = null;
                await LoadTablesAsync();
            }
            else if (!string.IsNullOrEmpty(error))
            {
                await Application.Current?.MainPage?.DisplayAlert(loc["Error"], error, loc["OK"]);
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task ShowMoveDialogAsync()
    {
        if (SelectedTable == null || !SelectedTable.IsReserved) return;

        var branchId = _authService.GetEffectiveBranchId();
        if (branchId == null) return;

        IsLoading = true;

        try
        {
            var (tables, error) = await _tableService.GetAvailableTablesAsync(branchId.Value);

            if (tables != null)
            {
                AvailableTables.Clear();
                foreach (var table in tables.OrderBy(t => t.Number))
                {
                    AvailableTables.Add(table);
                }

                TargetTable = null;
                ShowMoveTableDialog = true;
            }
            else if (!string.IsNullOrEmpty(error))
            {
                var loc = LocalizationService.Instance;
                await Application.Current?.MainPage?.DisplayAlert(loc["Error"], error, loc["OK"]);
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void CloseMoveDialog()
    {
        ShowMoveTableDialog = false;
        TargetTable = null;
    }

    private void SelectTargetTable(TableWithReservationDto? table)
    {
        // Clear previous selection
        if (TargetTable != null)
            TargetTable.IsSelectedTarget = false;

        TargetTable = table;

        // Mark new selection
        if (TargetTable != null)
            TargetTable.IsSelectedTarget = true;

        // Refresh list to update visual state
        var items = AvailableTables.ToList();
        AvailableTables.Clear();
        foreach (var t in items)
            AvailableTables.Add(t);
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
                await Application.Current?.MainPage?.DisplayAlert(
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
                await Application.Current?.MainPage?.DisplayAlert(loc["Error"], error, loc["OK"]);
            }
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
        var confirm = await Application.Current?.MainPage?.DisplayAlert(
            loc["Release"],
            loc["ConfirmRelease"],
            loc["Confirm"],
            loc["Cancel"]
        );

        if (confirm != true) return;

        // Prompt for reason
        var reason = await Application.Current?.MainPage?.DisplayPromptAsync(
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
                await Application.Current?.MainPage?.DisplayAlert(
                    loc["Release"],
                    loc["ReleaseSuccess"],
                    loc["OK"]
                );

                SelectedTable = null;
                await LoadTablesAsync();
            }
            else if (!string.IsNullOrEmpty(error))
            {
                await Application.Current?.MainPage?.DisplayAlert(loc["Error"], error, loc["OK"]);
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
