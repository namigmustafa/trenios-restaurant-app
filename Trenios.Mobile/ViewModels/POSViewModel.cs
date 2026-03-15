using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using Trenios.Mobile.Models.Api;
using Trenios.Mobile.Services;

namespace Trenios.Mobile.ViewModels;

public class POSViewModel : BaseViewModel
{
    private readonly ProductService _productService;
    private readonly OrderService _orderService;
    private readonly AuthService _authService;
    private readonly TableService _tableService;

    private Guid? _selectedCategoryId;
    private BranchMenuItemDto? _selectedMenuItem;
    private bool _showCustomization;
    private bool _isLoadingCategories;
    private bool _isLoadingProducts;
    private bool _isLoadingAdditions;
    private string? _errorMessage;
    private int _customizationQuantity = 1;

    // Cached customization totals for performance
    private decimal _cachedAdditionsTotal;
    private decimal _cachedItemPrice;
    private decimal _cachedTotalPrice;
    private string? _validationMessage;

    // Cache built SelectableAdditionGroups by menuItemId for instant reopening
    private readonly Dictionary<Guid, List<SelectableAdditionGroup>> _groupsCache = new();

    // Order Type Selection
    private bool _showOrderTypeSelection;
    private OrderType _selectedOrderType = OrderType.TakeAway;

    // Table Selection
    private bool _showTableSelection;
    private TableDto? _selectedTable;
    private bool _isLoadingTables;

    public ObservableCollection<CategoryDto> Categories { get; } = new();
    public ObservableCollection<BranchMenuItemDto> MenuItems { get; } = new();
    public ObservableCollection<CartItem> CartItems { get; } = new();
    public ObservableCollection<SelectableAdditionGroup> SelectableAdditionGroups { get; } = new();
    public ObservableCollection<TableDto> Tables { get; } = new();

    public event Action? OnOrderCompleted;

    public string UserName => _authService.CurrentUser?.FullName ?? "Cashier";
    public string BranchName => _authService.GetEffectiveBranchName() ?? "Branch";

    // Back button visible only for SuperAdmin and RestaurantOwner (to switch branches)
    public bool CanGoBack => _authService.CurrentUser?.UserRole == UserRole.SuperAdmin ||
                             _authService.CurrentUser?.UserRole == UserRole.RestaurantOwner;

    public Guid? SelectedCategoryId
    {
        get => _selectedCategoryId;
        set
        {
            if (SetProperty(ref _selectedCategoryId, value))
            {
                OnPropertyChanged(nameof(SelectedCategoryColor));
                _ = LoadMenuItemsAsync();
            }
        }
    }

    public string SelectedCategoryColor
    {
        get
        {
            var category = Categories.FirstOrDefault(c => c.Id == SelectedCategoryId);
            return category?.Color ?? "#F97316";
        }
    }

    public BranchMenuItemDto? SelectedMenuItem
    {
        get => _selectedMenuItem;
        set => SetProperty(ref _selectedMenuItem, value);
    }

    public bool ShowCustomization
    {
        get => _showCustomization;
        set => SetProperty(ref _showCustomization, value);
    }

    public bool IsLoadingCategories
    {
        get => _isLoadingCategories;
        set => SetProperty(ref _isLoadingCategories, value);
    }

    public bool IsLoadingProducts
    {
        get => _isLoadingProducts;
        set => SetProperty(ref _isLoadingProducts, value);
    }

