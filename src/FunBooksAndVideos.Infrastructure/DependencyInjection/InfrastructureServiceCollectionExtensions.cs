using FunBooksAndVideos.Domain.Persistence;
using FunBooksAndVideos.Infrastructure.Persistence;
using FunBooksAndVideos.Infrastructure.Persistence.Repositories;
using FunBooksAndVideos.Infrastructure.Seeding;
using Microsoft.Extensions.DependencyInjection;

namespace FunBooksAndVideos.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    /// <summary>Registers the in-memory persistence: one store per process, one unit of work per scope.</summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<InMemoryDataStore>();
        services.AddSingleton<IDemoDataSeeder, DemoDataSeeder>();

        services.AddScoped<InMemoryUnitOfWork>();
        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<InMemoryUnitOfWork>());

        services.AddScoped<ICustomerRepository, InMemoryCustomerRepository>();
        services.AddScoped<IProductRepository, InMemoryProductRepository>();
        services.AddScoped<IMembershipPlanRepository, InMemoryMembershipPlanRepository>();
        services.AddScoped<IPurchaseOrderRepository, InMemoryPurchaseOrderRepository>();
        services.AddScoped<IShippingSlipRepository, InMemoryShippingSlipRepository>();
        services.AddScoped<IIdempotencyRecordRepository, InMemoryIdempotencyRecordRepository>();

        return services;
    }
}
