using System.Net;
using System.Text.Json;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.eShopWeb.OrderItemsReserver.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Microsoft.eShopWeb.OrderItemsReserver.Functions;

public class DeliveryOrderProcessorFunction(
    IConfiguration configuration,
    ILogger<DeliveryOrderProcessorFunction> logger)
{
    private static readonly JsonSerializerOptions DeserializeOptions = new() { PropertyNameCaseInsensitive = true };

    [Function("ProcessDeliveryOrder")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "delivery")] HttpRequestData req,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("DeliveryOrderProcessor triggered.");

        DeliveryOrderRequest? payload;
        try
        {
            payload = await JsonSerializer.DeserializeAsync<DeliveryOrderRequest>(
                req.Body, DeserializeOptions, cancellationToken);
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Failed to deserialize request body.");
            var bad = req.CreateResponse(HttpStatusCode.BadRequest);
            await bad.WriteStringAsync("Invalid JSON payload.");
            return bad;
        }

        if (payload is null)
        {
            var bad = req.CreateResponse(HttpStatusCode.BadRequest);
            await bad.WriteStringAsync("Payload is missing.");
            return bad;
        }

        var connectionString = configuration["CosmosDbConnectionString"]
            ?? throw new InvalidOperationException("CosmosDbConnectionString is not configured.");
        var databaseName = configuration["CosmosDbDatabaseName"] ?? "deliveries";
        var containerName = configuration["CosmosDbContainerName"] ?? "delivery-orders";

        try
        {
            var cosmosClient = new CosmosClient(connectionString);
            var container = cosmosClient.GetContainer(databaseName, containerName);

            var documentId = $"delivery-{payload.OrderId}-{Guid.NewGuid():N}";
            var document = new DeliveryOrderDocument(
                Id: documentId,
                OrderId: payload.OrderId,
                ShippingAddress: payload.ShippingAddress,
                Items: payload.Items,
                FinalPrice: payload.FinalPrice,
                CreatedAt: DateTimeOffset.UtcNow);

            await container.CreateItemAsync(document, new PartitionKey(payload.OrderId),
                cancellationToken: cancellationToken);

            logger.LogInformation("Created CosmosDB document {DocumentId} for Order #{OrderId}.",
                documentId, payload.OrderId);

            var ok = req.CreateResponse(HttpStatusCode.OK);
            await ok.WriteStringAsync($"Delivery record created. Document: {documentId}");
            return ok;
        }
        catch (CosmosException ex)
        {
            logger.LogError(ex, "CosmosDB error for Order #{OrderId}. Status: {Status}",
                payload.OrderId, ex.StatusCode);
            var error = req.CreateResponse(HttpStatusCode.InternalServerError);
            await error.WriteStringAsync("Failed to write delivery record.");
            return error;
        }
    }
}