    public bool IsLoadingAdditions
    {
        get => _isLoadingAdditions;
        set => SetProperty(ref _isLoadingAdditions, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public int CustomizationQuantity
    {
        get => _customizationQuantity;
        set
        {
            if (value >= 1 && SetProperty(ref _customizationQuantity, value))
            {
                RecalculateCustomizationTotals();
                ((Command)DecreaseCustomizationQuantityCommand).ChangeCanExecute();
            }
        }
    }

    public decimal CustomizationAdditionsTotal => _cachedAdditionsTotal;
    public decimal CustomizationItemPrice => _cachedItemPrice;
    public decimal CustomizationTotalPrice => _cachedTotalPrice;

    public string? ValidationMessage
    {
        get => _validationMessage;
        private set => SetProperty(ref _validationMessage, value);
    }

    public bool HasValidationError => !string.IsNullOrEmpty(_validationMessage);

    public decimal Subtotal => _orderService.Subtotal;
    public decimal Tax => _orderService.Tax;
    public decimal Total => _orderService.Total;
    public int TotalItems => _orderService.TotalItems;
    public bool HasItems => _orderService.HasItems;
    public bool CanRepeat => _orderService.LastCompletedOrder != null;
    public int HeldOrdersCount => _orderService.HeldOrdersCount;

    // Order Type Selection Properties
    public bool ShowOrderTypeSelection
    {
        get => _showOrderTypeSelection;
        set => SetProperty(ref _showOrderTypeSelection, value);
    }

    public OrderType SelectedOrderType
    {
        get => _selectedOrderType;
        set => SetProperty(ref _selectedOrderType, value);
    }

    public bool IsDineInEnabled => _authService.CurrentBranch?.IsDineInEnabled ?? false;
    public bool IsTakeAwayEnabled => _authService.CurrentBranch?.IsTakeAwayEnabled ?? true;
    public bool IsDeliveryEnabled => _authService.CurrentBranch?.IsDeliveryEnabled ?? false;

    // Table Selection Properties
    public bool ShowTableSelection
    {
        get => _showTableSelection;
        set => SetProperty(ref _showTableSelection, value);
    }

    public TableDto? SelectedTable
    {
        get => _selectedTable;
        set => SetProperty(ref _selectedTable, value);
    }

    public bool IsLoadingTables
    {
        get => _isLoadingTables;
        set => SetProperty(ref _isLoadingTables, value);
    }

    // Backend always requires tableId for DineIn orders, so table selection is always required
    public bool IsTableRequired => true;

    // Commands
    public ICommand SelectCategoryCommand { get; }
    public ICommand SelectMenuItemCommand { get; }
    public ICommand IncreaseQuantityCommand { get; }
    public ICommand DecreaseQuantityCommand { get; }
    public ICommand RemoveItemCommand { get; }
    public ICommand PayCommand { get; }
    public ICommand HoldOrderCommand { get; }
    public ICommand CancelOrderCommand { get; }
    public ICommand RepeatLastOrderCommand { get; }
    public ICommand ToggleAdditionCommand { get; }
    public ICommand ConfirmCustomizationCommand { get; }
    public ICommand CancelCustomizationCommand { get; }
    public ICommand IncreaseCustomizationQuantityCommand { get; }
    public ICommand DecreaseCustomizationQuantityCommand { get; }
    public ICommand IncreaseAdditionQuantityCommand { get; }
    public ICommand DecreaseAdditionQuantityCommand { get; }
    public ICommand LogoutCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand BackCommand { get; }
    public ICommand ViewOrdersCommand { get; }
    public ICommand ViewKitchenCommand { get; }
    public ICommand ViewTablesCommand { get; }

    // Order Type & Table Selection Commands
    public ICommand SelectOrderTypeCommand { get; }
    public ICommand CancelOrderTypeCommand { get; }
    public ICommand SelectTableCommand { get; }
    public ICommand SkipTableSelectionCommand { get; }
    public ICommand ConfirmTableSelectionCommand { get; }
    public ICommand CancelTableSelectionCommand { get; }

    public POSViewModel(ProductService productService, OrderService orderService, AuthService authService, TableService tableService)
    {
        _productService = productService;
        _orderService = orderService;
        _authService = authService;
        _tableService = tableService;

        // Initialize commands
        SelectCategoryCommand = new Command<CategoryDto>(cat => SelectedCategoryId = cat.Id);
        SelectMenuItemCommand = new Command<BranchMenuItemDto>(SelectMenuItem);
        IncreaseQuantityCommand = new Command<CartItem>(item => _orderService.UpdateQuantity(item.Id, item.Quantity + 1));
        DecreaseQuantityCommand = new Command<CartItem>(item => _orderService.UpdateQuantity(item.Id, item.Quantity - 1));
        RemoveItemCommand = new Command<CartItem>(item => _orderService.RemoveItem(item.Id));
        PayCommand = new Command(async () => await CreateOrderAsync(), () => HasItems);
        HoldOrderCommand = new Command(() => _orderService.HoldOrder(), () => HasItems);
        CancelOrderCommand = new Command(() => _orderService.ClearCart(), () => HasItems);
        RepeatLastOrderCommand = new Command(() => _orderService.RepeatLastOrder(), () => CanRepeat);
        ToggleAdditionCommand = new Command<SelectableAddition>(ToggleAddition);
        ConfirmCustomizationCommand = new Command(ConfirmCustomization, CanConfirmCustomization);
        CancelCustomizationCommand = new Command(CancelCustomization);
        IncreaseCustomizationQuantityCommand = new Command(IncreaseCustomizationQuantity);
        DecreaseCustomizationQuantityCommand = new Command(DecreaseCustomizationQuantity, () => CustomizationQuantity > 1);
        IncreaseAdditionQuantityCommand = new Command<SelectableAddition>(IncreaseAdditionQuantity);
        DecreaseAdditionQuantityCommand = new Command<SelectableAddition>(DecreaseAdditionQuantity);
        LogoutCommand = new Command(async () => await LogoutAsync());
        RefreshCommand = new Command(async () => await LoadDataAsync(forceRefresh: true));
        BackCommand = new Command(async () => await GoBackAsync(), () => CanGoBack);
        ViewOrdersCommand = new Command(async () => await Shell.Current.GoToAsync("//OrdersPage"));
        ViewKitchenCommand = new Command(async () => await Shell.Current.GoToAsync("//KitchenPage"));
        ViewTablesCommand = new Command(async () => await Shell.Current.GoToAsync("//TablesPage"));

        // Order Type & Table Selection Commands
        SelectOrderTypeCommand = new Command<object>(async (param) => await SelectOrderTypeAsync(param));
        CancelOrderTypeCommand = new Command(() => ShowOrderTypeSelection = false);
        SelectTableCommand = new Command<TableDto>(table => SelectedTable = table);
        SkipTableSelectionCommand = new Command(async () => await SkipTableSelectionAsync());
        ConfirmTableSelectionCommand = new Command(async () => await ConfirmTableSelectionAsync());
        CancelTableSelectionCommand = new Command(CancelTableSelection);

        // Subscribe to cart changes
        _orderService.OnCartChanged += RefreshCart;

        // Reload data when branch/restaurant changes (SuperAdmin switching restaurants)
        _authService.OnAuthStateChanged += () => _ = LoadDataAsync(forceRefresh: true);

        // Load initial data
        _ = LoadDataAsync();
    }

    public async Task LoadDataAsync(bool forceRefresh = false)
    {
        SelectedCategoryId = null;
        await LoadCategoriesAsync(forceRefresh);

        if (Categories.Count > 0 && SelectedCategoryId == null)
        {
            SelectedCategoryId = Categories.First().Id;
        }
    }

    private async Task LoadCategoriesAsync(bool forceRefresh = false)
    {
        IsLoadingCategories = true;
        ErrorMessage = null;

        try
        {
            System.Diagnostics.Debug.WriteLine($"[LoadCategoriesAsync] Starting, forceRefresh: {forceRefresh}");
            var (categories, error) = await _productService.GetCategoriesAsync(forceRefresh);
            System.Diagnostics.Debug.WriteLine($"[LoadCategoriesAsync] Received {categories?.Count ?? 0} categories, Error: {error}");

            if (categories != null && categories.Count > 0)
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    try
                    {
                        System.Diagnostics.Debug.WriteLine($"[LoadCategoriesAsync] Updating Categories collection. Current: {Categories.Count}");

                        Categories.Clear();

                        foreach (var cat in categories)
                        {
                            System.Diagnostics.Debug.WriteLine($"[LoadCategoriesAsync] Adding category: {cat.Name}");
                            Categories.Add(cat);
                        }

                        System.Diagnostics.Debug.WriteLine($"[LoadCategoriesAsync] Categories updated. Final count: {Categories.Count}");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[LoadCategoriesAsync] Collection update ERROR: {ex.Message}");
                        ErrorMessage = $"Failed to update categories: {ex.Message}";
                    }
                });
            }
            else
            {
                ErrorMessage = error;
                System.Diagnostics.Debug.WriteLine($"[LoadCategoriesAsync] Service returned error: {error}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[LoadCategoriesAsync] EXCEPTION: {ex.Message}\nStack: {ex.StackTrace}");
            ErrorMessage = $"Failed to load categories: {ex.Message}";
        }
        finally
        {
            IsLoadingCategories = false;
            System.Diagnostics.Debug.WriteLine("[LoadCategoriesAsync] Completed");
        }
    }

