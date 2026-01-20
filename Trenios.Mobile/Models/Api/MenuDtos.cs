using System.Text.Json.Serialization;

namespace Trenios.Mobile.Models.Api;

public class CategoryDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("restaurantId")]
    public Guid RestaurantId { get; set; }

    [JsonPropertyName("restaurantName")]
    public string? RestaurantName { get; set; }

    [JsonPropertyName("displayOrder")]
    public int DisplayOrder { get; set; }

    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; }

    [JsonPropertyName("iconUrl")]
    public string? IconUrl { get; set; }

    [JsonPropertyName("color")]
    public string? Color { get; set; }

    [JsonPropertyName("createdAt")]
    public DateTime? CreatedAt { get; set; }

    [JsonPropertyName("updatedAt")]
    public DateTime? UpdatedAt { get; set; }
}

public class BranchMenuItemDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("branchId")]
    public Guid BranchId { get; set; }

    [JsonPropertyName("branchName")]
    public string BranchName { get; set; } = string.Empty;

    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; }

    [JsonPropertyName("menuItemId")]
    public Guid MenuItemId { get; set; }

    [JsonPropertyName("menuItemName")]
    public string MenuItemName { get; set; } = string.Empty;

    [JsonPropertyName("menuItemDescription")]
    public string? MenuItemDescription { get; set; }

    [JsonPropertyName("menuItemType")]
    public int MenuItemType { get; set; }

    [JsonPropertyName("categoryId")]
    public Guid CategoryId { get; set; }

    [JsonPropertyName("categoryName")]
    public string CategoryName { get; set; } = string.Empty;

    [JsonPropertyName("menuItemIsAvailable")]
    public bool MenuItemIsAvailable { get; set; }

    [JsonPropertyName("preparationTimeMinutes")]
    public int? PreparationTimeMinutes { get; set; }

    [JsonPropertyName("price")]
    public decimal Price { get; set; }

    [JsonPropertyName("currency")]
    public int Currency { get; set; }

    [JsonPropertyName("isBranchSpecificPrice")]
    public bool IsBranchSpecificPrice { get; set; }

    [JsonPropertyName("images")]
    public List<MenuItemImageDto>? Images { get; set; }

    // Direct image URL field (in case API sends it this way)
    [JsonPropertyName("imageUrl")]
    public string? ImageUrl { get; set; }

    [JsonPropertyName("additionGroups")]
    public List<AdditionGroupDto>? AdditionGroups { get; set; }

    [JsonIgnore]
    public string? PrimaryImageUrl
    {
        get
        {
            // First try direct imageUrl field
            if (!string.IsNullOrEmpty(ImageUrl))
                return ImageUrl;

            // Then try images array (find primary first, then any)
            var primaryImage = Images?.FirstOrDefault(i => i.IsPrimary);
            if (primaryImage != null && !string.IsNullOrEmpty(primaryImage.ImageUrl))
                return primaryImage.ImageUrl;

            var firstImage = Images?.FirstOrDefault();
            return firstImage?.ImageUrl;
        }
    }

    [JsonIgnore]
    public bool HasImage => !string.IsNullOrEmpty(PrimaryImageUrl);
}

public class MenuItemImageDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("imageUrl")]
    public string ImageUrl { get; set; } = string.Empty;

    [JsonPropertyName("altText")]
    public string? AltText { get; set; }

    [JsonPropertyName("displayOrder")]
    public int DisplayOrder { get; set; }

    [JsonPropertyName("isPrimary")]
    public bool IsPrimary { get; set; }
}

public class AdditionGroupDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("selectionType")]
    public int SelectionType { get; set; }

    [JsonPropertyName("isRequired")]
    public bool IsRequired { get; set; }

    [JsonPropertyName("minSelections")]
    public int MinSelections { get; set; }

    [JsonPropertyName("maxSelections")]
    public int MaxSelections { get; set; }

    [JsonPropertyName("additions")]
    public List<AdditionDto>? Additions { get; set; }

    [JsonIgnore]
    public bool IsSingleSelect => MaxSelections == 1 || SelectionType == (int)Api.SelectionType.Single || SelectionType == 0;

    [JsonIgnore]
    public bool IsMultiSelect => !IsSingleSelect;
}

public class AdditionDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("displayOrder")]
    public int DisplayOrder { get; set; }

    [JsonPropertyName("isAvailable")]
    public bool IsAvailable { get; set; }

    [JsonPropertyName("additionalCost")]
    public decimal AdditionalCost { get; set; }

    [JsonPropertyName("currency")]
    public int Currency { get; set; }

    // Alias for compatibility
    [JsonIgnore]
    public decimal CurrentPrice => AdditionalCost;
}
