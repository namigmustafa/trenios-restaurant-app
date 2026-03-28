using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using Trenios.Mobile.Helpers;
using Trenios.Mobile.Models.Api;
using Trenios.Mobile.Services;

namespace Trenios.Mobile.ViewModels;

public class ActivityBoardViewModel : INotifyPropertyChanged
{
    private readonly ActivityService _activityService;
    private readonly AuthService _authService;
    private readonly OrderService _orderService;

    private bool _isLoading;
    private bool _isRefreshing;
    private IDispatcherTimer? _elapsedTimer;

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<ActivityBoardGroupDto> BoardGroups { get; } = new();

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

    public bool HasGroups => BoardGroups.Count > 0;

    public string RestaurantName => _authService.GetEffectiveRestaurantName() ?? "";
    public string BranchName => _authService.GetEffectiveBranchName() ?? "";
    public string? RestaurantLogoUrl => _authService.SelectedRestaurant?.DisplayImageUrl
        ?? _authService.CurrentUser?.Restaurant?.DisplayImageUrl;
    public bool HasRestaurantLogo => !string.IsNullOrEmpty(RestaurantLogoUrl);

    public ICommand LoadBoardCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand StartSessionCommand { get; }
    public ICommand StopSessionCommand { get; }
    public ICommand CancelSessionCommand { get; }
    public ICommand GoToOrderCommand { get; }
    public ICommand LogoutCommand { get; }

    public ActivityBoardViewModel(ActivityService activityService, AuthService authService, OrderService orderService)
    {
        _activityService = activityService;
        _authService = authService;
        _orderService = orderService;

        LoadBoardCommand = new Command(async () => await LoadBoardAsync());
        RefreshCommand = new Command(async () => await RefreshAsync());
        StartSessionCommand = new Command<ActivityBoardItemDto>(async (a) => await StartSessionAsync(a));
        StopSessionCommand = new Command<ActivityBoardItemDto>(async (a) => await StopSessionAsync(a));
        CancelSessionCommand = new Command<ActivityBoardItemDto>(async (a) => await CancelSessionAsync(a));
        GoToOrderCommand = new Command<ActivityBoardItemDto>(async (a) => await GoToOrderAsync(a));
        LogoutCommand = new Command(async () => await LogoutAsync());
    }

    public void StartElapsedTimer(IDispatcher dispatcher)
    {
        _elapsedTimer = dispatcher.CreateTimer();
        _elapsedTimer.Interval = TimeSpan.FromSeconds(30);
        _elapsedTimer.Tick += (s, e) => RefreshElapsedTimes();
        _elapsedTimer.Start();
    }

    public void StopElapsedTimer()
    {
        _elapsedTimer?.Stop();
        _elapsedTimer = null;
    }

    private void RefreshElapsedTimes()
    {
        // Force re-render of elapsed displays by reloading from API
        _ = LoadBoardAsync();
    }

