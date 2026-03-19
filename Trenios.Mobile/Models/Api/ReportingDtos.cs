using System.Text.Json.Serialization;
using Trenios.Mobile.Services;

namespace Trenios.Mobile.Models.Api;

public class SalesSummaryReport
{
    [JsonPropertyName("startDate")]
    public DateTime StartDate { get; set; }

    [JsonPropertyName("endDate")]
    public DateTime EndDate { get; set; }

    [JsonPropertyName("branchId")]
    public Guid? BranchId { get; set; }

    [JsonPropertyName("branchName")]
    public string? BranchName { get; set; }

    [JsonPropertyName("totalRevenue")]
    public decimal TotalRevenue { get; set; }

    [JsonPropertyName("totalOrders")]
    public int TotalOrders { get; set; }

    [JsonPropertyName("averageOrderValue")]
    public decimal AverageOrderValue { get; set; }

    [JsonPropertyName("totalTaxCollected")]
    public decimal TotalTaxCollected { get; set; }

    [JsonPropertyName("totalDiscountsGiven")]
    public decimal TotalDiscountsGiven { get; set; }

    [JsonPropertyName("netRevenue")]
    public decimal NetRevenue { get; set; }

    [JsonPropertyName("completedOrders")]
    public int CompletedOrders { get; set; }

    [JsonPropertyName("cancelledOrders")]
    public int CancelledOrders { get; set; }

    [JsonPropertyName("completionRate")]
    public decimal CompletionRate { get; set; }

    [JsonPropertyName("previousPeriod")]
    public PreviousPeriodData? PreviousPeriod { get; set; }

    // Derived helpers
    [JsonIgnore]
    public int InProgressOrders => TotalOrders - CompletedOrders - CancelledOrders;

    [JsonIgnore]
    public string TotalRevenueDisplay => $"€{TotalRevenue:F2}";

    [JsonIgnore]
    public string AverageOrderValueDisplay => $"€{AverageOrderValue:F2}";

    [JsonIgnore]
    public string CompletionRateDisplay => $"{CompletionRate:F1}%";

    [JsonIgnore]
    public double CompletionRateProgress => (double)(CompletionRate / 100m);
}

public class PreviousPeriodData
{
    [JsonPropertyName("startDate")]
    public DateTime StartDate { get; set; }

    [JsonPropertyName("endDate")]
    public DateTime EndDate { get; set; }

    [JsonPropertyName("totalRevenue")]
    public decimal TotalRevenue { get; set; }

    [JsonPropertyName("totalOrders")]
    public int TotalOrders { get; set; }

    [JsonPropertyName("averageOrderValue")]
    public decimal AverageOrderValue { get; set; }

    [JsonPropertyName("revenueChange")]
    public decimal RevenueChange { get; set; }

    [JsonPropertyName("revenueChangePercent")]
    public decimal RevenueChangePercent { get; set; }

    [JsonPropertyName("ordersChange")]
    public int OrdersChange { get; set; }

    [JsonPropertyName("ordersChangePercent")]
    public decimal OrdersChangePercent { get; set; }

    [JsonPropertyName("aovChange")]
    public decimal AovChange { get; set; }

    [JsonPropertyName("aovChangePercent")]
    public decimal AovChangePercent { get; set; }

    [JsonIgnore]
    public string RevenueChangeDisplay => RevenueChangePercent >= 0
        ? $"+{RevenueChangePercent:F2}%"
        : $"{RevenueChangePercent:F2}%";

    [JsonIgnore]
    public string OrdersChangeDisplay => OrdersChangePercent >= 0
        ? $"+{OrdersChangePercent:F1}%"
        : $"{OrdersChangePercent:F1}%";

    [JsonIgnore]
    public string AovChangeDisplay => AovChangePercent >= 0
        ? $"+{AovChangePercent:F1}%"
        : $"{AovChangePercent:F1}%";

    [JsonIgnore]
    public Color RevenueChangeColor => RevenueChangePercent >= 0
        ? Color.FromRgb(39, 174, 96)
        : Color.FromRgb(231, 76, 60);

