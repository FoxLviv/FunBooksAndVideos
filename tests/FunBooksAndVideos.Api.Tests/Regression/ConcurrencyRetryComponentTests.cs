using FluentAssertions;
using FunBooksAndVideos.Application.Abstractions;
using FunBooksAndVideos.Application.Customers.CreateCustomer;
using FunBooksAndVideos.Application.DependencyInjection;
using FunBooksAndVideos.Application.Dtos;
using FunBooksAndVideos.Application.Processing.Rules;
using FunBooksAndVideos.Application.PurchaseOrders.SubmitPurchaseOrder;
using FunBooksAndVideos.Domain.Customers;
using FunBooksAndVideos.Domain.Orders;
using FunBooksAndVideos.Domain.Persistence;
using FunBooksAndVideos.Infrastructure.DependencyInjection;
using FunBooksAndVideos.Infrastructure.Seeding;
using FunBooksAndVideos.TestKit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace FunBooksAndVideos.Api.Tests.Regression;

/// <summary>
/// Deterministic counterpart of <see cref="ConcurrencyRegressionTests"/>. The parallel HTTP test cannot prove that an
/// optimistic concurrency conflict actually occurred; here a wrapped customer repository performs exactly one competing
/// write right after the handler loaded the customer, so the first commit must fail and the retry decorator must re-run
/// the command. The tests verify the retry outcome and that the failed attempt left no orphan order or shipping slip.
/// </summary>
[Trait("Category", "Regression")]
public sealed class ConcurrencyRetryComponentTests
{
    [Fact]
    public async Task A_concurrent_membership_activation_is_detected_and_resolved_on_retry()
    {
        await using var provider = BuildProvider(out var injector);
        var customerId = await CreateCustomerAsync(provider);
        injector.TargetCustomerId = customerId;

        var result = await SubmitAsync(provider, customerId, new MembershipLineRequest(MembershipType.BookClub));

        injector.FindCount.Should().Be(2, "the first attempt loads the customer once and the single retry loads it again");
        injector.Injected.Should().BeTrue();
        result.MembershipActivations.Should().ContainSingle()
            .Which.Outcome.Should().Be(MembershipActivationOutcome.AlreadyActive, "the retry sees the competing activation");
        result.AppliedRules.Should().Equal(MembershipActivationRule.RuleName);

        await using var scope = provider.CreateAsyncScope();
        var customer = await scope.ServiceProvider.GetRequiredService<ICustomerRepository>()
            .FindAsync(customerId, CancellationToken.None);
        customer.Should().NotBeNull();
        customer!.Memberships.Should().ContainSingle().Which.Type.Should().Be(MembershipType.BookClub);
        customer.ActiveClubs.Should().Be(Club.Book);

        var orders = scope.ServiceProvider.GetRequiredService<IPurchaseOrderRepository>();
        var order = await orders.FindAsync(new PurchaseOrderId(result.PurchaseOrder.Id), CancellationToken.None);
        order.Should().NotBeNull();
        order!.Status.Should().Be(PurchaseOrderStatus.Processed);

        var orphan = await orders.FindAsync(new PurchaseOrderId(result.PurchaseOrder.Id - 1), CancellationToken.None);
        orphan.Should().BeNull("the order id consumed by the failed first attempt must not have been persisted");
    }

    [Fact]
    public async Task A_retried_order_with_a_physical_product_leaves_no_orphan_shipping_slip()
    {
        await using var provider = BuildProvider(out var injector);
        var customerId = await CreateCustomerAsync(provider);
        injector.TargetCustomerId = customerId;

        var result = await SubmitAsync(
            provider,
            customerId,
            new ProductLineRequest(DemoData.GirlOnTheTrainBookId),
            new MembershipLineRequest(MembershipType.VideoClub));

        injector.FindCount.Should().Be(2);
        injector.Injected.Should().BeTrue();
        result.MembershipActivations.Should().ContainSingle()
            .Which.Should().Match<MembershipActivationDto>(activation =>
                activation.MembershipType == MembershipType.VideoClub
                && activation.Outcome == MembershipActivationOutcome.Activated);

        await using var scope = provider.CreateAsyncScope();
        var slips = scope.ServiceProvider.GetRequiredService<IShippingSlipRepository>();
        var slip = await slips.FindByPurchaseOrderAsync(new PurchaseOrderId(result.PurchaseOrder.Id), CancellationToken.None);
        slip.Should().NotBeNull();
        slip!.Items.Should().ContainSingle();

        var orphanSlip = await slips.FindByPurchaseOrderAsync(new PurchaseOrderId(result.PurchaseOrder.Id - 1), CancellationToken.None);
        orphanSlip.Should().BeNull("the failed first attempt must not leave a shipping slip behind");

        var customer = await scope.ServiceProvider.GetRequiredService<ICustomerRepository>()
            .FindAsync(customerId, CancellationToken.None);
        customer.Should().NotBeNull();
        customer!.Memberships.Select(membership => membership.Type)
            .Should().BeEquivalentTo([MembershipType.BookClub, MembershipType.VideoClub]);
        customer.ActiveClubs.Should().Be(Club.Book | Club.Video);
    }

