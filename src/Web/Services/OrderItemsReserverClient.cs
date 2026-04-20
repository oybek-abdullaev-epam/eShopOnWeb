using System.Net.Http.Json;
using Microsoft.eShopWeb.Web.Interfaces;
using Microsoft.Extensions.Logging;

namespace Microsoft.eShopWeb.Web.Services;

public class OrderItemsReserverClient(
    HttpClient httpClient,
    ILogger<OrderItemsReserverClient> logger) : IOrderItemsReserverClient
{
    public async Task ReserveAsync(int orderId, IReadOnlyList<(int ItemId, int Quantity)> items,
        CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            OrderId = orderId,
            Items = items.Select(i => new { ItemId = i.ItemId, Quantity = i.Quantity })
        };

        logger.LogInformation("Sending reservation request for order {OrderId} with {ItemCount} item(s) to {BaseAddress}.",
            orderId, items.Count, httpClient.BaseAddress);

        var response = await httpClient.PostAsJsonAsync("api/reserve", payload, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogWarning(
                "OrderItemsReserver returned {StatusCode}: {Body}",
                (int)response.StatusCode, body);
        }
        else
        {
            logger.LogInformation("Order {OrderId} items reserved successfully.", orderId);
        }
    }
}
