using FluentAssertions;
using FunBooksAndVideos.Domain.Persistence;
using FunBooksAndVideos.Infrastructure.DependencyInjection;
using FunBooksAndVideos.Infrastructure.Persistence;
using FunBooksAndVideos.Infrastructure.Seeding;
using Microsoft.Extensions.DependencyInjection;

namespace FunBooksAndVideos.Infrastructure.Tests.DependencyInjection;

public sealed class InfrastructureServiceCollectionExtensionsTests
{
    private static ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        services.AddInfrastructure();
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
    }

    [Fact]
    public void The_store_is_a_singleton_shared_by_all_scopes()
    {
        using var provider = BuildProvider();
        using var first = provider.CreateScope();
        using var second = provider.CreateScope();

        first.ServiceProvider.GetRequiredService<InMemoryDataStore>()
            .Should().BeSameAs(second.ServiceProvider.GetRequiredService<InMemoryDataStore>());
    }

    [Fact]
    public void The_unit_of_work_is_one_instance_per_scope_under_both_registrations()
    {
        using var provider = BuildProvider();
        using var first = provider.CreateScope();
        using var second = provider.CreateScope();

        var unitOfWork = first.ServiceProvider.GetRequiredService<IUnitOfWork>();

        unitOfWork.Should().BeSameAs(first.ServiceProvider.GetRequiredService<InMemoryUnitOfWork>());
        unitOfWork.Should().NotBeSameAs(second.ServiceProvider.GetRequiredService<IUnitOfWork>());
    }

    [Fact]
    public void Every_repository_and_the_seeder_can_be_resolved()
    {
        using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var services = scope.ServiceProvider;

        services.GetRequiredService<ICustomerRepository>().Should().NotBeNull();
        services.GetRequiredService<IProductRepository>().Should().NotBeNull();
        services.GetRequiredService<IMembershipPlanRepository>().Should().NotBeNull();
        services.GetRequiredService<IPurchaseOrderRepository>().Should().NotBeNull();
        services.GetRequiredService<IShippingSlipRepository>().Should().NotBeNull();
        services.GetRequiredService<IIdempotencyRecordRepository>().Should().NotBeNull();
        services.GetRequiredService<IDemoDataSeeder>().Should().BeOfType<DemoDataSeeder>();
    }

    [Fact]
    public void Rejects_a_null_service_collection()
    {
        var act = () => InfrastructureServiceCollectionExtensions.AddInfrastructure(null!);

        act.Should().Throw<ArgumentNullException>();
    }
}
