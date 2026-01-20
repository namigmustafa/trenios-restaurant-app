using System.Collections.ObjectModel;
using System.ComponentModel;
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
                OnPropertyChanged(nameof(CustomizationTotalPrice));
                ((Command)DecreaseCustomizationQuantityCommand).ChangeCanExecute();
            }
        }
    }

    public decimal CustomizationAdditionsTotal => SelectableAdditionGroups
        .SelectMany(g => g.Additions)
        .Where(a => a.IsSelected && a.Quantity > 0)
        .Sum(a => a.TotalPrice);

    public decimal CustomizationItemPrice => (SelectedMenuItem?.Price ?? 0) + CustomizationAdditionsTotal;

    public decimal CustomizationTotalPrice => CustomizationItemPrice * CustomizationQuantity;

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
            SelectedMenuItem = menuItem;
            CustomizationQuantity = 1;

            // Check if item has addition groups
            if (menuItem.AdditionGroups?.Count > 0)
            {
                IsLoadingAdditions = true;

                // Small delay for visual feedback
                await Task.Delay(100);

                // Build selectable addition groups
                SelectableAdditionGroups.Clear();
                foreach (var group in menuItem.AdditionGroups)
                {
                    if (group == null) continue;
                    var selectableGroup = new SelectableAdditionGroup(group, OnAdditionSelectionChanged);
                    SelectableAdditionGroups.Add(selectableGroup);
                }

                IsLoadingAdditions = false;
                ShowCustomization = true;
            }
            else
            {
                // Add directly to cart
                _orderService.AddItem(menuItem);
            }
        }
        catch (Exception ex)
        {
            IsLoadingAdditions = false;
            await Application.Current!.MainPage!.DisplayAlert("Error",
                $"{ex.GetType().Name}: {ex.Message}\n\n{ex.StackTrace}", "OK");
        }
    }

    private void OnAdditionSelectionChanged()
    {
        OnPropertyChanged(nameof(CustomizationAdditionsTotal));
        OnPropertyChanged(nameof(CustomizationItemPrice));
        OnPropertyChanged(nameof(CustomizationTotalPrice));
        ((Command)ConfirmCustomizationCommand).ChangeCanExecute();
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
        foreach (var group in SelectableAdditionGroups)
        {
            if (group.IsRequired)
            {
                // For MultiSelect, count total quantity of selected items; for SingleSelect, count selected items
                var totalSelections = group.IsMultiSelect
                    ? group.Additions.Where(a => a.IsSelected).Sum(a => a.Quantity)
                    : group.Additions.Count(a => a.IsSelected);

                if (totalSelections < group.MinSelections || totalSelections == 0)
                {
                    return false;
                }
            }
        }
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
/// Represents an addition group with selectable additions for the customization UI
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
    public ObservableCollection<SelectableAddition> Additions { get; } = new();

    private bool _isUpdating; // Guard against re-entrant updates

    public event PropertyChangedEventHandler? PropertyChanged;

    // Current total quantity selected in this group (only count selected additions)
    public int TotalSelected => Additions.Where(a => a.IsSelected).Sum(a => a.Quantity);

    // Remaining selections available
    public int RemainingSelections => MaxSelections > 0 ? Math.Max(0, MaxSelections - TotalSelected) : int.MaxValue;

    // Can add more selections
    public bool CanAddMore => MaxSelections == 0 || TotalSelected < MaxSelections;

    public SelectableAdditionGroup(AdditionGroupDto group, Action onSelectionChanged)
    {
        Id = group.Id;
        Name = group.Name ?? "Unknown";
        IsSingleSelect = group.IsSingleSelect;
        IsMultiSelect = group.IsMultiSelect;
        IsRequired = group.IsRequired;
        MinSelections = group.MinSelections;
        MaxSelections = group.MaxSelections;

        // Wrap the callback to also notify our properties and update disabled state
        Action wrappedCallback = () =>
        {
            // Guard against re-entrant calls
            if (_isUpdating) return;
            _isUpdating = true;
            try
            {
                onSelectionChanged();
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TotalSelected)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(RemainingSelections)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CanAddMore)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectionHint)));
                UpdateDisabledStates();
            }
            finally
            {
                _isUpdating = false;
            }
        };

        if (group.Additions != null)
        {
            var availableAdditions = group.Additions
                .Where(a => a != null && a.IsAvailable)
                .OrderBy(a => a.DisplayOrder)
                .ToList();
            HasOnlyOneAddition = availableAdditions.Count == 1;

            foreach (var addition in availableAdditions)
            {
                var selectable = new SelectableAddition(addition, wrappedCallback);
                Additions.Add(selectable);
            }

            // Auto-select first addition if:
            // 1. Only one addition exists, OR
            // 2. Required and single-select
            // Note: Use SetSelectedWithoutCallback to avoid triggering callbacks during construction
            if (HasOnlyOneAddition || (IsRequired && IsSingleSelect))
            {
                if (Additions.Count > 0)
                {
                    Additions[0].SetSelectedWithoutCallback(true);
                }
            }
        }
    }

    /// <summary>
    /// Updates disabled state for all additions based on max selections
    /// </summary>
    private void UpdateDisabledStates()
    {
        // No disabling needed for single-select (can always switch) or unlimited
        if (MaxSelections == 0 || IsSingleSelect) return;

        var canAdd = CanAddMore;
        foreach (var addition in Additions)
        {
            // Disable if max reached and this item is not selected
            addition.IsDisabled = !canAdd && !addition.IsSelected;
        }
    }

    public string SelectionHint
    {
        get
        {
            if (HasOnlyOneAddition)
                return "Required";
            if (IsSingleSelect)
                return IsRequired ? "Select one (required)" : "Select one (optional)";
            if (MaxSelections > 0)
            {
                if (IsRequired && MinSelections > 0)
                    return $"Select {MinSelections}-{MaxSelections} ({TotalSelected}/{MaxSelections})";
                return $"Select up to {MaxSelections} ({TotalSelected}/{MaxSelections})";
            }
            return "Select any";
        }
    }
}

