using MediatR;
using Microsoft.eShopWeb.ApplicationCore.Entities.OrderAggregate.Events;
using Microsoft.eShopWeb.Web.Interfaces;
using Microsoft.Extensions.Logging;

namespace Microsoft.eShopWeb.Web.EventHandlers;

public class OrderItemsReserverNotificationHandler(
    IOrderItemsReserverClient reserverClient,
    ILogger<OrderItemsReserverNotificationHandler> logger)
    : INotificationHandler<OrderCreatedEvent>
{
    public async Task Handle(OrderCreatedEvent notification, CancellationToken cancellationToken)
    {
        var order = notification.Order;

        logger.LogInformation(
            "OrderItemsReserverNotificationHandler triggered for Order #{OrderId}.", order.Id);

        var items = order.OrderItems
            .Select(i => (ItemId: i.ItemOrdered.CatalogItemId, Quantity: i.Units))
            .ToList();

        try
        {
            await reserverClient.ReserveAsync(order.Id, items, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Failed to reserve items for Order #{OrderId}. The order was still created.", order.Id);
        }
    }
}