    [JsonIgnore]
    public Color OrdersChangeColor => OrdersChangePercent >= 0
        ? Color.FromRgb(39, 174, 96)
        : Color.FromRgb(231, 76, 60);

    [JsonIgnore]
    public Color AovChangeColor => AovChangePercent >= 0
        ? Color.FromRgb(39, 174, 96)
        : Color.FromRgb(231, 76, 60);
}

public class OrdersSummaryReport
{
    [JsonPropertyName("totalRevenue")]
    public decimal TotalRevenue { get; set; }

    [JsonPropertyName("totalOrders")]
    public int TotalOrders { get; set; }

    [JsonPropertyName("completedOrders")]
    public int CompletedOrders { get; set; }

    [JsonPropertyName("cancelledOrders")]
    public int CancelledOrders { get; set; }

    [JsonIgnore]
    public string TotalRevenueDisplay => $"€{TotalRevenue:F2}";
}

public class OrderTypeReport
{
    [JsonPropertyName("totalRevenue")]
    public decimal TotalRevenue { get; set; }

    [JsonPropertyName("totalOrders")]
    public int TotalOrders { get; set; }

    [JsonPropertyName("orderTypes")]
    public List<OrderTypeItem> OrderTypes { get; set; } = new();
}

public class OrderTypeItem
{
    [JsonPropertyName("type")]
    public int Type { get; set; }

    [JsonPropertyName("typeName")]
    public string TypeName { get; set; } = string.Empty;

    [JsonPropertyName("orders")]
    public int Orders { get; set; }

    [JsonPropertyName("revenue")]
    public decimal Revenue { get; set; }

    [JsonPropertyName("averageOrderValue")]
    public decimal AverageOrderValue { get; set; }

    [JsonPropertyName("percentOfTotal")]
    public decimal PercentOfTotal { get; set; }

    [JsonPropertyName("previousPeriod")]
    public OrderTypePreviousPeriod? PreviousPeriod { get; set; }

    [JsonIgnore]
    public string RevenueDisplay => $"€{Revenue:F2}";

    [JsonIgnore]
    public string PercentDisplay => $"{PercentOfTotal:F1}%";

    [JsonIgnore]
    public double PercentProgress => (double)(PercentOfTotal / 100m);

    [JsonIgnore]
    public string OrdersChangeDisplay
    {
        get
        {
            if (PreviousPeriod == null) return string.Empty;
            return PreviousPeriod.OrdersChangePercent >= 0
                ? $"↑ +{PreviousPeriod.OrdersChangePercent:F1}%"
                : $"↓ {PreviousPeriod.OrdersChangePercent:F1}%";
        }
    }

    [JsonIgnore]
    public Color OrdersChangeColor => PreviousPeriod?.OrdersChangePercent >= 0
        ? Color.FromRgb(39, 174, 96)
        : Color.FromRgb(231, 76, 60);
}

public class OrderTypePreviousPeriod
{
    [JsonPropertyName("orders")]
    public int Orders { get; set; }

    [JsonPropertyName("revenue")]
    public decimal Revenue { get; set; }

    [JsonPropertyName("ordersChange")]
    public int OrdersChange { get; set; }

    [JsonPropertyName("ordersChangePercent")]
    public decimal OrdersChangePercent { get; set; }

    [JsonPropertyName("revenueChange")]
    public decimal RevenueChange { get; set; }

    [JsonPropertyName("revenueChangePercent")]
    public decimal RevenueChangePercent { get; set; }
}

public class SalesByTimePeriodReport
{
    [JsonPropertyName("startDate")]
    public DateTime StartDate { get; set; }

    [JsonPropertyName("endDate")]
    public DateTime EndDate { get; set; }

    [JsonPropertyName("branchId")]
    public Guid? BranchId { get; set; }

    [JsonPropertyName("branchName")]
    public string? BranchName { get; set; }

    [JsonPropertyName("totalRevenue")]
    public decimal TotalRevenue { get; set; }

