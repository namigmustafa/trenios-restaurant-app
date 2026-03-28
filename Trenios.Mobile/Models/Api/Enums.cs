namespace Trenios.Mobile.Models.Api;

public enum UserRole
{
    SuperAdmin = 1,
    RestaurantOwner = 2,
    BranchManager = 3,
    Cashier = 4
}

public enum OrderType
{
    DineIn = 1,
    TakeAway = 2,
    Delivery = 3
}

public enum OrderStatus
{
    Created = 1,
    Confirmed = 2,
    Preparing = 3,
    Completed = 4,
    Cancelled = 5
}

public enum Currency
{
    EUR = 1,
    USD = 2,
    AZN = 3,
    GBP = 4,
    TRY = 5,
    RUB = 6,
    DKK = 7
}

public enum SelectionType
{
    Single = 1,
    Multiple = 2
}

public enum DiscountType
{
    Percentage = 1,
    FixedAmount = 2
}

public enum DiscountTarget
{
    AllItems = 1,
    Category = 2,
    SpecificItem = 3
}

public enum ActivityPricingUnit
{
    PerMinute = 1,
    PerHour = 2
}

public enum ActivitySessionStatus
{
    Active = 1,
    Completed = 2,
    Cancelled = 3
}
