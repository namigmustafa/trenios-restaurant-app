using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using Trenios.Mobile.Models.Api;
using Trenios.Mobile.Services;

namespace Trenios.Mobile.ViewModels;

public class ReportingViewModel : INotifyPropertyChanged
{
    private readonly ReportingService _reportingService;
    private readonly AuthService _authService;

    private const int PageSize = 5;

    private bool _isLoading;
    private bool _hasData;
    private bool _showTodayOnly = true;
    private DateTime _startDate = DateTime.Today;
    private DateTime _endDate = DateTime.Today;
    private SalesSummaryReport? _salesSummary;
    private List<RestaurantDto> _restaurants = new();
    private List<BranchDto> _branches = new();
    private RestaurantDto? _selectedRestaurant;
    private BranchDto? _selectedBranch;

    // Top selling
    private List<TopSellingItem> _allTopProducts = new();
    private ObservableCollection<TopSellingItem> _visibleTopProducts = new();
    private int _visibleTopProductsCount = PageSize;
    private bool _hasTopProductsData;

    // Sales trends
    private List<SalesTrendDataPoint> _allSalesTrends = new();
    private ObservableCollection<SalesTrendDataPoint> _visibleSalesTrends = new();
    private int _visibleSalesTrendsCount = PageSize;
    private bool _hasSalesTrendsData;
    private SalesTrendsReport? _salesTrendsReport;

    public event PropertyChangedEventHandler? PropertyChanged;

    // ── Core state ─────────────────────────────────────────────────────────────

    public bool IsLoading
    {
        get => _isLoading;
        set { _isLoading = value; OnPropertyChanged(nameof(IsLoading)); }
    }

    public bool HasData
    {
        get => _hasData;
        set
        {
            _hasData = value;
            OnPropertyChanged(nameof(HasData));
            (ViewOrderBreakdownCommand as Command)?.ChangeCanExecute();
        }
    }

    public bool ShowTodayOnly
    {
        get => _showTodayOnly;
        set
        {
            _showTodayOnly = value;
            OnPropertyChanged(nameof(ShowTodayOnly));
            OnPropertyChanged(nameof(ShowDatePickers));
            OnPropertyChanged(nameof(TodayDisplay));

            if (value)
            {
                StartDate = DateTime.Today;
                EndDate = DateTime.Today;
            }
        }
    }

    public DateTime StartDate
    {
        get => _startDate;
        set { _startDate = value; OnPropertyChanged(nameof(StartDate)); OnPropertyChanged(nameof(TodayDisplay)); }
    }

    public DateTime EndDate
    {
        get => _endDate;
        set { _endDate = value; OnPropertyChanged(nameof(EndDate)); }
    }

    public SalesSummaryReport? SalesSummary
    {
        get => _salesSummary;
        set { _salesSummary = value; OnPropertyChanged(nameof(SalesSummary)); OnPropertyChanged(nameof(PerformanceInsight)); OnPropertyChanged(nameof(InsightIcon)); }
    }

    public List<RestaurantDto> Restaurants
    {
        get => _restaurants;
        set { _restaurants = value; OnPropertyChanged(nameof(Restaurants)); }
    }

    public List<BranchDto> Branches
    {
        get => _branches;
        set { _branches = value; OnPropertyChanged(nameof(Branches)); }
    }

    public RestaurantDto? SelectedRestaurant
    {
        get => _selectedRestaurant;
        set
        {
            _selectedRestaurant = value;
            OnPropertyChanged(nameof(SelectedRestaurant));
            OnPropertyChanged(nameof(SelectedRestaurantName));
        }
    }

    public BranchDto? SelectedBranch
    {
        get => _selectedBranch;
        set
        {
            _selectedBranch = value;
            OnPropertyChanged(nameof(SelectedBranch));
            OnPropertyChanged(nameof(SelectedBranchName));
        }
    }

    // ── Top selling ────────────────────────────────────────────────────────────

    public ObservableCollection<TopSellingItem> VisibleTopProducts
    {
        get => _visibleTopProducts;
        set { _visibleTopProducts = value; OnPropertyChanged(nameof(VisibleTopProducts)); }
    }

    public bool HasTopProductsData
    {
        get => _hasTopProductsData;
        set { _hasTopProductsData = value; OnPropertyChanged(nameof(HasTopProductsData)); }
    }

    public bool CanShowMoreTopProducts => _visibleTopProductsCount < _allTopProducts.Count;

