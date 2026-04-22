namespace Microsoft.eShopWeb.OrderItemsReserver.Models;

public record DeliveryOrderRequest(
    int OrderId,
    ShippingAddressDto ShippingAddress,
    IReadOnlyList<DeliveryItemDto> Items,
    decimal FinalPrice
);

public record ShippingAddressDto(string Street, string City, string State, string Country, string ZipCode);

public record DeliveryItemDto(int CatalogItemId, string ProductName, string PictureUri, decimal UnitPrice, int Units);