    [JsonPropertyName("totalOrders")]
    public int TotalOrders { get; set; }

    [JsonPropertyName("periods")]
    public List<SalesPeriodItem> Periods { get; set; } = new();
}

public class SalesPeriodItem
{
    [JsonPropertyName("periodName")]
    public string PeriodName { get; set; } = string.Empty;

    [JsonPropertyName("startHour")]
    public int StartHour { get; set; }

    [JsonPropertyName("endHour")]
    public int EndHour { get; set; }

    [JsonPropertyName("orders")]
    public int Orders { get; set; }

    [JsonPropertyName("revenue")]
    public decimal Revenue { get; set; }

    [JsonPropertyName("averageOrderValue")]
    public decimal AverageOrderValue { get; set; }

    [JsonPropertyName("percentOfTotal")]
    public decimal PercentOfTotal { get; set; }

    [JsonIgnore]
    public string RevenueDisplay => $"€{Revenue:F2}";

    [JsonIgnore]
    public string PercentDisplay => $"{PercentOfTotal:F1}%";

    [JsonIgnore]
    public string TimeRangeDisplay => $"{StartHour:D2}:00 - {EndHour:D2}:00 • {Orders} {LocalizationService.Instance.Get("Orders")}";

    [JsonIgnore]
    public string PeriodNameDisplay => PeriodName switch
    {
        "Morning"    => LocalizationService.Instance.Get("Morning"),
        "Lunch"      => LocalizationService.Instance.Get("Lunch"),
        "Afternoon"  => LocalizationService.Instance.Get("Afternoon"),
        "Dinner"     => LocalizationService.Instance.Get("Dinner"),
        "Late Night" => LocalizationService.Instance.Get("LateNight"),
        _            => PeriodName
    };

    [JsonIgnore]
    public string PeriodIcon => PeriodName switch
    {
        "Morning"    => "☀️",
        "Lunch"      => "🍽️",
        "Afternoon"  => "🌤️",
        "Dinner"     => "🌙",
        "Late Night" => "🌛",
        _            => "🕐"
    };
}

public class SalesByHourReport
{
    [JsonPropertyName("startDate")]
    public DateTime StartDate { get; set; }

    [JsonPropertyName("endDate")]
    public DateTime EndDate { get; set; }

    [JsonPropertyName("branchId")]
    public Guid? BranchId { get; set; }

    [JsonPropertyName("branchName")]
    public string? BranchName { get; set; }

    [JsonPropertyName("totalRevenue")]
    public decimal TotalRevenue { get; set; }

    [JsonPropertyName("totalOrders")]
    public int TotalOrders { get; set; }

    [JsonPropertyName("hours")]
    public List<SalesHourItem> Hours { get; set; } = new();
}

public class SalesHourItem
{
    [JsonPropertyName("hour")]
    public int Hour { get; set; }

    [JsonPropertyName("hourLabel")]
    public string HourLabel { get; set; } = string.Empty;

    [JsonPropertyName("orders")]
    public int Orders { get; set; }

    [JsonPropertyName("revenue")]
    public decimal Revenue { get; set; }

    [JsonPropertyName("averageOrderValue")]
    public decimal AverageOrderValue { get; set; }

    [JsonPropertyName("percentOfTotal")]
    public decimal PercentOfTotal { get; set; }

    [JsonIgnore]
    public string RevenueDisplay => $"€{Revenue:F2}";

    [JsonIgnore]
    public string OrdersDisplay => $"{Orders} {LocalizationService.Instance.Get("Orders")}";

    [JsonIgnore]
    public string PercentDisplay => $"{PercentOfTotal:F1}%";

    [JsonIgnore]
    public double PercentProgress => (double)(PercentOfTotal / 100m);
}

// ── Top Selling ────────────────────────────────────────────────────────────────

public class TopSellingReport
{
    [JsonPropertyName("startDate")]
    public DateTime StartDate { get; set; }

    [JsonPropertyName("endDate")]
    public DateTime EndDate { get; set; }

    [JsonPropertyName("branchId")]
    public Guid? BranchId { get; set; }

