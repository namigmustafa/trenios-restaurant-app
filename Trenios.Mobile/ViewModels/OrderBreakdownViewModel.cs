using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using Trenios.Mobile.Models.Api;
using Trenios.Mobile.Services;

namespace Trenios.Mobile.ViewModels;

public class OrderBreakdownViewModel : INotifyPropertyChanged, IQueryAttributable
{
    private readonly ReportingService _reportingService;
    private readonly AuthService _authService;

    private const int HoursPageSize = 5;

    private bool _isLoading;
    private int _totalOrders;
    private int _cancelledOrders;
    private string _totalRevenueDisplay = string.Empty;
    private ObservableCollection<OrderTypeItem> _orderTypes = new();
    private ObservableCollection<SalesPeriodItem> _timePeriods = new();
    private ObservableCollection<SalesHourItem> _visibleHours = new();
    private List<SalesHourItem> _allHours = new();
    private int _visibleHoursCount = HoursPageSize;
    private bool _hasTimePeriodData;
    private bool _hasHourlyData;

    // Query parameters received from navigation
    private DateTime _startDate = DateTime.Today;
    private DateTime _endDate = DateTime.Today;
    private Guid? _restaurantId;
    private Guid? _branchId;

    public event PropertyChangedEventHandler? PropertyChanged;

    // ── Properties ─────────────────────────────────────────────────────────────

    public bool IsLoading
    {
        get => _isLoading;
        set { _isLoading = value; OnPropertyChanged(nameof(IsLoading)); }
    }

    public int TotalOrders
    {
        get => _totalOrders;
        set { _totalOrders = value; OnPropertyChanged(nameof(TotalOrders)); OnPropertyChanged(nameof(TotalOrdersBadge)); }
    }

    public int CancelledOrders
    {
        get => _cancelledOrders;
        set { _cancelledOrders = value; OnPropertyChanged(nameof(CancelledOrders)); }
    }

    public string TotalRevenueDisplay
    {
        get => _totalRevenueDisplay;
        set { _totalRevenueDisplay = value; OnPropertyChanged(nameof(TotalRevenueDisplay)); }
    }

    public ObservableCollection<OrderTypeItem> OrderTypes
    {
        get => _orderTypes;
        set { _orderTypes = value; OnPropertyChanged(nameof(OrderTypes)); }
    }

    public ObservableCollection<SalesPeriodItem> TimePeriods
    {
        get => _timePeriods;
        set { _timePeriods = value; OnPropertyChanged(nameof(TimePeriods)); }
    }

    public ObservableCollection<SalesHourItem> VisibleHours
    {
        get => _visibleHours;
        set { _visibleHours = value; OnPropertyChanged(nameof(VisibleHours)); }
    }

    public bool HasTimePeriodData
    {
        get => _hasTimePeriodData;
        set { _hasTimePeriodData = value; OnPropertyChanged(nameof(HasTimePeriodData)); }
    }

    public bool HasHourlyData
    {
        get => _hasHourlyData;
        set { _hasHourlyData = value; OnPropertyChanged(nameof(HasHourlyData)); }
    }

    public bool CanShowMoreHours => _visibleHoursCount < _allHours.Count;

    public string TotalOrdersBadge => $"{TotalOrders} {LocalizationService.Instance.Get("Orders")}";

    // ── Commands ───────────────────────────────────────────────────────────────

    public ICommand BackCommand { get; }
    public ICommand ShowMoreHoursCommand { get; }
    public ICommand LogoutCommand { get; }

    public OrderBreakdownViewModel(ReportingService reportingService, AuthService authService)
    {
        _reportingService = reportingService;
        _authService = authService;

        BackCommand = new Command(async () => await Shell.Current.GoToAsync(".."));
        ShowMoreHoursCommand = new Command(ShowMoreHours);
        LogoutCommand = new Command(async () => await _authService.LogoutAsync());

        LocalizationService.Instance.OnLanguageChanged += () => OnPropertyChanged(nameof(TotalOrdersBadge));
    }

    // ── IQueryAttributable ─────────────────────────────────────────────────────

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("startDate", out var s) &&
            DateTime.TryParse(s?.ToString(), null, System.Globalization.DateTimeStyles.RoundtripKind, out var start))
            _startDate = start;

        if (query.TryGetValue("endDate", out var e) &&
            DateTime.TryParse(e?.ToString(), null, System.Globalization.DateTimeStyles.RoundtripKind, out var end))
            _endDate = end;

        if (query.TryGetValue("restaurantId", out var r) &&
            Guid.TryParse(r?.ToString(), out var rid) && rid != Guid.Empty)
            _restaurantId = rid;
        else
            _restaurantId = null;

        if (query.TryGetValue("branchId", out var b) &&
            Guid.TryParse(b?.ToString(), out var bid) && bid != Guid.Empty)
            _branchId = bid;
        else
            _branchId = null;

        _ = LoadAsync();
    }

    // ── Data loading ───────────────────────────────────────────────────────────

    private async Task LoadAsync()
    {
        if (IsLoading) return;

        IsLoading = true;
        try
        {
            var timezone = TimeZoneInfo.Local.Id;

            var salesTask = _reportingService.GetSalesSummaryAsync(
                _startDate, _endDate, _restaurantId, _branchId);
            var breakdownTask = _reportingService.GetOrderTypeBreakdownAsync(
                _startDate, _endDate, _restaurantId, _branchId);
            var timePeriodTask = _reportingService.GetSalesByTimePeriodAsync(
                _startDate, _endDate, _restaurantId, _branchId, timezone: timezone);
            var hourlyTask = _reportingService.GetSalesByHourAsync(
                _startDate, _endDate, _restaurantId, _branchId, timezone: timezone);

            await Task.WhenAll(salesTask, breakdownTask, timePeriodTask, hourlyTask);

            var salesResult = await salesTask;
            var breakdownResult = await breakdownTask;
            var timePeriodResult = await timePeriodTask;
            var hourlyResult = await hourlyTask;

            if (salesResult.IsSuccess && salesResult.Data != null)
            {
                TotalOrders = salesResult.Data.TotalOrders;
                CancelledOrders = salesResult.Data.CancelledOrders;
                TotalRevenueDisplay = salesResult.Data.TotalRevenueDisplay;
            }

            if (breakdownResult.IsSuccess && breakdownResult.Data != null)
                OrderTypes = new ObservableCollection<OrderTypeItem>(breakdownResult.Data.OrderTypes);

            if (timePeriodResult.IsSuccess && timePeriodResult.Data != null)
            {
                TimePeriods = new ObservableCollection<SalesPeriodItem>(timePeriodResult.Data.Periods);
                HasTimePeriodData = TimePeriods.Count > 0;
            }

            if (hourlyResult.IsSuccess && hourlyResult.Data != null)
            {
                _allHours = hourlyResult.Data.Hours;
                _visibleHoursCount = HoursPageSize;
                RefreshVisibleHours();
                HasHourlyData = _allHours.Count > 0;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[OrderBreakdownViewModel] LoadAsync error: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void ShowMoreHours()
    {
        _visibleHoursCount = Math.Min(_visibleHoursCount + HoursPageSize, _allHours.Count);
        RefreshVisibleHours();
    }

    private void RefreshVisibleHours()
    {
        VisibleHours = new ObservableCollection<SalesHourItem>(_allHours.Take(_visibleHoursCount));
        OnPropertyChanged(nameof(CanShowMoreHours));
    }

    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
