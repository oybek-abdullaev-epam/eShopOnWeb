namespace Microsoft.eShopWeb.Web.Services;

public record DeliveryAddressDto(string Street, string City, string State, string Country, string ZipCode);

public record DeliveryItemDto(int CatalogItemId, string ProductName, string PictureUri, decimal UnitPrice, int Units);