    // ── Sales trends ───────────────────────────────────────────────────────────

    public ObservableCollection<SalesTrendDataPoint> VisibleSalesTrends
    {
        get => _visibleSalesTrends;
        set { _visibleSalesTrends = value; OnPropertyChanged(nameof(VisibleSalesTrends)); }
    }

    public bool HasSalesTrendsData
    {
        get => _hasSalesTrendsData;
        set { _hasSalesTrendsData = value; OnPropertyChanged(nameof(HasSalesTrendsData)); }
    }

    public bool CanShowMoreSalesTrends => _visibleSalesTrendsCount < _allSalesTrends.Count;

    public string TrendPercentDisplay => _salesTrendsReport?.TrendPercentDisplay ?? string.Empty;
    public Color TrendPercentColor => _salesTrendsReport?.TrendPercentColor ?? Color.FromRgb(39, 174, 96);
    public string TrendAverageDisplay => _salesTrendsReport?.AveragePerPeriodDisplay ?? string.Empty;

    // ── Computed role-based properties ─────────────────────────────────────────

    public bool IsAuthorized => UserRole != UserRole.Cashier;
    public bool IsSuperAdmin => UserRole == UserRole.SuperAdmin;
    public bool IsRestaurantOwner => UserRole == UserRole.RestaurantOwner;
    public bool ShowRestaurantFilter => IsSuperAdmin;
    public bool ShowBranchFilter => IsSuperAdmin || IsRestaurantOwner;
    public bool ShowDatePickers => !ShowTodayOnly;

    private UserRole UserRole => _authService.CurrentUser?.UserRole ?? UserRole.Cashier;

    public string ContextLabel => UserRole switch
    {
        UserRole.BranchManager => "BRANCH",
        UserRole.RestaurantOwner => "RESTAURANT",
        _ => "ADMIN"
    };

    public string ContextName => UserRole switch
    {
        UserRole.BranchManager => _authService.GetEffectiveBranchName() ?? "Branch",
        UserRole.RestaurantOwner => _authService.GetEffectiveRestaurantName() ?? "Restaurant",
        _ => LocalizationService.Instance.Get("AllRestaurants")
    };

    public string SelectedRestaurantName => SelectedRestaurant?.Name ?? LocalizationService.Instance.Get("AllRestaurants");
    public string SelectedBranchName => SelectedBranch?.Name ?? LocalizationService.Instance.Get("AllBranches");

    public string TodayDisplay => StartDate.ToString("MMMM dd, yyyy");

    // ── Insight properties ─────────────────────────────────────────────────────

    public string PerformanceInsight
    {
        get
        {
            if (SalesSummary?.PreviousPeriod == null)
                return "Load a report to see performance insights.";

            var prev = SalesSummary.PreviousPeriod;
            var revenueDir = prev.RevenueChangePercent >= 0 ? "increased" : "decreased";
            var ordersDir = prev.OrdersChangePercent >= 0 ? "increased" : "decreased";
            var revenueAbs = Math.Abs(prev.RevenueChangePercent);
            var ordersAbs = Math.Abs(prev.OrdersChangePercent);

            return $"Revenue {revenueDir} by {revenueAbs:F1}% vs. prior period. " +
                   $"Total orders {ordersDir} by {ordersAbs:F1}%. " +
                   $"Completion rate stands at {SalesSummary.CompletionRateDisplay}.";
        }
    }

    public string InsightIcon => SalesSummary?.PreviousPeriod?.RevenueChangePercent >= 0 ? "📈" : "📉";

    // ── Commands ───────────────────────────────────────────────────────────────

    public ICommand LoadReportCommand { get; }
    public ICommand ToggleTodayCommand { get; }
    public ICommand SelectRestaurantCommand { get; }
    public ICommand SelectBranchCommand { get; }
    public ICommand LogoutCommand { get; }
    public ICommand ViewOrderBreakdownCommand { get; }
    public ICommand ShowMoreTopProductsCommand { get; }
    public ICommand ShowMoreSalesTrendsCommand { get; }