    [Fact]
    public async Task Without_a_competing_write_there_is_no_retry()
    {
        await using var provider = BuildProvider(out var injector);
        var customerId = await CreateCustomerAsync(provider);
        injector.TargetCustomerId = customerId;
        injector.Enabled = false;

        var result = await SubmitAsync(provider, customerId, new MembershipLineRequest(MembershipType.BookClub));

        injector.FindCount.Should().Be(1);
        injector.Injected.Should().BeFalse();
        result.MembershipActivations.Should().ContainSingle()
            .Which.Outcome.Should().Be(MembershipActivationOutcome.Activated);
    }

    private static ServiceProvider BuildProvider(out ConflictInjector injector)
    {
        var services = new ServiceCollection();
        services.AddSingleton<TimeProvider>(TestClock.Create());
        services.AddApplication();
        services.AddInfrastructure();
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));

        var conflictInjector = new ConflictInjector();
        services.AddSingleton(conflictInjector);

        var descriptor = services.Single(service => service.ServiceType == typeof(ICustomerRepository));
        var implementationType = descriptor.ImplementationType
            ?? throw new InvalidOperationException("ICustomerRepository is expected to be registered by implementation type.");
        services.Remove(descriptor);
        services.AddScoped<ICustomerRepository>(sp => new ConflictInjectingCustomerRepository(
            (ICustomerRepository)ActivatorUtilities.CreateInstance(sp, implementationType),
            sp.GetRequiredService<ConflictInjector>()));

        var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = false });
        conflictInjector.RootProvider = provider;
        provider.GetRequiredService<IDemoDataSeeder>().Seed();

        injector = conflictInjector;
        return provider;
    }

    private static async Task<CustomerId> CreateCustomerAsync(IServiceProvider provider)
    {
        await using var scope = provider.CreateAsyncScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<CreateCustomerCommand, CustomerDto>>();
        var customer = await handler.HandleAsync(
            new CreateCustomerCommand(
                "Retry Tester",
                new ShippingAddressDto("221B Baker Street", null, "London", "NW1 6XE", "United Kingdom")),
            CancellationToken.None);
        return new CustomerId(customer.Id);
    }

    private static async Task<SubmitPurchaseOrderResult> SubmitAsync(
        IServiceProvider provider,
        CustomerId customerId,
        params OrderLineRequest[] lines)
    {
        await using var scope = provider.CreateAsyncScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<SubmitPurchaseOrderCommand, SubmitPurchaseOrderResult>>();
        return await handler.HandleAsync(new SubmitPurchaseOrderCommand(customerId.Value, lines, null), CancellationToken.None);
    }

    /// <summary>Test-wide switchboard deciding when (and for whom) the competing write happens.</summary>
    private sealed class ConflictInjector
    {
        private int _findCount;

        public IServiceProvider? RootProvider { get; set; }

        public CustomerId? TargetCustomerId { get; set; }

        public bool Enabled { get; set; } = true;

        public bool Injected { get; private set; }

        /// <summary>True while the competing write runs, so its own load is neither counted nor re-injected.</summary>
        public bool Injecting { get; private set; }

        /// <summary>Number of loads of the target customer outside the competing write.</summary>
        public int FindCount => _findCount;

        public async Task OnFoundAsync(CustomerId id, CancellationToken cancellationToken)
        {
            if (Injecting || id != TargetCustomerId)
            {
                return;
            }

            Interlocked.Increment(ref _findCount);

            if (!Enabled || Injected)
            {
                return;
            }

            Injected = true;
            Injecting = true;
            try
            {
                var root = RootProvider ?? throw new InvalidOperationException("The root provider has not been set.");
                await using var scope = root.CreateAsyncScope();
                var customers = scope.ServiceProvider.GetRequiredService<ICustomerRepository>();
                var customer = await customers.FindAsync(id, cancellationToken)
                    ?? throw new InvalidOperationException($"Customer {id} does not exist.");
                customer.ActivateMembership(MembershipType.BookClub, scope.ServiceProvider.GetRequiredService<TimeProvider>().GetUtcNow());
                customers.Update(customer);
                await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().CommitAsync(cancellationToken);
            }
            finally
            {
                Injecting = false;
            }
        }
    }

    /// <summary>
    /// Decorates the real repository: the first load of the target customer is followed by a competing commit,
    /// so the caller holds a stale copy and its own commit hits an optimistic concurrency conflict.
    /// </summary>
    private sealed class ConflictInjectingCustomerRepository : ICustomerRepository
    {
        private readonly ICustomerRepository _inner;
        private readonly ConflictInjector _injector;

        public ConflictInjectingCustomerRepository(ICustomerRepository inner, ConflictInjector injector)
        {
            _inner = inner;
            _injector = injector;
        }

        public async Task<Customer?> FindAsync(CustomerId id, CancellationToken cancellationToken)
        {
            var customer = await _inner.FindAsync(id, cancellationToken);
            await _injector.OnFoundAsync(id, cancellationToken);
            return customer;
        }

        public Task<CustomerId> NextIdentityAsync(CancellationToken cancellationToken) => _inner.NextIdentityAsync(cancellationToken);

        public void Add(Customer customer) => _inner.Add(customer);

        public void Update(Customer customer) => _inner.Update(customer);
    }
}