    private async Task LoadMenuItemsAsync()
    {
        if (SelectedCategoryId == null) return;

        IsLoadingProducts = true;
        ErrorMessage = null;

        try
        {
            System.Diagnostics.Debug.WriteLine($"[LoadMenuItemsAsync] Loading menu items for category: {SelectedCategoryId}");
            var (items, error) = await _productService.GetMenuItemsByCategoryAsync(SelectedCategoryId.Value);
            System.Diagnostics.Debug.WriteLine($"[LoadMenuItemsAsync] Received {items?.Count ?? 0} items, Error: {error}");

            if (items != null)
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    System.Diagnostics.Debug.WriteLine($"[LoadMenuItemsAsync] Updating MenuItems collection. Current: {MenuItems.Count}");
                    MenuItems.Clear();

                    foreach (var item in items)
                    {
                        System.Diagnostics.Debug.WriteLine($"[LoadMenuItemsAsync] Adding item: {item.MenuItemName}");
                        MenuItems.Add(item);
                    }

                    System.Diagnostics.Debug.WriteLine($"[LoadMenuItemsAsync] MenuItems updated. Final count: {MenuItems.Count}");
                });
            }
            else
            {
                ErrorMessage = error;
                System.Diagnostics.Debug.WriteLine($"[LoadMenuItemsAsync] Service returned error: {error}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[LoadMenuItemsAsync] EXCEPTION: {ex.Message}\nStack: {ex.StackTrace}");
            ErrorMessage = $"Failed to load menu items: {ex.Message}";
        }
        finally
        {
            IsLoadingProducts = false;
            System.Diagnostics.Debug.WriteLine("[LoadMenuItemsAsync] Completed");
        }
    }

    private async void SelectMenuItem(BranchMenuItemDto menuItem)
    {
        if (IsLoadingAdditions) return;

        try
        {
            // If reopening the same item, just reset and show - INSTANT!
            if (SelectedMenuItem?.MenuItemId == menuItem.MenuItemId && SelectableAdditionGroups.Count > 0)
            {
                CustomizationQuantity = 1;

                // Reset all selections without destroying UI
                foreach (var group in SelectableAdditionGroups)
                {
                    group.ResetSelections();
                }

                RecalculateCustomizationTotals();
                ((Command)ConfirmCustomizationCommand).ChangeCanExecute();
                ShowCustomization = true;
                return;
            }

            // Show loading spinner IMMEDIATELY
            IsLoadingAdditions = true;

            // Small delay to ensure spinner shows before heavy work
            await Task.Delay(50);

            SelectedMenuItem = menuItem;
            CustomizationQuantity = 1;

            // Check if we have cached groups for a different menu item
            if (_groupsCache.TryGetValue(menuItem.MenuItemId, out var cachedGroups))
            {
                // Swap to cached groups (still faster than building)
                SelectableAdditionGroups.Clear();
                foreach (var group in cachedGroups)
                {
                    group.ResetSelections();
                    SelectableAdditionGroups.Add(group);
                }

                RecalculateCustomizationTotals();
                ((Command)ConfirmCustomizationCommand).ChangeCanExecute();
                IsLoadingAdditions = false;
                ShowCustomization = true;
                return;
            }

            // First time opening this item - build groups
            ShowCustomization = true;

            await Task.Run(() =>
            {
                var tempGroups = new List<SelectableAdditionGroup>();

                if (menuItem.AdditionGroups != null)
                {
                    foreach (var group in menuItem.AdditionGroups)
                    {
                        if (group == null || group.Additions == null || group.Additions.Count == 0) continue;
                        tempGroups.Add(new SelectableAdditionGroup(group, OnAdditionSelectionChanged));
                    }
                }

                // Cache these groups for future access
                _groupsCache[menuItem.MenuItemId] = tempGroups;

                // Update UI on main thread
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    SelectableAdditionGroups.Clear();
                    foreach (var group in tempGroups)
                    {
                        SelectableAdditionGroups.Add(group);
                    }

                    RecalculateCustomizationTotals();
                    ((Command)ConfirmCustomizationCommand).ChangeCanExecute();
                    IsLoadingAdditions = false;
                });
            });
        }
        catch (Exception ex)
        {
            IsLoadingAdditions = false;
            await Application.Current?.MainPage?.DisplayAlert("Error",
                $"{ex.GetType().Name}: {ex.Message}\n\n{ex.StackTrace}", "OK");
        }
    }

    private void OnAdditionSelectionChanged()
    {
        RecalculateCustomizationTotals();
        ((Command)ConfirmCustomizationCommand).ChangeCanExecute();
    }

    /// <summary>
    /// Recalculates and caches customization totals, then fires property change notifications.
    /// </summary>
    private void RecalculateCustomizationTotals()
    {
        // Calculate additions total using simple loop (faster than LINQ)
        decimal additionsTotal = 0;
        foreach (var group in SelectableAdditionGroups)
        {
            foreach (var addition in group.Additions)
            {
                if (addition.IsSelected && addition.Quantity > 0)
                {
                    additionsTotal += addition.TotalPrice;
                }
            }
        }

        var basePrice = SelectedMenuItem?.Price ?? 0;
        var itemPrice = basePrice + additionsTotal;
        var totalPrice = itemPrice * CustomizationQuantity;

        // Only notify if values changed
        var additionsChanged = _cachedAdditionsTotal != additionsTotal;
        var itemPriceChanged = _cachedItemPrice != itemPrice;
        var totalPriceChanged = _cachedTotalPrice != totalPrice;

        _cachedAdditionsTotal = additionsTotal;
        _cachedItemPrice = itemPrice;
        _cachedTotalPrice = totalPrice;

        if (additionsChanged)
            OnPropertyChanged(nameof(CustomizationAdditionsTotal));
        if (itemPriceChanged)
            OnPropertyChanged(nameof(CustomizationItemPrice));
        if (totalPriceChanged)
            OnPropertyChanged(nameof(CustomizationTotalPrice));
    }

    private void ToggleAddition(SelectableAddition addition)
    {
        var group = SelectableAdditionGroups.FirstOrDefault(g => g.Additions.Contains(addition));
        if (group == null) return;

        // Don't allow toggling disabled items (unless already selected) - only for multi-select
        if (!group.IsSingleSelect && addition.IsDisabled && !addition.IsSelected)
            return;

        // If only one addition in group, it's always selected and can't be toggled
        if (group.HasOnlyOneAddition)
        {
            if (!addition.IsSelected)
            {
                addition.IsSelected = true;
                OnAdditionSelectionChanged();
            }
            return;
        }

        if (group.IsSingleSelect)
        {
            // For single select (radio), select the clicked item and deselect all others
            foreach (var a in group.Additions)
            {
                if (a == addition)
                {
                    // For required groups, always select; for optional, toggle
                    a.ForceSetSelected(group.IsRequired || !a.IsSelected);
                }
                else
                {
                    a.ForceSetSelected(false);
                }
            }
        }
        else
        {
            // For multi-select (checkbox)
            if (addition.IsSelected)
            {
                var currentCount = group.TotalSelected;
                if (!group.IsRequired || currentCount > group.MinSelections)
                {
                    addition.IsSelected = false;
                }
            }
            else if (group.CanAddMore)
            {
                addition.IsSelected = true;
            }
        }

        OnAdditionSelectionChanged();
    }

    private void ConfirmCustomization()
    {
        if (SelectedMenuItem != null)
        {
            var selectedAdditions = SelectableAdditionGroups
                .SelectMany(g => g.Additions)
                .Where(a => a.IsSelected && a.Quantity > 0)
                .Select(a => new SelectedAddition
                {
                    AdditionId = a.AdditionId,
                    AdditionName = a.Name,
                    UnitPrice = a.UnitPrice,
                    Quantity = a.Quantity
                })
                .ToList();

            _orderService.AddItem(
                SelectedMenuItem,
                quantity: CustomizationQuantity,
                additions: selectedAdditions);
        }

        CancelCustomization();
    }

    private void CancelCustomization()
    {
        ShowCustomization = false;
        // DON'T set SelectedMenuItem to null - keeps the same item check working
        // DON'T clear SelectableAdditionGroups - keeps visual tree alive for instant reopen
        CustomizationQuantity = 1;
        ValidationMessage = null;
        OnPropertyChanged(nameof(HasValidationError));
    }

    private async Task CreateOrderAsync()
    {
        if (IsBusy) return;

        // Check if we need to show order type selection (if DineIn is enabled)
        if (IsDineInEnabled)
        {
            ShowOrderTypeSelection = true;
            return;
        }

        // If only TakeAway enabled (no DineIn), proceed directly
        await SubmitOrderAsync(OrderType.TakeAway, null);
    }

    private async Task SelectOrderTypeAsync(object param)
    {
        if (param is int typeInt)
        {
            var orderType = (OrderType)typeInt;
            SelectedOrderType = orderType;
            ShowOrderTypeSelection = false;

            if (orderType == OrderType.DineIn)
            {
                // Show table selection overlay first so CollectionView can layout,
                // then load tables into it (avoids iOS UICollectionView zero-frame crash)
                ShowTableSelection = true;
                await LoadTablesAsync();
                return;
            }

            // For TakeAway/Delivery, submit directly
            await SubmitOrderAsync(orderType, null);
        }
        else if (param is string typeStr && int.TryParse(typeStr, out var parsed))
        {
            await SelectOrderTypeAsync(parsed);
        }
    }

    private async Task LoadTablesAsync()
    {
        IsLoadingTables = true;
        SelectedTable = null;

        try
        {
            var branchId = _authService.GetEffectiveBranchId();
            if (branchId.HasValue)
            {
                var (tables, error) = await _tableService.GetTablesAsync(branchId.Value);

                Tables.Clear();
                if (tables != null)
                {
                    foreach (var table in tables)
                    {
                        Tables.Add(table);
                    }
                }
            }
        }
        finally
        {
            IsLoadingTables = false;
        }
    }

    private async Task ConfirmTableSelectionAsync()
    {
        // If required but no table selected, show error
        if (IsTableRequired && SelectedTable == null)
        {
            await Application.Current!.MainPage!.DisplayAlert(
                LocalizationService.Instance["Error"],
                LocalizationService.Instance["TableRequired"],
                LocalizationService.Instance["OK"]);
            return;
        }

        ShowTableSelection = false;
        await SubmitOrderAsync(OrderType.DineIn, SelectedTable?.Id);
    }

    private async Task SkipTableSelectionAsync()
    {
        // Backend requires tableId for DineIn, so skip is not allowed
        // This method is kept for future use if backend changes
        await Application.Current!.MainPage!.DisplayAlert(
            LocalizationService.Instance["Error"],
            LocalizationService.Instance["TableRequired"],
            LocalizationService.Instance["OK"]);
    }

    private void CancelTableSelection()
    {
        ShowTableSelection = false;
        SelectedTable = null;
    }

    private async Task SubmitOrderAsync(OrderType orderType, Guid? tableId)
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;

            var (order, error) = await _orderService.SubmitOrderAsync(orderType, tableId);

            if (order != null)
            {
                var message = $"Order #{order.OrderNumber}\nTotal: €{order.TotalAmount:F2}";
                if (order.HasTable)
                {
                    message += $"\n{order.TableDisplay}";
                }

                await Application.Current!.MainPage!.DisplayAlert(
                    LocalizationService.Instance["OrderSubmitted"],
                    message,
                    LocalizationService.Instance["OK"]);

                OnOrderCompleted?.Invoke();
            }
            else
            {
                var errorMessage = string.IsNullOrWhiteSpace(error)
                    ? LocalizationService.Instance["FailedToSubmit"]
                    : error;
                await Application.Current!.MainPage!.DisplayAlert(
                    LocalizationService.Instance["Error"],
                    errorMessage,
                    LocalizationService.Instance["OK"]);
            }
        }
        finally
        {
            IsBusy = false;
            SelectedTable = null;
        }
    }

    private async Task LogoutAsync()
    {
        var confirm = await Application.Current!.MainPage!.DisplayAlert(
            "Logout",
            "Are you sure you want to logout?",
            "Yes",
            "No");

        if (confirm)
        {
            _orderService.ClearCart();
            _productService.ClearCache();
            _tableService.ClearCache();
            _groupsCache.Clear(); // Clear cached groups on logout
            await _authService.LogoutAsync();
            await Shell.Current.GoToAsync("//LoginPage");
        }
    }

    private async Task GoBackAsync()
    {
        if (CanGoBack)
        {
            _productService.ClearCache();
            _tableService.ClearCache();
            _groupsCache.Clear();
            await Shell.Current.GoToAsync("//BranchSelection");
        }
    }

    private void IncreaseCustomizationQuantity()
    {
        CustomizationQuantity++;
    }

    private void DecreaseCustomizationQuantity()
    {
        if (CustomizationQuantity > 1)
        {
            CustomizationQuantity--;
        }
    }

    private void IncreaseAdditionQuantity(SelectableAddition addition)
    {
        var group = SelectableAdditionGroups.FirstOrDefault(g => g.Additions.Contains(addition));
        if (group == null || group.IsSingleSelect) return;

        // Check if we can add more
        if (group.CanAddMore)
        {
            addition.Quantity++;
        }
    }

    private void DecreaseAdditionQuantity(SelectableAddition addition)
    {
        var group = SelectableAdditionGroups.FirstOrDefault(g => g.Additions.Contains(addition));
        if (group == null || group.IsSingleSelect) return;

        if (addition.Quantity > 0)
        {
            addition.Quantity--;
        }
    }

    private bool CanConfirmCustomization()
    {
        // Check all required groups have selections
        // Uses cached TotalSelected from each group for performance
        var missingGroups = new List<string>();

        foreach (var group in SelectableAdditionGroups)
        {
            if (group.IsRequired)
            {
                // TotalSelected is already cached in the group
                var totalSelections = group.TotalSelected;

                if (totalSelections < group.MinSelections || totalSelections == 0)
                {
                    missingGroups.Add(group.Name);
                }
            }
        }

        if (missingGroups.Count > 0)
        {
            ValidationMessage = $"Please select: {string.Join(", ", missingGroups)}";
            OnPropertyChanged(nameof(HasValidationError));
            return false;
        }

        ValidationMessage = null;
        OnPropertyChanged(nameof(HasValidationError));
        return true;
    }

    private void RefreshCart()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[RefreshCart] Starting. Current: {CartItems.Count}, New: {_orderService.CartItems.Count}");

                // iOS CRITICAL FIX: Don't update CartItems collection to avoid UICollectionView crash
                // The phone cart view populates CartItems on demand in OnCartTapped/OnCartChanged
                if (DeviceInfo.Platform != DevicePlatform.iOS)
                {
                    CartItems.Clear();
                    foreach (var item in _orderService.CartItems)
                    {
                        CartItems.Add(item);
                    }
                    System.Diagnostics.Debug.WriteLine($"[RefreshCart] CartItems updated: {CartItems.Count}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[RefreshCart] iOS: Skipping CartItems update to avoid UICollectionView crash");
                }

                // Fire property changes
                OnPropertyChanged(nameof(Subtotal));
                OnPropertyChanged(nameof(Tax));
                OnPropertyChanged(nameof(Total));
                OnPropertyChanged(nameof(TotalItems));
                OnPropertyChanged(nameof(HasItems));
                OnPropertyChanged(nameof(CanRepeat));
                OnPropertyChanged(nameof(HeldOrdersCount));

                ((Command)PayCommand).ChangeCanExecute();
                ((Command)HoldOrderCommand).ChangeCanExecute();
                ((Command)CancelOrderCommand).ChangeCanExecute();
                ((Command)RepeatLastOrderCommand).ChangeCanExecute();

                System.Diagnostics.Debug.WriteLine($"[RefreshCart] Completed. HasItems: {HasItems}, TotalItems: {TotalItems}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[RefreshCart] ERROR: {ex.Message}");
            }
        });
    }
}

