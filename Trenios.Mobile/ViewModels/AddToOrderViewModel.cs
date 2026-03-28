using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using Trenios.Mobile.Models.Api;
using Trenios.Mobile.Services;
using Trenios.Mobile.Helpers;

namespace Trenios.Mobile.ViewModels;

public class AddToOrderViewModel : INotifyPropertyChanged, IQueryAttributable
{
    private readonly ProductService _productService;
    private readonly ApiService _apiService;

    private Guid _orderId;
    private string _orderNumber = string.Empty;
    private Guid? _selectedCategoryId;
    private bool _isLoading;
    private bool _isSubmitting;
    private string _searchText = string.Empty;

    // Customization overlay state
    private BranchMenuItemDto? _selectedMenuItem;
    private bool _showCustomization;
    private bool _isLoadingAdditions;
    private int _customizationQuantity = 1;
    private string? _validationMessage;
    private decimal _cachedAdditionsTotal;
    private decimal _cachedItemPrice;
    private decimal _cachedTotalPrice;
    private readonly Dictionary<Guid, List<SelectableAdditionGroup>> _groupsCache = new();

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<SelectableCategory> Categories { get; } = new();
    public ObservableCollection<BranchMenuItemDto> MenuItems { get; } = new();
    public ObservableCollection<CartItem> CartItems { get; } = new();
    public ObservableCollection<SelectableAdditionGroup> SelectableAdditionGroups { get; } = new();

    public string OrderNumber
    {
        get => _orderNumber;
        private set { _orderNumber = value; OnPropertyChanged(nameof(OrderNumber)); }
    }

    public bool IsAllSelected => SelectedCategoryId == null;

    public Guid? SelectedCategoryId
    {
        get => _selectedCategoryId;
        set
        {
            _selectedCategoryId = value;
            foreach (var cat in Categories)
                cat.IsSelected = cat.Id == value;
            OnPropertyChanged(nameof(SelectedCategoryId));
            OnPropertyChanged(nameof(IsAllSelected));
            _ = LoadMenuItemsAsync();
        }
    }

    public bool IsLoading
    {
        get => _isLoading;
        set { _isLoading = value; OnPropertyChanged(nameof(IsLoading)); }
    }

    public bool IsSubmitting
    {
        get => _isSubmitting;
        set { _isSubmitting = value; OnPropertyChanged(nameof(IsSubmitting)); OnPropertyChanged(nameof(CanSubmit)); }
    }

    public string SearchText
    {
        get => _searchText;
        set
        {
            _searchText = value;
            OnPropertyChanged(nameof(SearchText));
            OnPropertyChanged(nameof(FilteredMenuItems));
        }
    }

    // Customization properties
    public BranchMenuItemDto? SelectedMenuItem
    {
        get => _selectedMenuItem;
        set { _selectedMenuItem = value; OnPropertyChanged(nameof(SelectedMenuItem)); }
    }

    public bool ShowCustomization
    {
        get => _showCustomization;
        set { _showCustomization = value; OnPropertyChanged(nameof(ShowCustomization)); }
    }

    public bool IsLoadingAdditions
    {
        get => _isLoadingAdditions;
        set { _isLoadingAdditions = value; OnPropertyChanged(nameof(IsLoadingAdditions)); }
    }

    public int CustomizationQuantity
    {
        get => _customizationQuantity;
        set
        {
            if (value >= 1 && _customizationQuantity != value)
            {
                _customizationQuantity = value;
                OnPropertyChanged(nameof(CustomizationQuantity));
                RecalculateCustomizationTotals();
                ((Command)DecreaseCustomizationQuantityCommand).ChangeCanExecute();
            }
        }
    }

    public string? ValidationMessage
    {
        get => _validationMessage;
        private set { _validationMessage = value; OnPropertyChanged(nameof(ValidationMessage)); OnPropertyChanged(nameof(HasValidationError)); }
    }

    public bool HasValidationError => !string.IsNullOrEmpty(_validationMessage);
    public string CustomizationItemPriceDisplay => CurrencyFormatter.Format(_cachedItemPrice);
    public string CustomizationTotalPriceDisplay => CurrencyFormatter.Format(_cachedTotalPrice);

    public IEnumerable<BranchMenuItemDto> FilteredMenuItems =>
        string.IsNullOrWhiteSpace(SearchText)
            ? MenuItems
            : MenuItems.Where(m => m.MenuItemName.Contains(SearchText, StringComparison.OrdinalIgnoreCase));

    public decimal Subtotal => CartItems.Sum(i => i.TotalPrice);
    public string SubtotalDisplay => CurrencyFormatter.Format(Subtotal);
    public int TotalItems => CartItems.Sum(i => i.Quantity);
    public bool HasItems => CartItems.Count > 0;
    public bool CanSubmit => HasItems && !IsSubmitting;

