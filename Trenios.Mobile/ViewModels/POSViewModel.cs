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

    public ObservableCollection<CategoryDto> Categories { get; } = new();
    public ObservableCollection<BranchMenuItemDto> MenuItems { get; } = new();
    public ObservableCollection<CartItem> CartItems { get; } = new();
    public ObservableCollection<SelectableAdditionGroup> SelectableAdditionGroups { get; } = new();

    public string UserName => _authService.CurrentUser?.FullName ?? "Cashier";
    public string BranchName => _authService.GetEffectiveBranchName() ?? "Branch";

    // Back button visible for SuperAdmin and RestaurantOwner only
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

    public POSViewModel(ProductService productService, OrderService orderService, AuthService authService)
    {
        _productService = productService;
        _orderService = orderService;
        _authService = authService;

        // Initialize commands
        SelectCategoryCommand = new Command<CategoryDto>(cat => SelectedCategoryId = cat.Id);
        SelectMenuItemCommand = new Command<BranchMenuItemDto>(SelectMenuItem);
        IncreaseQuantityCommand = new Command<CartItem>(item => _orderService.UpdateQuantity(item.Id, item.Quantity + 1));
        DecreaseQuantityCommand = new Command<CartItem>(item => _orderService.UpdateQuantity(item.Id, item.Quantity - 1));
        RemoveItemCommand = new Command<CartItem>(item => _orderService.RemoveItem(item.Id));
        PayCommand = new Command(async () => await PayAsync(), () => HasItems);
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
        ViewOrdersCommand = new Command(async () => await Shell.Current.GoToAsync("orders"));
        ViewKitchenCommand = new Command(async () => await Shell.Current.GoToAsync("kitchen"));

        // Subscribe to cart changes
        _orderService.OnCartChanged += RefreshCart;

        // Load initial data
        _ = LoadDataAsync();
    }

    public async Task LoadDataAsync(bool forceRefresh = false)
    {
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
            var (categories, error) = await _productService.GetCategoriesAsync(forceRefresh);

            if (categories != null)
            {
                Categories.Clear();
                foreach (var cat in categories)
                {
                    Categories.Add(cat);
                }
            }
            else
            {
                ErrorMessage = error;
            }
        }
        finally
        {
            IsLoadingCategories = false;
        }
    }

    private async Task LoadMenuItemsAsync()
    {
        if (SelectedCategoryId == null) return;

        IsLoadingProducts = true;
        ErrorMessage = null;

        try
        {
            var (items, error) = await _productService.GetMenuItemsByCategoryAsync(SelectedCategoryId.Value);

            if (items != null)
            {
                MenuItems.Clear();
                foreach (var item in items)
                {
                    MenuItems.Add(item);
                }
            }
            else
            {
                ErrorMessage = error;
            }
        }
        finally
        {
            IsLoadingProducts = false;
        }
    }

    private async void SelectMenuItem(BranchMenuItemDto menuItem)
    {
        if (IsLoadingAdditions) return;

        try
        {
            IsLoadingAdditions = true;

            SelectedMenuItem = menuItem;
            CustomizationQuantity = 1;

            // Count total additions across all groups
            var totalAdditionsCount = 0;
            if (menuItem.AdditionGroups != null)
            {
                foreach (var group in menuItem.AdditionGroups)
                {
                    if (group?.Additions != null)
                    {
                        totalAdditionsCount += group.Additions.Count;
                    }
                }
            }

            System.Diagnostics.Debug.WriteLine($"MenuItem: {menuItem.MenuItemName}, Groups: {menuItem.AdditionGroups?.Count ?? 0}, Total Additions: {totalAdditionsCount}");

            // Always show customization popup
            // Build selectable addition groups if there are any
            SelectableAdditionGroups.Clear();

            if (totalAdditionsCount > 0 && menuItem.AdditionGroups != null)
            {
                foreach (var group in menuItem.AdditionGroups)
                {
                    if (group == null || group.Additions == null || group.Additions.Count == 0) continue;
                    SelectableAdditionGroups.Add(new SelectableAdditionGroup(group, OnAdditionSelectionChanged));
                }
            }

            // Update cached totals after groups are built
            RecalculateCustomizationTotals();

            // Evaluate button state initially (important for required groups)
            ((Command)ConfirmCustomizationCommand).ChangeCanExecute();

            // Small delay to show the loading spinner
            await Task.Delay(100);

            // Always show the customization popup (even if no additions)
            ShowCustomization = true;
        }
        catch (Exception ex)
        {
            await Application.Current!.MainPage!.DisplayAlert("Error",
                $"{ex.GetType().Name}: {ex.Message}\n\n{ex.StackTrace}", "OK");
        }
        finally
        {
            IsLoadingAdditions = false;
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
        SelectedMenuItem = null;
        SelectableAdditionGroups.Clear();
        CustomizationQuantity = 1;
        ValidationMessage = null;
        OnPropertyChanged(nameof(HasValidationError));
    }

    private async Task PayAsync()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;

            // Show confirmation
            var confirm = await Application.Current!.MainPage!.DisplayAlert(
                "Complete Order",
                $"Total: €{Total:F2}\n\nSubmit order?",
                "Submit",
                "Cancel");

            if (!confirm) return;

            var (order, error) = await _orderService.SubmitOrderAsync();

            if (order != null)
            {
                await Application.Current.MainPage.DisplayAlert(
                    "Order Submitted",
                    $"Order #{order.OrderNumber}\nTotal: €{order.TotalAmount:F2}",
                    "OK");
            }
            else
            {
                await Application.Current.MainPage.DisplayAlert(
                    "Error",
                    error ?? "Failed to submit order",
                    "OK");
            }
        }
        finally
        {
            IsBusy = false;
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
            await _authService.LogoutAsync();
            await Shell.Current.GoToAsync("//LoginPage");
        }
    }

    private async Task GoBackAsync()
    {
        if (CanGoBack)
        {
            _productService.ClearCache();
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
        CartItems.Clear();
        foreach (var item in _orderService.CartItems)
        {
            CartItems.Add(item);
        }

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

    public event PropertyChangedEventHandler? PropertyChanged;

    public int TotalSelected => _cachedTotalSelected;
    public int RemainingSelections => MaxSelections > 0 ? Math.Max(0, MaxSelections - _cachedTotalSelected) : int.MaxValue;
    public bool CanAddMore => _cachedCanAddMore;
    public string SelectionHint => _cachedSelectionHint;

    public SelectableAdditionGroup(AdditionGroupDto group, Action onSelectionChanged)
    {
        Id = group.Id;
        Name = group.Name ?? "Unknown";
        IsSingleSelect = group.IsSingleSelect;
        IsMultiSelect = group.IsMultiSelect;
        IsRequired = group.IsRequired;
        MinSelections = group.MinSelections;
        MaxSelections = group.MaxSelections;

        if (group.Additions != null)
        {
            var availableAdditions = group.Additions
                .Where(a => a != null && a.IsAvailable)
                .OrderBy(a => a.DisplayOrder)
                .ToList();

            HasOnlyOneAddition = availableAdditions.Count == 1;

            // Create all additions first without any callbacks
            foreach (var addition in availableAdditions)
            {
                var selectable = new SelectableAddition(addition, this, onSelectionChanged);
                _additions.Add(selectable);
            }

            // Auto-select first addition if needed (without triggering callbacks)
            if (HasOnlyOneAddition || (IsRequired && IsSingleSelect))
            {
                if (_additions.Count > 0)
                {
                    _additions[0].SetSelectedSilent(true);
                }
            }

            // Calculate initial cached values (no notifications during init)
            RecalculateCachedValues();
        }
        else
        {
            HasOnlyOneAddition = false;
            _cachedSelectionHint = ComputeSelectionHint();
        }
    }

    /// <summary>
    /// Called by child additions when selection/quantity changes.
    /// Recalculates cached values and fires a single batch of notifications.
    /// </summary>
    internal void OnAdditionChanged()
    {
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

    public event PropertyChangedEventHandler? PropertyChanged;

    private void NotifyPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public SelectableAddition(AdditionDto addition, SelectableAdditionGroup parentGroup, Action onSelectionChanged)
    {
        ArgumentNullException.ThrowIfNull(addition);
        ArgumentNullException.ThrowIfNull(parentGroup);

        AdditionId = addition.Id;
        Name = addition.Name ?? string.Empty;
        UnitPrice = addition.CurrentPrice;
        _parentGroup = parentGroup;
        _onSelectionChanged = onSelectionChanged;
    }
}