/// <summary>
/// Represents an addition group with selectable additions for the customization UI.
/// Optimized for performance with cached properties and batched notifications.
/// </summary>
public class SelectableAdditionGroup : INotifyPropertyChanged
{
    public Guid Id { get; }
    public string Name { get; }
    public bool IsSingleSelect { get; }
    public bool IsMultiSelect { get; }
    public bool IsRequired { get; }
    public int MinSelections { get; }
    public int MaxSelections { get; }
    public bool HasOnlyOneAddition { get; }

    // Use List for faster iteration - we don't need collection change notifications
    private readonly List<SelectableAddition> _additions = new();
    public IReadOnlyList<SelectableAddition> Additions => _additions;

    // Cached computed values
    private int _cachedTotalSelected;
    private string _cachedSelectionHint = string.Empty;
    private bool _cachedCanAddMore = true;
    private bool _suppressNotifications; // Flag to suppress notifications during init

    public event PropertyChangedEventHandler? PropertyChanged;

    public int TotalSelected => _cachedTotalSelected;
    public int RemainingSelections => MaxSelections > 0 ? Math.Max(0, MaxSelections - _cachedTotalSelected) : int.MaxValue;
    public bool CanAddMore => _cachedCanAddMore;
    public string SelectionHint => _cachedSelectionHint;