    public ReportingViewModel(ReportingService reportingService, AuthService authService)
    {
        _reportingService = reportingService;
        _authService = authService;

        LoadReportCommand = new Command(async () => await LoadReportAsync());
        ToggleTodayCommand = new Command(() => ShowTodayOnly = !ShowTodayOnly);
        SelectRestaurantCommand = new Command(async () => await SelectRestaurantAsync());
        SelectBranchCommand = new Command(async () => await SelectBranchAsync());
        LogoutCommand = new Command(async () => await LogoutAsync());
        ViewOrderBreakdownCommand = new Command(async () => await NavigateToOrderBreakdownAsync(), () => HasData);
        ShowMoreTopProductsCommand = new Command(ShowMoreTopProducts);
        ShowMoreSalesTrendsCommand = new Command(ShowMoreSalesTrends);

        LocalizationService.Instance.OnLanguageChanged += () =>
        {
            OnPropertyChanged(nameof(ContextName));
            OnPropertyChanged(nameof(SelectedRestaurantName));
            OnPropertyChanged(nameof(SelectedBranchName));
        };

        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        if (IsSuperAdmin)
        {
            await LoadRestaurantsAsync();
        }
        else if (IsRestaurantOwner)
        {
            var restaurantId = _authService.GetEffectiveRestaurantId();
            if (restaurantId.HasValue)
                await LoadBranchesAsync(restaurantId.Value);
        }
    }

    private async Task LoadRestaurantsAsync()
    {
        var result = await _reportingService.GetRestaurantsAsync();
        if (result.IsSuccess && result.Data != null)
            Restaurants = result.Data;
    }

    private async Task LoadBranchesAsync(Guid restaurantId)
    {
        var result = await _reportingService.GetBranchesAsync(restaurantId);
        if (result.IsSuccess && result.Data != null)
            Branches = result.Data;
    }

    private static (DateTime start, DateTime end) BuildUtcDateRange(DateTime localStart, DateTime localEnd)
    {
        var start = new DateTime(localStart.Year, localStart.Month, localStart.Day, 0, 0, 0, DateTimeKind.Utc);
        var end   = new DateTime(localEnd.Year,   localEnd.Month,   localEnd.Day,   0, 0, 0, DateTimeKind.Utc).AddDays(1);
        return (start, end);
    }

    private (DateTime start, DateTime end, Guid? restaurantId, Guid? branchId) GetEffectiveParams()
    {
        var localStart = ShowTodayOnly ? DateTime.Today : StartDate;
        var localEnd   = ShowTodayOnly ? DateTime.Today : EndDate;
        var (start, end) = BuildUtcDateRange(localStart, localEnd);

        Guid? effectiveRestaurantId = null;
        Guid? effectiveBranchId = null;

        switch (UserRole)
        {
            case UserRole.SuperAdmin:
                effectiveRestaurantId = SelectedRestaurant?.Id;
                effectiveBranchId = SelectedBranch?.Id;
                break;

            case UserRole.RestaurantOwner:
                effectiveRestaurantId = _authService.GetEffectiveRestaurantId();
                effectiveBranchId = SelectedBranch?.Id;
                break;

            case UserRole.BranchManager:
                effectiveBranchId = _authService.GetEffectiveBranchId();
                break;
        }

        return (start, end, effectiveRestaurantId, effectiveBranchId);
    }