    public ICommand SelectCategoryCommand { get; }
    public ICommand SelectAllCategoriesCommand { get; }
    public ICommand SelectMenuItemCommand { get; }
    public ICommand IncreaseQuantityCommand { get; }
    public ICommand DecreaseQuantityCommand { get; }
    public ICommand RemoveItemCommand { get; }
    public ICommand SubmitCommand { get; }
    public ICommand CloseCommand { get; }

    // Customization commands
    public ICommand ToggleAdditionCommand { get; }
    public ICommand ConfirmCustomizationCommand { get; }
    public ICommand CancelCustomizationCommand { get; }
    public ICommand IncreaseCustomizationQuantityCommand { get; }
    public ICommand DecreaseCustomizationQuantityCommand { get; }
    public ICommand IncreaseAdditionQuantityCommand { get; }
    public ICommand DecreaseAdditionQuantityCommand { get; }

    public AddToOrderViewModel(ProductService productService, ApiService apiService)
    {
        _productService = productService;
        _apiService = apiService;

        SelectCategoryCommand = new Command<SelectableCategory>(cat => SelectedCategoryId = cat.Id);
        SelectAllCategoriesCommand = new Command(() => { SelectedCategoryId = null; _ = LoadMenuItemsAsync(); });
        SelectMenuItemCommand = new Command<BranchMenuItemDto>(item => SelectMenuItem(item));
        IncreaseQuantityCommand = new Command<CartItem>(item => UpdateQuantity(item, item.Quantity + 1));
        DecreaseQuantityCommand = new Command<CartItem>(item => UpdateQuantity(item, item.Quantity - 1));
        RemoveItemCommand = new Command<CartItem>(RemoveItem);
        SubmitCommand = new Command(async () => await SubmitAsync(), () => CanSubmit);
        CloseCommand = new Command(async () => await Shell.Current.GoToAsync(".."));

        ToggleAdditionCommand = new Command<SelectableAddition>(ToggleAddition);
        ConfirmCustomizationCommand = new Command(ConfirmCustomization, CanConfirmCustomization);
        CancelCustomizationCommand = new Command(CancelCustomization);
        IncreaseCustomizationQuantityCommand = new Command(() => CustomizationQuantity++);
        DecreaseCustomizationQuantityCommand = new Command(() => { if (CustomizationQuantity > 1) CustomizationQuantity--; }, () => CustomizationQuantity > 1);
        IncreaseAdditionQuantityCommand = new Command<SelectableAddition>(IncreaseAdditionQuantity);
        DecreaseAdditionQuantityCommand = new Command<SelectableAddition>(DecreaseAdditionQuantity);

        MenuItems.CollectionChanged += (_, _) => OnPropertyChanged(nameof(FilteredMenuItems));
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("orderId", out var id) && Guid.TryParse(id?.ToString(), out var guid))
            _orderId = guid;
        if (query.TryGetValue("orderNumber", out var num))
            OrderNumber = num?.ToString() ?? string.Empty;
    }

    public async Task InitializeAsync()
    {
        IsLoading = true;
        CartItems.Clear();
        RefreshTotals();
        try
        {
            await LoadCategoriesAsync();
            await LoadMenuItemsAsync();
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadCategoriesAsync()
    {
        var (categories, _) = await _productService.GetCategoriesAsync();
        if (categories == null) return;

        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            Categories.Clear();
            foreach (var cat in categories)
                Categories.Add(new SelectableCategory { Category = cat });
        });
    }

    private async Task LoadMenuItemsAsync()
    {
        List<BranchMenuItemDto>? items;
        if (SelectedCategoryId.HasValue)
        {
            var (filtered, _) = await _productService.GetMenuItemsByCategoryAsync(SelectedCategoryId.Value);
            items = filtered;
        }
        else
        {
            var (all, _) = await _productService.GetMenuItemsAsync();
            items = all;
        }

        if (items == null) return;

        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            MenuItems.Clear();
            foreach (var item in items)
                MenuItems.Add(item);
            OnPropertyChanged(nameof(FilteredMenuItems));
        });
    }

    private async void SelectMenuItem(BranchMenuItemDto menuItem)
    {
        if (IsLoadingAdditions) return;

        // If same item reopened, reset and show instantly
        if (SelectedMenuItem?.MenuItemId == menuItem.MenuItemId && SelectableAdditionGroups.Count > 0)
        {
            CustomizationQuantity = 1;
            foreach (var group in SelectableAdditionGroups)
                group.ResetSelections();
            RecalculateCustomizationTotals();
            ((Command)ConfirmCustomizationCommand).ChangeCanExecute();
            ShowCustomization = true;
            return;
        }

        IsLoadingAdditions = true;
        await Task.Delay(50);

        SelectedMenuItem = menuItem;
        CustomizationQuantity = 1;

        if (_groupsCache.TryGetValue(menuItem.MenuItemId, out var cachedGroups))
        {
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

        // No additions configured — add directly without showing overlay
        if (menuItem.AdditionGroups == null || menuItem.AdditionGroups.Count == 0)
        {
            IsLoadingAdditions = false;
            AddMenuItem(menuItem, 1, new List<SelectedAddition>());
            return;
        }

        ShowCustomization = true;

        await Task.Run(() =>
        {
            var tempGroups = new List<SelectableAdditionGroup>();
            foreach (var group in menuItem.AdditionGroups)
            {
                if (group?.Additions?.Count == 0) continue;
                tempGroups.Add(new SelectableAdditionGroup(group, OnAdditionSelectionChanged));
            }

            _groupsCache[menuItem.MenuItemId] = tempGroups;

            MainThread.BeginInvokeOnMainThread(() =>
            {
                SelectableAdditionGroups.Clear();
                foreach (var group in tempGroups)
                    SelectableAdditionGroups.Add(group);
                RecalculateCustomizationTotals();
                ((Command)ConfirmCustomizationCommand).ChangeCanExecute();
                IsLoadingAdditions = false;
            });
        });
    }

    private void OnAdditionSelectionChanged()
    {
        RecalculateCustomizationTotals();
        ((Command)ConfirmCustomizationCommand).ChangeCanExecute();
    }

    private void RecalculateCustomizationTotals()
    {
        decimal additionsTotal = 0;
        foreach (var group in SelectableAdditionGroups)
            foreach (var addition in group.Additions)
                if (addition.IsSelected && addition.Quantity > 0)
                    additionsTotal += addition.TotalPrice;

        var basePrice = SelectedMenuItem?.Price ?? 0;
        var itemPrice = basePrice + additionsTotal;
        var totalPrice = itemPrice * CustomizationQuantity;

        _cachedAdditionsTotal = additionsTotal;
        _cachedItemPrice = itemPrice;
        _cachedTotalPrice = totalPrice;

        OnPropertyChanged(nameof(CustomizationItemPriceDisplay));
        OnPropertyChanged(nameof(CustomizationTotalPriceDisplay));
    }

    private void ToggleAddition(SelectableAddition addition)
    {
        var group = SelectableAdditionGroups.FirstOrDefault(g => g.Additions.Contains(addition));
        if (group == null) return;

        if (!group.IsSingleSelect && addition.IsDisabled && !addition.IsSelected) return;

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
            foreach (var a in group.Additions)
                a.ForceSetSelected(a == addition ? (group.IsRequired || !a.IsSelected) : false);
        }
        else
        {
            if (addition.IsSelected)
            {
                if (!group.IsRequired || group.TotalSelected > group.MinSelections)
                    addition.IsSelected = false;
            }
            else if (group.CanAddMore)
            {
                addition.IsSelected = true;
            }
        }

        OnAdditionSelectionChanged();
    }

    private void IncreaseAdditionQuantity(SelectableAddition addition)
    {
        var group = SelectableAdditionGroups.FirstOrDefault(g => g.Additions.Contains(addition));
        if (group == null || group.IsSingleSelect) return;
        if (group.CanAddMore) addition.Quantity++;
    }

    private void DecreaseAdditionQuantity(SelectableAddition addition)
    {
        var group = SelectableAdditionGroups.FirstOrDefault(g => g.Additions.Contains(addition));
        if (group == null || group.IsSingleSelect) return;
        if (addition.Quantity > 0) addition.Quantity--;
    }

    private bool CanConfirmCustomization()
    {
        var missing = new List<string>();
        foreach (var group in SelectableAdditionGroups)
        {
            if (group.IsRequired && (group.TotalSelected < group.MinSelections || group.TotalSelected == 0))
                missing.Add(group.Name);
        }

        if (missing.Count > 0)
        {
            ValidationMessage = $"Please select: {string.Join(", ", missing)}";
            return false;
        }

        ValidationMessage = null;
        return true;
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

            AddMenuItem(SelectedMenuItem, CustomizationQuantity, selectedAdditions);
        }

        CancelCustomization();
    }

    private void CancelCustomization()
    {
        ShowCustomization = false;
        CustomizationQuantity = 1;
        ValidationMessage = null;
    }

    private void AddMenuItem(BranchMenuItemDto menuItem, int quantity, List<SelectedAddition> additions)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            var existingIdx = -1;
            CartItem? existing = null;
            for (int i = 0; i < CartItems.Count; i++)
            {
                if (CartItems[i].MenuItemId == menuItem.MenuItemId &&
                    AdditionsKey(CartItems[i].Additions) == AdditionsKey(additions))
                {
                    existing = CartItems[i];
                    existingIdx = i;
                    break;
                }
            }

            if (existing != null)
            {
                // Replace with a new object so BindableLayout re-renders
                var updated = new CartItem
                {
                    Id = existing.Id,
                    MenuItemId = existing.MenuItemId,
                    MenuItemName = existing.MenuItemName,
                    OriginalUnitPrice = existing.OriginalUnitPrice,
                    DiscountedUnitPrice = existing.DiscountedUnitPrice,
                    Quantity = existing.Quantity + quantity,
                    Additions = existing.Additions
                };
                CartItems.RemoveAt(existingIdx);
                CartItems.Insert(existingIdx, updated);
            }
            else
            {
                CartItems.Add(new CartItem
                {
                    Id = Guid.NewGuid(),
                    MenuItemId = menuItem.MenuItemId,
                    MenuItemName = menuItem.MenuItemName,
                    OriginalUnitPrice = menuItem.Price,
                    DiscountedUnitPrice = menuItem.Price,
                    Quantity = quantity,
                    Additions = additions
                });
            }

            RefreshTotals();
        });
    }

    private void UpdateQuantity(CartItem item, int quantity)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (quantity <= 0)
            {
                CartItems.Remove(item);
            }
            else
            {
                var idx = CartItems.IndexOf(item);
                if (idx >= 0)
                {
                    item.Quantity = quantity;
                    CartItems.RemoveAt(idx);
                    CartItems.Insert(idx, item);
                }
            }
            RefreshTotals();
        });
    }

    private void RemoveItem(CartItem item)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            CartItems.Remove(item);
            RefreshTotals();
        });
    }

    private void RefreshTotals()
    {
        OnPropertyChanged(nameof(Subtotal));
        OnPropertyChanged(nameof(SubtotalDisplay));
        OnPropertyChanged(nameof(TotalItems));
        OnPropertyChanged(nameof(HasItems));
        OnPropertyChanged(nameof(CanSubmit));
        ((Command)SubmitCommand).ChangeCanExecute();
    }

    private async Task SubmitAsync()
    {
        if (!HasItems || _orderId == Guid.Empty) return;

        IsSubmitting = true;
        try
        {
            // PUT /api/orders/{id} does full item replacement.
            // Fetch existing items first, then merge with new cart items.
            var getResult = await _apiService.GetAsync<OrderResponse>($"/api/orders/{_orderId}");
            if (!getResult.IsSuccess || getResult.Data == null)
            {
                var loc = LocalizationService.Instance;
                await Application.Current?.MainPage?.DisplayAlert(
                    loc["Error"],
                    getResult.ErrorMessage ?? "Could not load existing order",
                    loc["OK"]);
                return;
            }

            // Map existing order items → CreateOrderItemRequest
            var existingItems = getResult.Data.Items.Select(i => new CreateOrderItemRequest
            {
                MenuItemId = i.MenuItemId,
                Quantity = i.Quantity,
                ExpectedUnitPrice = i.UnitPrice,
                Additions = i.Additions?
                    .Where(a => a.AdditionId != Guid.Empty)
                    .Select(a => new CreateOrderItemAdditionRequest
                    {
                        AdditionId = a.AdditionId,
                        UnitPrice = a.UnitPrice,
                        Quantity = a.Quantity
                    }).ToList()
            }).ToList();

            // Map new cart items → CreateOrderItemRequest
            var newItems = CartItems.Select(item => new CreateOrderItemRequest
            {
                MenuItemId = item.MenuItemId,
                Quantity = item.Quantity,
                ExpectedUnitPrice = item.DiscountedUnitPrice,
                Additions = item.Additions.Count > 0
                    ? item.Additions.Select(a => new CreateOrderItemAdditionRequest
                    {
                        AdditionId = a.AdditionId,
                        UnitPrice = a.UnitPrice,
                        Quantity = a.Quantity
                    }).ToList()
                    : null
            }).ToList();

            var request = new AddOrderItemsRequest
            {
                Items = existingItems.Concat(newItems).ToList()
            };

            var result = await _apiService.PutAsync<OrderResponse>($"/api/orders/{_orderId}", request);

            if (result.IsSuccess)
            {
                await Shell.Current.GoToAsync("..");
            }
            else
            {
                var loc = LocalizationService.Instance;
                await Application.Current?.MainPage?.DisplayAlert(
                    loc["Error"],
                    result.ErrorMessage ?? "An error occurred",
                    loc["OK"]);
            }
        }
        finally
        {
            IsSubmitting = false;
        }
    }

    private static string AdditionsKey(List<SelectedAddition> additions) =>
        additions.Count == 0
            ? string.Empty
            : string.Join("|", additions
                .OrderBy(a => a.AdditionId)
                .Select(a => $"{a.AdditionId}:{a.Quantity}"));

    protected void OnPropertyChanged(string propertyName)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