    public SelectableAdditionGroup(AdditionGroupDto group, Action onSelectionChanged)
    {
        // Suppress all notifications during construction
        _suppressNotifications = true;

        Id = group.Id;
        Name = group.Name ?? "Unknown";
        IsSingleSelect = group.IsSingleSelect;
        IsMultiSelect = group.IsMultiSelect;
        IsRequired = group.IsRequired;
        MinSelections = group.MinSelections ?? 0;
        MaxSelections = group.MaxSelections ?? 0;

        if (group.Additions != null && group.Additions.Count > 0)
        {
            // Optimized: avoid LINQ, filter and sort in one pass with minimal allocations
            var count = group.Additions.Count;
            var tempList = new List<AdditionDto>(count);

            for (int i = 0; i < count; i++)
            {
                var addition = group.Additions[i];
                if (addition != null && addition.IsAvailable)
                {
                    tempList.Add(addition);
                }
            }

            // Sort by DisplayOrder in-place
            tempList.Sort((a, b) => a.DisplayOrder.CompareTo(b.DisplayOrder));

            HasOnlyOneAddition = tempList.Count == 1;

            // Pre-allocate list with exact capacity
            _additions.Capacity = tempList.Count;

            // Create all additions without triggering change notifications
            for (int i = 0; i < tempList.Count; i++)
            {
                _additions.Add(new SelectableAddition(tempList[i], this, onSelectionChanged));
            }

            // Auto-select first addition if needed (without triggering callbacks)
            if ((HasOnlyOneAddition || (IsRequired && IsSingleSelect)) && _additions.Count > 0)
            {
                _additions[0].SetSelectedSilent(true);
                _cachedTotalSelected = 1;
            }

            // Calculate other cached values
            _cachedCanAddMore = MaxSelections == 0 || _cachedTotalSelected < MaxSelections;
            _cachedSelectionHint = ComputeSelectionHint();
        }
        else
        {
            HasOnlyOneAddition = false;
            _cachedSelectionHint = ComputeSelectionHint();
        }

        // Re-enable notifications after construction complete
        _suppressNotifications = false;
    }

