namespace Microsoft.eShopWeb.Web.Interfaces;

public interface IOrderItemsReserverClient
{
    Task ReserveAsync(int orderId, IReadOnlyList<(int ItemId, int Quantity)> items,
        CancellationToken cancellationToken = default);
}
