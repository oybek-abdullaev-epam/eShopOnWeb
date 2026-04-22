using Newtonsoft.Json;

namespace Microsoft.eShopWeb.OrderItemsReserver.Models;

public record DeliveryOrderDocument(
    [property: JsonProperty("id")] string Id,
    int OrderId,
    ShippingAddressDto ShippingAddress,
    IReadOnlyList<DeliveryItemDto> Items,
    decimal FinalPrice,
    DateTimeOffset CreatedAt
);