    /// <summary>
    /// Called by child additions when selection/quantity changes.
    /// Recalculates cached values and fires a single batch of notifications.
    /// </summary>
    internal void OnAdditionChanged()
    {
        if (_suppressNotifications) return; // Skip during initialization

        var oldTotalSelected = _cachedTotalSelected;
        var oldCanAddMore = _cachedCanAddMore;
        var oldHint = _cachedSelectionHint;

        RecalculateCachedValues();

        // Only notify if values actually changed
        if (oldTotalSelected != _cachedTotalSelected)
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TotalSelected)));

        if (oldCanAddMore != _cachedCanAddMore)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CanAddMore)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(RemainingSelections)));

            // Update disabled states only when CanAddMore changes
            if (!IsSingleSelect && MaxSelections > 0)
            {
                UpdateDisabledStates();
            }
        }

        if (oldHint != _cachedSelectionHint)
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectionHint)));
    }

    private void RecalculateCachedValues()
    {
        _cachedTotalSelected = 0;
        foreach (var a in _additions)
        {
            if (a.IsSelected)
                _cachedTotalSelected += a.Quantity;
        }

        _cachedCanAddMore = MaxSelections == 0 || _cachedTotalSelected < MaxSelections;
        _cachedSelectionHint = ComputeSelectionHint();
    }

    private void UpdateDisabledStates()
    {
        var canAdd = _cachedCanAddMore;
        foreach (var addition in _additions)
        {
            var shouldBeDisabled = !canAdd && !addition.IsSelected;
            if (addition.IsDisabled != shouldBeDisabled)
            {
                addition.SetDisabledSilent(shouldBeDisabled);
            }
        }
    }

    private string ComputeSelectionHint()
    {
        if (HasOnlyOneAddition)
            return "Required";
        if (IsSingleSelect)
            return IsRequired ? "Select one (required)" : "Select one (optional)";
        if (MaxSelections > 0)
        {
            if (IsRequired && MinSelections > 0)
                return $"Select {MinSelections}-{MaxSelections} ({_cachedTotalSelected}/{MaxSelections})";
            return $"Select up to {MaxSelections} ({_cachedTotalSelected}/{MaxSelections})";
        }
        return "Select any";
    }

    /// <summary>
    /// Reset all selections to initial state for reuse
    /// </summary>
    public void ResetSelections()
    {
        _suppressNotifications = true;

        // Reset all additions to default state
        foreach (var addition in _additions)
        {
            addition.ResetToDefault();
        }

        // Auto-select first if needed
        if ((HasOnlyOneAddition || (IsRequired && IsSingleSelect)) && _additions.Count > 0)
        {
            _additions[0].SetSelectedSilent(true);
            _cachedTotalSelected = 1;
        }
        else
        {
            _cachedTotalSelected = 0;
        }

        // Recalculate cached values
        _cachedCanAddMore = MaxSelections == 0 || _cachedTotalSelected < MaxSelections;
        _cachedSelectionHint = ComputeSelectionHint();

        _suppressNotifications = false;

        // Fire one batch update
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TotalSelected)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectionHint)));
    }
}

