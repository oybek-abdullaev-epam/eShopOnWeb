using Microsoft.eShopWeb.ApplicationCore.Services;
using Microsoft.eShopWeb.Web.EventHandlers;
using Microsoft.eShopWeb.Web.Interfaces;
using Microsoft.eShopWeb.Web.Services;

namespace Microsoft.eShopWeb.Web.Configuration;

public static class ConfigureWebServices
{
    public static IServiceCollection AddWebServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Add MediatR support for the services
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(BasketViewModelService).Assembly);
            cfg.RegisterServicesFromAssembly(typeof(OrderService).Assembly);
        }
        );
        services.AddScoped<IBasketViewModelService, BasketViewModelService>();
        services.AddScoped<CatalogViewModelService>();
        services.AddScoped<ICatalogItemViewModelService, CatalogItemViewModelService>();
        services.Configure<CatalogSettings>(configuration);
        services.AddScoped<ICatalogViewModelService, CachedCatalogViewModelService>();

        services.AddHttpClient<IOrderItemsReserverClient, OrderItemsReserverClient>((sp, client) =>
        {
            var functionUrl = configuration["OrderItemsReserver:FunctionUrl"]
                ?? throw new InvalidOperationException("OrderItemsReserver:FunctionUrl is not configured.");
            client.BaseAddress = new Uri(functionUrl);
        });

        services.AddHttpClient<IDeliveryOrderClient, DeliveryOrderClient>((sp, client) =>
        {
            var functionUrl = configuration["DeliveryOrderProcessor:FunctionUrl"]
                ?? throw new InvalidOperationException("DeliveryOrderProcessor:FunctionUrl is not configured.");
            client.BaseAddress = new Uri(functionUrl);
        });

        return services;
    }
}