    public async Task LoadBoardAsync()
    {
        IsLoading = true;

        try
        {
            var (groups, error) = await _activityService.GetActivityBoardAsync();

            if (groups != null)
            {
                BoardGroups.Clear();
                foreach (var g in groups)
                    BoardGroups.Add(g);
                OnPropertyChanged(nameof(HasGroups));
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
        await LoadBoardAsync();
        IsRefreshing = false;
    }

    private async Task StartSessionAsync(ActivityBoardItemDto activity)
    {
        if (activity == null || !activity.IsActive) return;

        var loc = LocalizationService.Instance;

        IsLoading = true;

        try
        {
            var request = new StartActivitySessionRequest
            {
                ActivityId = activity.Id,
                OrderId = null
            };

            var (session, error) = await _activityService.StartSessionAsync(request);

            if (session != null)
            {
                var message = session.OrderAutoCreated
                    ? $"{loc["SessionStarted"]}\n{loc["NewOrderCreated"]}: #{session.OrderNumber}"
                    : $"{loc["SessionStarted"]}\n{loc["Order"]}: #{session.OrderNumber}";

                await ShowAlertAsync(loc["StartSession"], message, loc["OK"]);
                await LoadBoardAsync();
            }
            else if (!string.IsNullOrEmpty(error))
            {
                await ShowAlertAsync(loc["Error"], error, loc["OK"]);
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task StopSessionAsync(ActivityBoardItemDto activity)
    {
        if (activity?.ActiveSession == null) return;

        var loc = LocalizationService.Instance;

        var confirm = await ShowAlertAsync(
            loc["StopSession"],
            $"{loc["ConfirmStopSession"]} {activity.Name}?",
            loc["Confirm"],
            loc["Cancel"]
        );

        if (!confirm) return;

        IsLoading = true;

        try
        {
            var (session, error) = await _activityService.StopSessionAsync(activity.ActiveSession.Id);

            if (session != null)
            {
                // Complete the linked order
                var (orderCompleted, orderError) = await _orderService.UpdateOrderStatusAsync(session.OrderId, OrderStatus.Completed);

                var summary = $"{activity.Name}\n{loc["Duration"]}: {session.DurationDisplay}\n{loc["TotalAmount"]}: {session.TotalAmountDisplay}";
                if (!orderCompleted && !string.IsNullOrEmpty(orderError))
                    summary += $"\n\n⚠ {loc["OrderCompleteFailed"]}: {orderError}";

                await ShowAlertAsync(loc["StopSession"], summary, loc["OK"]);
                await LoadBoardAsync();
            }
            else if (!string.IsNullOrEmpty(error))
            {
                await ShowAlertAsync(loc["Error"], error, loc["OK"]);
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task CancelSessionAsync(ActivityBoardItemDto activity)
    {
        if (activity?.ActiveSession == null) return;

        var loc = LocalizationService.Instance;

        var confirm = await ShowAlertAsync(
            loc["CancelSession"],
            $"{loc["ConfirmCancelSession"]} {activity.Name}?",
            loc["Confirm"],
            loc["Cancel"]
        );

        if (!confirm) return;

        var reason = await ShowPromptAsync(
            loc["CancelSession"],
            loc["EnterCancellationReason"],
            loc["Confirm"],
            loc["Cancel"],
            placeholder: loc["CancellationPlaceholder"]
        );

        if (reason == null) return;

        IsLoading = true;

        try
        {
            var (session, error) = await _activityService.CancelSessionAsync(activity.ActiveSession.Id, reason);

            if (session != null)
            {
                await ShowAlertAsync(loc["CancelSession"], loc["SessionCancelled"], loc["OK"]);
                await LoadBoardAsync();
            }
            else if (!string.IsNullOrEmpty(error))
            {
                await ShowAlertAsync(loc["Error"], error, loc["OK"]);
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task GoToOrderAsync(ActivityBoardItemDto activity)
    {
        if (activity?.ActiveSession == null) return;
        var orderId = activity.ActiveSession.OrderId;
        OrdersViewModel.PendingOrderId = orderId;
        await Shell.Current.GoToAsync("//OrdersPage");
    }

    private async Task LogoutAsync()
    {
        await _authService.LogoutAsync();
        await Shell.Current.GoToAsync("//LoginPage");
    }

    private static Task ShowAlertAsync(string title, string message, string cancel)
        => Shell.Current?.DisplayAlert(title, message, cancel) ?? Task.CompletedTask;

    private static Task<bool> ShowAlertAsync(string title, string message, string accept, string cancel)
        => Shell.Current?.DisplayAlert(title, message, accept, cancel) ?? Task.FromResult(false);

    private static Task<string?> ShowPromptAsync(string title, string message, string accept, string cancel, string? placeholder = null)
        => Shell.Current?.DisplayPromptAsync(title, message, accept, cancel, placeholder: placeholder) ?? Task.FromResult<string?>(null);

    protected void OnPropertyChanged(string propertyName)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
