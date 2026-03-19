using Trenios.Mobile.Models.Api;

namespace Trenios.Mobile.Helpers;

public class OrderDataTemplateSelector : DataTemplateSelector
{
    public DataTemplate SwipeableTemplate { get; set; } = null!;
    public DataTemplate ReadOnlyTemplate { get; set; } = null!;

    protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
        => item is OrderResponse order && order.CanSwipe
            ? SwipeableTemplate
            : ReadOnlyTemplate;
}
