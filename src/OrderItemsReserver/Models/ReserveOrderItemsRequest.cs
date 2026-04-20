namespace Microsoft.eShopWeb.OrderItemsReserver.Models;

public record ReserveOrderItemsRequest(
    int OrderId,
    IReadOnlyList<OrderItemDto> Items
);

public record OrderItemDto(
    int ItemId,
    int Quantity
);
