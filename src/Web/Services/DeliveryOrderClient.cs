using System.Net.Http.Json;
using Microsoft.eShopWeb.Web.Interfaces;
using Microsoft.Extensions.Logging;

namespace Microsoft.eShopWeb.Web.Services;

public class DeliveryOrderClient(
    HttpClient httpClient,
    ILogger<DeliveryOrderClient> logger) : IDeliveryOrderClient
{
    public async Task ProcessDeliveryAsync(int orderId, DeliveryAddressDto address,
        IReadOnlyList<DeliveryItemDto> items, decimal finalPrice, CancellationToken cancellationToken = default)
    {
        var payload = new { OrderId = orderId, ShippingAddress = address, Items = items, FinalPrice = finalPrice };

        logger.LogInformation("Sending delivery request for Order #{OrderId} to {BaseAddress}.",
            orderId, httpClient.BaseAddress);

        var response = await httpClient.PostAsJsonAsync("api/delivery", payload, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogWarning("DeliveryOrderProcessor returned {StatusCode}: {Body}",
                (int)response.StatusCode, body);
        }
        else
        {
            logger.LogInformation("Delivery record created for Order #{OrderId}.", orderId);
        }
    }
}