    public async Task LoadReportAsync()
    {
        if (IsLoading) return;

        IsLoading = true;
        HasData = false;
        HasTopProductsData = false;
        HasSalesTrendsData = false;

        try
        {
            var (start, end, effectiveRestaurantId, effectiveBranchId) = GetEffectiveParams();

            var timezone = TimeZoneInfo.Local.Id;

            var summaryTask    = _reportingService.GetSalesSummaryAsync(start, end, effectiveRestaurantId, effectiveBranchId);
            var topSellingTask = _reportingService.GetTopSellingAsync(start, end, effectiveRestaurantId, effectiveBranchId);
            var trendsTask     = _reportingService.GetSalesTrendsAsync(start, end, effectiveRestaurantId, effectiveBranchId, timezone: timezone);

            await Task.WhenAll(summaryTask, topSellingTask, trendsTask);

            var summaryResult = await summaryTask;
            var topSellingResult = await topSellingTask;
            var trendsResult = await trendsTask;

            if (summaryResult.IsSuccess && summaryResult.Data != null)
            {
                SalesSummary = summaryResult.Data;
                HasData = true;
            }
            else
            {
                var loc = LocalizationService.Instance;
                await Shell.Current?.DisplayAlert(loc["Error"], summaryResult.ErrorMessage ?? loc["Error"], loc["OK"]);
            }

            if (topSellingResult.IsSuccess && topSellingResult.Data != null)
            {
                _allTopProducts = topSellingResult.Data.Items;
                _visibleTopProductsCount = PageSize;
                RefreshVisibleTopProducts();
                HasTopProductsData = _allTopProducts.Count > 0;
            }

            if (trendsResult.IsSuccess && trendsResult.Data != null)
            {
                _salesTrendsReport = trendsResult.Data;
                _allSalesTrends = trendsResult.Data.DataPoints;
                _visibleSalesTrendsCount = PageSize;
                RefreshVisibleSalesTrends();
                HasSalesTrendsData = _allSalesTrends.Count > 0;
                OnPropertyChanged(nameof(TrendPercentDisplay));
                OnPropertyChanged(nameof(TrendPercentColor));
                OnPropertyChanged(nameof(TrendAverageDisplay));
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ReportingViewModel] LoadReportAsync error: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void ShowMoreTopProducts()
    {
        _visibleTopProductsCount = Math.Min(_visibleTopProductsCount + PageSize, _allTopProducts.Count);
        RefreshVisibleTopProducts();
    }

    private void RefreshVisibleTopProducts()
    {
        VisibleTopProducts = new ObservableCollection<TopSellingItem>(_allTopProducts.Take(_visibleTopProductsCount));
        OnPropertyChanged(nameof(CanShowMoreTopProducts));
    }

    private void ShowMoreSalesTrends()
    {
        _visibleSalesTrendsCount = Math.Min(_visibleSalesTrendsCount + PageSize, _allSalesTrends.Count);
        RefreshVisibleSalesTrends();
    }

    private void RefreshVisibleSalesTrends()
    {
        VisibleSalesTrends = new ObservableCollection<SalesTrendDataPoint>(_allSalesTrends.Take(_visibleSalesTrendsCount));
        OnPropertyChanged(nameof(CanShowMoreSalesTrends));
    }

    private async Task NavigateToOrderBreakdownAsync()
    {
        if (!HasData) return;

        var (start, end, restaurantId, branchId) = GetEffectiveParams();

        var parameters = new Dictionary<string, object>
        {
            { "startDate", start.ToString("o") },
            { "endDate", end.ToString("o") },
            { "restaurantId", (restaurantId ?? Guid.Empty).ToString() },
            { "branchId", (branchId ?? Guid.Empty).ToString() }
        };

        await Shell.Current.GoToAsync(nameof(Pages.OrderBreakdownPage), parameters);
    }

    private async Task SelectRestaurantAsync()
    {
        if (Restaurants.Count == 0)
            await LoadRestaurantsAsync();

        var loc = LocalizationService.Instance;
        var options = new List<string> { loc["AllRestaurants"] };
        options.AddRange(Restaurants.Select(r => r.Name));

        var result = await Shell.Current?.DisplayActionSheet(
            loc["SelectRestaurant"], loc["Cancel"], null, options.ToArray());

        if (string.IsNullOrEmpty(result) || result == loc["Cancel"])
            return;

        if (result == loc["AllRestaurants"])
        {
            SelectedRestaurant = null;
            SelectedBranch = null;
            Branches = new List<BranchDto>();
            return;
        }

        var picked = Restaurants.FirstOrDefault(r => r.Name == result);
        if (picked != null)
        {
            SelectedRestaurant = picked;
            SelectedBranch = null;
            Branches = new List<BranchDto>();
            await LoadBranchesAsync(picked.Id);
        }
    }

    private async Task SelectBranchAsync()
    {
        if (ShowBranchFilter && Branches.Count == 0 && IsRestaurantOwner)
        {
            var restaurantId = _authService.GetEffectiveRestaurantId();
            if (restaurantId.HasValue)
                await LoadBranchesAsync(restaurantId.Value);
        }

        var loc = LocalizationService.Instance;
        var options = new List<string> { loc["AllBranches"] };
        options.AddRange(Branches.Select(b => b.Name));

        var result = await Shell.Current?.DisplayActionSheet(
            loc["SelectBranch"], loc["Cancel"], null, options.ToArray());

        if (string.IsNullOrEmpty(result) || result == loc["Cancel"])
            return;

        if (result == loc["AllBranches"])
        {
            SelectedBranch = null;
            return;
        }

        var picked = Branches.FirstOrDefault(b => b.Name == result);
        if (picked != null)
            SelectedBranch = picked;
    }

    private async Task LogoutAsync()
    {
        await _authService.LogoutAsync();
        await Shell.Current.GoToAsync("//LoginPage");
    }

    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