/// <summary>
/// Represents a single selectable addition with selection state and quantity
/// </summary>
public class SelectableAddition : INotifyPropertyChanged
{
    private bool _isSelected;
    private int _quantity = 1;
    private bool _isDisabled;
    private readonly Action _onSelectionChanged;

    public Guid AdditionId { get; }
    public string Name { get; }
    public decimal UnitPrice { get; }

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected != value)
            {
                _isSelected = value;
                if (!value)
                {
                    _quantity = 1; // Reset quantity when deselected
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Quantity)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TotalPrice)));
                }
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
                _onSelectionChanged?.Invoke();
            }
        }
    }

    /// <summary>
    /// Sets IsSelected without triggering the callback (used during initial construction)
    /// </summary>
    public void SetSelectedWithoutCallback(bool selected)
    {
        _isSelected = selected;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
    }

    /// <summary>
    /// Force sets the selected state, updating UI but not triggering group callbacks.
    /// Used for single-select to batch all changes and call callback once at the end.
    /// </summary>
    public void ForceSetSelected(bool selected)
    {
        if (_isSelected == selected) return; // No change needed

        _isSelected = selected;
        if (!selected)
        {
            _quantity = 1; // Reset quantity when deselected
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Quantity)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TotalPrice)));
        }
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
    }

    public int Quantity
    {
        get => _quantity;
        set
        {
            if (_quantity != value && value >= 0)
            {
                _quantity = value;
                if (value == 0)
                {
                    IsSelected = false;
                }
                else if (!_isSelected && value > 0)
                {
                    _isSelected = true;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
                }
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Quantity)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TotalPrice)));
                _onSelectionChanged?.Invoke();
            }
        }
    }

    /// <summary>
    /// Item is disabled when max selections reached and this item is not selected
    /// </summary>
    public bool IsDisabled
    {
        get => _isDisabled;
        set
        {
            if (_isDisabled != value)
            {
                _isDisabled = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsDisabled)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ItemOpacity)));
            }
        }
    }

    public double ItemOpacity => IsDisabled ? 0.4 : 1.0;

    public decimal TotalPrice => UnitPrice * Quantity;
    public bool HasPrice => UnitPrice > 0;

    // Alias for backward compatibility
    public decimal Price => UnitPrice;

    public event PropertyChangedEventHandler? PropertyChanged;

    public SelectableAddition(AdditionDto addition, Action onSelectionChanged)
    {
        ArgumentNullException.ThrowIfNull(addition);
        AdditionId = addition.Id;
        Name = addition.Name ?? string.Empty;
        UnitPrice = addition.CurrentPrice;
        _onSelectionChanged = onSelectionChanged;
    }
}
