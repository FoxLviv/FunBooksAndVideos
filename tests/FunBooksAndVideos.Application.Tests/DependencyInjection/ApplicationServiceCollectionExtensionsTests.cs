using FluentAssertions;
using FunBooksAndVideos.Application.Abstractions;
using FunBooksAndVideos.Application.Catalog.CreateProduct;
using FunBooksAndVideos.Application.Catalog.GetProduct;
using FunBooksAndVideos.Application.Catalog.ListMembershipPlans;
using FunBooksAndVideos.Application.Catalog.ListProducts;
using FunBooksAndVideos.Application.Customers.CreateCustomer;
using FunBooksAndVideos.Application.Customers.GetCustomer;
using FunBooksAndVideos.Application.DependencyInjection;
using FunBooksAndVideos.Application.Dtos;
using FunBooksAndVideos.Application.Idempotency;
using FunBooksAndVideos.Application.Processing;
using FunBooksAndVideos.Application.Processing.Rules;
using FunBooksAndVideos.Application.PurchaseOrders.GetPurchaseOrder;
using FunBooksAndVideos.Application.PurchaseOrders.GetShippingSlip;
using FunBooksAndVideos.Application.PurchaseOrders.SubmitPurchaseOrder;
using FunBooksAndVideos.Domain.Persistence;
using FunBooksAndVideos.TestKit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace FunBooksAndVideos.Application.Tests.DependencyInjection;

public sealed class ApplicationServiceCollectionExtensionsTests
{
    private static ServiceProvider BuildProvider(Action<IServiceCollection>? configure = null, bool withIdempotencyRecords = true)
    {
        var services = new ServiceCollection();
        if (withIdempotencyRecords)
        {
            services.AddSingleton(Substitute.For<IIdempotencyRecordRepository>());
        }

        services.AddSingleton(Substitute.For<ICustomerRepository>());
        services.AddSingleton(Substitute.For<IProductRepository>());
        services.AddSingleton(Substitute.For<IMembershipPlanRepository>());
        services.AddSingleton(Substitute.For<IPurchaseOrderRepository>());
        services.AddSingleton(Substitute.For<IShippingSlipRepository>());
        services.AddSingleton(Substitute.For<IUnitOfWork>());
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        configure?.Invoke(services);

        services.AddApplication();

        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
    }

    [Fact]
    public void Registers_the_submit_handler_behind_the_idempotency_and_concurrency_retry_decorators()
    {
        using var provider = BuildProvider();
        using var scope = provider.CreateScope();

        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<SubmitPurchaseOrderCommand, SubmitPurchaseOrderResult>>();

        handler.Should().BeOfType<IdempotentSubmitPurchaseOrderDecorator>();
        scope.ServiceProvider.GetRequiredService<SubmitPurchaseOrderHandler>().Should().NotBeNull();
    }

    [Fact]
    public void The_submit_pipeline_requires_an_idempotency_record_repository()
    {
        var act = () => BuildProvider(withIdempotencyRecords: false);

        act.Should().Throw<AggregateException>().WithMessage($"*{nameof(IIdempotencyRecordRepository)}*");
    }

    [Fact]
    public void Registers_the_business_rules_in_the_documented_order()
    {
        using var provider = BuildProvider();
        using var scope = provider.CreateScope();

        var processor = scope.ServiceProvider.GetRequiredService<IPurchaseOrderProcessor>();

        processor.Should().BeOfType<PurchaseOrderProcessor>()
            .Which.RuleNames.Should().Equal(MembershipActivationRule.RuleName, ShippingSlipGenerationRule.RuleName);
    }

    [Fact]
    public void Registers_every_use_case()
    {
        using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var services = scope.ServiceProvider;

        services.GetRequiredService<ICommandHandler<CreateCustomerCommand, CustomerDto>>().Should().BeOfType<CreateCustomerHandler>();
        services.GetRequiredService<ICommandHandler<CreateProductCommand, ProductDto>>().Should().BeOfType<CreateProductHandler>();
        services.GetRequiredService<IQueryHandler<GetPurchaseOrderQuery, PurchaseOrderDto>>().Should().BeOfType<GetPurchaseOrderHandler>();
        services.GetRequiredService<IQueryHandler<GetShippingSlipQuery, ShippingSlipDto>>().Should().BeOfType<GetShippingSlipHandler>();
        services.GetRequiredService<IQueryHandler<GetCustomerQuery, CustomerDto>>().Should().BeOfType<GetCustomerHandler>();
        services.GetRequiredService<IQueryHandler<GetProductQuery, ProductDto>>().Should().BeOfType<GetProductHandler>();
        services.GetRequiredService<IQueryHandler<ListProductsQuery, IReadOnlyList<ProductDto>>>().Should().BeOfType<ListProductsHandler>();
        services.GetRequiredService<IQueryHandler<ListMembershipPlansQuery, IReadOnlyList<MembershipPlanDto>>>().Should().BeOfType<ListMembershipPlansHandler>();
        services.GetRequiredService<IOrderLineFactory>().Should().BeOfType<OrderLineFactory>();
    }

    [Fact]
    public void Uses_the_system_clock_unless_one_was_registered_before()
    {
        using var defaultProvider = BuildProvider();
        var fake = TestClock.Create();
        using var customProvider = BuildProvider(services => services.AddSingleton<TimeProvider>(fake));

        defaultProvider.GetRequiredService<TimeProvider>().Should().BeSameAs(TimeProvider.System);
        customProvider.GetRequiredService<TimeProvider>().Should().BeSameAs(fake);
    }

    [Fact]
    public void Rejects_a_null_service_collection()
    {
        var act = () => ApplicationServiceCollectionExtensions.AddApplication(null!);

        act.Should().Throw<ArgumentNullException>();
    }
}
