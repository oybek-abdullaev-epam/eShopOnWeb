using MediatR;
using Microsoft.eShopWeb.ApplicationCore.Entities.OrderAggregate.Events;
using Microsoft.eShopWeb.Web.Interfaces;
using Microsoft.eShopWeb.Web.Services;
using Microsoft.Extensions.Logging;

namespace Microsoft.eShopWeb.Web.EventHandlers;

public class DeliveryOrderNotificationHandler(
    IDeliveryOrderClient deliveryClient,
    ILogger<DeliveryOrderNotificationHandler> logger) : INotificationHandler<OrderCreatedEvent>
{
    public async Task Handle(OrderCreatedEvent notification, CancellationToken cancellationToken)
    {
        var order = notification.Order;

        logger.LogInformation("DeliveryOrderNotificationHandler triggered for Order #{OrderId}.", order.Id);

        var address = new DeliveryAddressDto(
            order.ShipToAddress.Street, order.ShipToAddress.City,
            order.ShipToAddress.State, order.ShipToAddress.Country, order.ShipToAddress.ZipCode);

        var items = order.OrderItems
            .Select(i => new DeliveryItemDto(
                i.ItemOrdered.CatalogItemId, i.ItemOrdered.ProductName,
                i.ItemOrdered.PictureUri, i.UnitPrice, i.Units))
            .ToList();

        try
        {
            await deliveryClient.ProcessDeliveryAsync(order.Id, address, items, order.Total(), cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Failed to process delivery for Order #{OrderId}. The order was still created.", order.Id);
        }
    }
}