    [JsonPropertyName("branchName")]
    public string? BranchName { get; set; }

    [JsonPropertyName("totalItemsSold")]
    public int TotalItemsSold { get; set; }

    [JsonPropertyName("totalItemsRevenue")]
    public decimal TotalItemsRevenue { get; set; }

    [JsonPropertyName("items")]
    public List<TopSellingItem> Items { get; set; } = new();
}

public class TopSellingItem
{
    [JsonPropertyName("rank")]
    public int Rank { get; set; }

    [JsonPropertyName("menuItemId")]
    public Guid MenuItemId { get; set; }

    [JsonPropertyName("menuItemName")]
    public string MenuItemName { get; set; } = string.Empty;

    [JsonPropertyName("categoryName")]
    public string CategoryName { get; set; } = string.Empty;

    [JsonPropertyName("quantitySold")]
    public int QuantitySold { get; set; }

    [JsonPropertyName("revenue")]
    public decimal Revenue { get; set; }

    [JsonPropertyName("averagePrice")]
    public decimal AveragePrice { get; set; }

    [JsonPropertyName("percentOfTotal")]
    public decimal PercentOfTotal { get; set; }

    [JsonIgnore]
    public string RevenueDisplay => $"€{Revenue:F2}";

    [JsonIgnore]
    public string PercentDisplay => $"{PercentOfTotal:F1}%";

    [JsonIgnore]
    public string QuantityDisplay => $"x{QuantitySold}";

    [JsonIgnore]
    public double PercentProgress => (double)(PercentOfTotal / 100m);

    [JsonIgnore]
    public string RankDisplay => $"#{Rank}";
}

// ── Sales Trends ───────────────────────────────────────────────────────────────

public class SalesTrendsReport
{
    [JsonPropertyName("startDate")]
    public DateTime StartDate { get; set; }

    [JsonPropertyName("endDate")]
    public DateTime EndDate { get; set; }

    [JsonPropertyName("branchId")]
    public Guid? BranchId { get; set; }

    [JsonPropertyName("branchName")]
    public string? BranchName { get; set; }

    [JsonPropertyName("period")]
    public string Period { get; set; } = string.Empty;

    [JsonPropertyName("totalRevenue")]
    public decimal TotalRevenue { get; set; }

    [JsonPropertyName("totalOrders")]
    public int TotalOrders { get; set; }

    [JsonPropertyName("averagePerPeriod")]
    public decimal AveragePerPeriod { get; set; }

    [JsonPropertyName("trendPercent")]
    public decimal TrendPercent { get; set; }

    [JsonPropertyName("dataPoints")]
    public List<SalesTrendDataPoint> DataPoints { get; set; } = new();

    [JsonIgnore]
    public string TrendPercentDisplay => TrendPercent >= 0
        ? $"+{TrendPercent:F1}%"
        : $"{TrendPercent:F1}%";

    [JsonIgnore]
    public Color TrendPercentColor => TrendPercent >= 0
        ? Color.FromRgb(39, 174, 96)
        : Color.FromRgb(231, 76, 60);

    [JsonIgnore]
    public string AveragePerPeriodDisplay => $"€{AveragePerPeriod:F2}";
}

public class SalesTrendDataPoint
{
    [JsonPropertyName("date")]
    public DateTime Date { get; set; }

    [JsonPropertyName("label")]
    public string Label { get; set; } = string.Empty;

    [JsonPropertyName("revenue")]
    public decimal Revenue { get; set; }

    [JsonPropertyName("orders")]
    public int Orders { get; set; }

    [JsonPropertyName("averageOrderValue")]
    public decimal AverageOrderValue { get; set; }

    [JsonIgnore]
    public string RevenueDisplay => $"€{Revenue:F2}";

    [JsonIgnore]
    public string OrdersDisplay => $"{Orders} {LocalizationService.Instance.Get("Orders")}";

    [JsonIgnore]
    public string AverageOrderValueDisplay => $"€{AverageOrderValue:F2} avg";
}
