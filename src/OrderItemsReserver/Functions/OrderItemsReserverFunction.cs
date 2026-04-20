using System.Net;
using System.Text;
using System.Text.Json;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.eShopWeb.OrderItemsReserver.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Microsoft.eShopWeb.OrderItemsReserver.Functions;

public class OrderItemsReserverFunction(
    IConfiguration configuration,
    ILogger<OrderItemsReserverFunction> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private static readonly JsonSerializerOptions DeserializeOptions = new() { PropertyNameCaseInsensitive = true };

    [Function("ReserveOrderItems")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "reserve")] HttpRequestData req,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("OrderItemsReserver triggered.");

        ReserveOrderItemsRequest? payload;
        try
        {
            payload = await JsonSerializer.DeserializeAsync<ReserveOrderItemsRequest>(
                req.Body,
                DeserializeOptions,
                cancellationToken);
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Failed to deserialize request body.");
            var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
            await badRequest.WriteStringAsync("Invalid JSON payload.");
            return badRequest;
        }

        if (payload is null || payload.Items is null || payload.Items.Count == 0)
        {
            var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
            await badRequest.WriteStringAsync("Payload missing or empty items.");
            return badRequest;
        }

        string connectionString;
        try
        {
            connectionString = configuration["OrderItemsStorage"]
                ?? throw new InvalidOperationException("OrderItemsStorage connection string not configured.");
        }
        catch (InvalidOperationException ex)
        {
            logger.LogError(ex, "Missing OrderItemsStorage configuration.");
            var error = req.CreateResponse(HttpStatusCode.InternalServerError);
            await error.WriteStringAsync("Storage not configured.");
            return error;
        }

        var containerName = configuration["BlobContainerName"] ?? "order-items";

        try
        {
            var blobServiceClient = new BlobServiceClient(connectionString);
            var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
            await containerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

            var blobName = $"order-{payload.OrderId}-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}.json";
            var blobClient = containerClient.GetBlobClient(blobName);

            var blobContent = JsonSerializer.Serialize(payload, JsonOptions);
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(blobContent));
            await blobClient.UploadAsync(stream, overwrite: true, cancellationToken);

            logger.LogInformation("Uploaded blob {BlobName} for order {OrderId}.", blobName, payload.OrderId);

            var ok = req.CreateResponse(HttpStatusCode.OK);
            await ok.WriteStringAsync($"Reserved. Blob: {blobName}");
            return ok;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to upload reservation blob for order {OrderId}.", payload.OrderId);
            var error = req.CreateResponse(HttpStatusCode.InternalServerError);
            await error.WriteStringAsync("Failed to store reservation.");
            return error;
        }
    }
}
