using Microsoft.eShopWeb.Web.Services;

namespace Microsoft.eShopWeb.Web.Interfaces;

public interface IDeliveryOrderClient
{
    Task ProcessDeliveryAsync(int orderId, DeliveryAddressDto address,
        IReadOnlyList<DeliveryItemDto> items, decimal finalPrice,
        CancellationToken cancellationToken = default);
}
