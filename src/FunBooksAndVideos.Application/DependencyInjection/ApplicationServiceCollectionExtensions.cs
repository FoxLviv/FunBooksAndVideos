using FunBooksAndVideos.Application.Abstractions;
using FunBooksAndVideos.Application.Catalog.CreateProduct;
using FunBooksAndVideos.Application.Catalog.GetProduct;
using FunBooksAndVideos.Application.Catalog.ListMembershipPlans;
using FunBooksAndVideos.Application.Catalog.ListProducts;
using FunBooksAndVideos.Application.Customers.CreateCustomer;
using FunBooksAndVideos.Application.Customers.GetCustomer;
using FunBooksAndVideos.Application.Decorators;
using FunBooksAndVideos.Application.Dtos;
using FunBooksAndVideos.Application.Idempotency;
using FunBooksAndVideos.Application.Processing;
using FunBooksAndVideos.Application.Processing.Rules;
using FunBooksAndVideos.Application.PurchaseOrders.GetPurchaseOrder;
using FunBooksAndVideos.Application.PurchaseOrders.GetShippingSlip;
using FunBooksAndVideos.Application.PurchaseOrders.SubmitPurchaseOrder;
using FunBooksAndVideos.Domain.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace FunBooksAndVideos.Application.DependencyInjection;

public static class ApplicationServiceCollectionExtensions
{
    /// <summary>Registers use cases, the purchase order processor and its business rules.</summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton(TimeProvider.System);

        // Purchase order processing pipeline. Rules execute in registration order.
        services.AddScoped<IPurchaseOrderRule, MembershipActivationRule>();
        services.AddScoped<IPurchaseOrderRule, ShippingSlipGenerationRule>();
        services.AddScoped<IPurchaseOrderProcessor, PurchaseOrderProcessor>();
        services.AddScoped<IOrderLineFactory, OrderLineFactory>();

        // Commands. Submit = Idempotent( ConcurrencyRetry( SubmitPurchaseOrderHandler ) ).
        services.AddScoped<SubmitPurchaseOrderHandler>();
        services.AddScoped<ICommandHandler<SubmitPurchaseOrderCommand, SubmitPurchaseOrderResult>>(provider =>
            new IdempotentSubmitPurchaseOrderDecorator(
                new ConcurrencyRetryDecorator<SubmitPurchaseOrderCommand, SubmitPurchaseOrderResult>(
                    provider.GetRequiredService<SubmitPurchaseOrderHandler>(),
                    provider.GetRequiredService<ILogger<ConcurrencyRetryDecorator<SubmitPurchaseOrderCommand, SubmitPurchaseOrderResult>>>()),
                provider.GetRequiredService<IIdempotencyRecordRepository>(),
                provider.GetRequiredService<ILogger<IdempotentSubmitPurchaseOrderDecorator>>()));
        services.AddScoped<ICommandHandler<CreateCustomerCommand, CustomerDto>, CreateCustomerHandler>();
        services.AddScoped<ICommandHandler<CreateProductCommand, ProductDto>, CreateProductHandler>();

        // Queries.
        services.AddScoped<IQueryHandler<GetPurchaseOrderQuery, PurchaseOrderDto>, GetPurchaseOrderHandler>();
        services.AddScoped<IQueryHandler<GetShippingSlipQuery, ShippingSlipDto>, GetShippingSlipHandler>();
        services.AddScoped<IQueryHandler<GetCustomerQuery, CustomerDto>, GetCustomerHandler>();
        services.AddScoped<IQueryHandler<GetProductQuery, ProductDto>, GetProductHandler>();
        services.AddScoped<IQueryHandler<ListProductsQuery, IReadOnlyList<ProductDto>>, ListProductsHandler>();
        services.AddScoped<IQueryHandler<ListMembershipPlansQuery, IReadOnlyList<MembershipPlanDto>>, ListMembershipPlansHandler>();

        return services;
    }
}