/// <summary>
/// Represents a single selectable addition with selection state and quantity.
/// Optimized for performance with minimal property notifications.
/// </summary>
public class SelectableAddition : INotifyPropertyChanged
{
    private bool _isSelected;
    private int _quantity = 1;
    private bool _isDisabled;
    private readonly SelectableAdditionGroup _parentGroup;
    private readonly Action _onSelectionChanged;
    private bool _suppressNotifications; // Flag to suppress notifications during init

    public Guid AdditionId { get; }
    public string Name { get; }
    public decimal UnitPrice { get; }

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value) return;

            _isSelected = value;
            if (!value)
            {
                _quantity = 1; // Reset quantity when deselected
            }

            // Batch notify: fire all property changes first, then notify parent once
            NotifyPropertyChanged(nameof(IsSelected));
            NotifyPropertyChanged(nameof(StrokeColor));
            NotifyPropertyChanged(nameof(BackgroundColor));
            NotifyPropertyChanged(nameof(CheckboxBackgroundColor));
            if (!value)
            {
                NotifyPropertyChanged(nameof(Quantity));
                NotifyPropertyChanged(nameof(TotalPrice));
            }

            _parentGroup.OnAdditionChanged();
            _onSelectionChanged?.Invoke();
        }
    }

    /// <summary>
    /// Sets IsSelected without triggering any callbacks or parent notifications.
    /// Used during initialization.
    /// </summary>
    internal void SetSelectedSilent(bool selected)
    {
        if (_isSelected == selected) return;
        _isSelected = selected;
        NotifyPropertyChanged(nameof(IsSelected));
        NotifyPropertyChanged(nameof(StrokeColor));
        NotifyPropertyChanged(nameof(BackgroundColor));
        NotifyPropertyChanged(nameof(CheckboxBackgroundColor));
    }

    /// <summary>
    /// Force sets the selected state, updating UI but not triggering parent/group callbacks.
    /// Used for single-select batch operations.
    /// </summary>
    public void ForceSetSelected(bool selected)
    {
        if (_isSelected == selected) return;

        _isSelected = selected;
        if (!selected)
        {
            _quantity = 1;
            NotifyPropertyChanged(nameof(Quantity));
            NotifyPropertyChanged(nameof(TotalPrice));
        }
        NotifyPropertyChanged(nameof(IsSelected));
        NotifyPropertyChanged(nameof(StrokeColor));
        NotifyPropertyChanged(nameof(BackgroundColor));
        NotifyPropertyChanged(nameof(CheckboxBackgroundColor));
    }

    public int Quantity
    {
        get => _quantity;
        set
        {
            if (_quantity == value || value < 0) return;

            _quantity = value;

            if (value == 0)
            {
                // Deselecting via quantity - handle as selection change
                _isSelected = false;
                NotifyPropertyChanged(nameof(IsSelected));
            }
            else if (!_isSelected)
            {
                // Auto-select when quantity > 0
                _isSelected = true;
                NotifyPropertyChanged(nameof(IsSelected));
            }

            NotifyPropertyChanged(nameof(Quantity));
            NotifyPropertyChanged(nameof(TotalPrice));

            _parentGroup.OnAdditionChanged();
            _onSelectionChanged?.Invoke();
        }
    }

    public bool IsDisabled
    {
        get => _isDisabled;
        set
        {
            if (_isDisabled == value) return;
            _isDisabled = value;
            NotifyPropertyChanged(nameof(IsDisabled));
            NotifyPropertyChanged(nameof(ItemOpacity));
        }
    }

    /// <summary>
    /// Sets disabled state without firing notifications (used during batch updates)
    /// </summary>
    internal void SetDisabledSilent(bool disabled)
    {
        if (_isDisabled == disabled) return;
        _isDisabled = disabled;
        NotifyPropertyChanged(nameof(IsDisabled));
        NotifyPropertyChanged(nameof(ItemOpacity));
    }

    public double ItemOpacity => _isDisabled ? 0.4 : 1.0;
    public decimal TotalPrice => UnitPrice * _quantity;
    public bool HasPrice => UnitPrice > 0;
    public decimal Price => UnitPrice; // Backward compatibility

    // Cached style properties to avoid converter overhead
    private static readonly Color PrimaryColor = Application.Current?.Resources.TryGetValue("Primary", out var primary) == true && primary is Color c1 ? c1 : Colors.Orange;
    private static readonly Color Gray200Color = Application.Current?.Resources.TryGetValue("Gray200", out var gray200) == true && gray200 is Color c2 ? c2 : Colors.LightGray;
    private static readonly Color Gray50Color = Application.Current?.Resources.TryGetValue("Gray50", out var gray50) == true && gray50 is Color c3 ? c3 : Color.FromRgb(249, 249, 249);
    private static readonly Color WhiteColor = Colors.White;
    private static readonly Color TransparentColor = Colors.Transparent;

    public Color StrokeColor => _isSelected ? PrimaryColor : Gray200Color;
    public Color BackgroundColor => _isSelected ? Gray50Color : WhiteColor;
    public Color CheckboxBackgroundColor => _isSelected ? PrimaryColor : TransparentColor;

    public event PropertyChangedEventHandler? PropertyChanged;

    private void NotifyPropertyChanged(string propertyName)
    {
        if (!_suppressNotifications)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public SelectableAddition(AdditionDto addition, SelectableAdditionGroup parentGroup, Action onSelectionChanged)
    {
        // Suppress all notifications during construction
        _suppressNotifications = true;

        AdditionId = addition.Id;
        Name = addition.Name ?? string.Empty;
        UnitPrice = addition.CurrentPrice;
        _parentGroup = parentGroup;
        _onSelectionChanged = onSelectionChanged;

        // Re-enable notifications after construction
        _suppressNotifications = false;
    }

    /// <summary>
    /// Reset to default unselected state for reuse
    /// </summary>
    public void ResetToDefault()
    {
        _suppressNotifications = true;
        _isSelected = false;
        _quantity = 1;
        _isDisabled = false;
        _suppressNotifications = false;

        // Fire property changes for UI update
        NotifyPropertyChanged(nameof(IsSelected));
        NotifyPropertyChanged(nameof(Quantity));
        NotifyPropertyChanged(nameof(IsDisabled));
        NotifyPropertyChanged(nameof(StrokeColor));
        NotifyPropertyChanged(nameof(BackgroundColor));
        NotifyPropertyChanged(nameof(CheckboxBackgroundColor));
        NotifyPropertyChanged(nameof(ItemOpacity));
        NotifyPropertyChanged(nameof(TotalPrice));
    }
}
