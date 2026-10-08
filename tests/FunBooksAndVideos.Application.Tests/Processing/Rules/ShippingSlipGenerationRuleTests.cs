using FluentAssertions;
using FunBooksAndVideos.Application.Processing;
using FunBooksAndVideos.Application.Processing.Rules;
using FunBooksAndVideos.Domain.Catalog;
using FunBooksAndVideos.Domain.Common;
using FunBooksAndVideos.Domain.Customers;
using FunBooksAndVideos.Domain.Persistence;
using FunBooksAndVideos.Domain.Shipping;
using FunBooksAndVideos.TestKit;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace FunBooksAndVideos.Application.Tests.Processing.Rules;

public sealed class ShippingSlipGenerationRuleTests
{
    private static readonly ShippingSlipId NextId = new(77);

    private readonly IShippingSlipRepository _shippingSlips = Substitute.For<IShippingSlipRepository>();
    private readonly ShippingSlipGenerationRule _rule;

    public ShippingSlipGenerationRuleTests()
    {
        _shippingSlips.NextIdentityAsync(Arg.Any<CancellationToken>()).Returns(NextId);
        _rule = new ShippingSlipGenerationRule(_shippingSlips, NullLogger<ShippingSlipGenerationRule>.Instance);
    }

    [Fact]
    public void Is_named_after_business_rule_two()
    {
        _rule.Name.Should().Be("BR2.ShippingSlipGeneration");
        ShippingSlipGenerationRule.RuleName.Should().Be(_rule.Name);
    }

    [Fact]
    public void Applies_only_to_orders_with_physical_products()
    {
        _rule.AppliesTo(new PurchaseOrderBuilder().WithBook().Build()).Should().BeTrue();
        _rule.AppliesTo(new PurchaseOrderBuilder().WithKataExampleLines().Build()).Should().BeTrue();
        _rule.AppliesTo(new PurchaseOrderBuilder().WithVideo().WithMembership(MembershipType.Premium).Build()).Should().BeFalse();
    }

    [Fact]
    public async Task Generates_a_slip_for_the_physical_products_and_stages_it()
    {
        var customer = new CustomerBuilder().Build();
        var order = new PurchaseOrderBuilder().ForCustomer(customer).WithBook().WithVideo().WithBook().Build();
        var context = new PurchaseOrderProcessingContext(order, customer, TestClock.Now);

        await _rule.ApplyAsync(context, CancellationToken.None);

        var effect = context.Effects.Should().ContainSingle().Which.Should().BeOfType<ShippingSlipGeneratedEffect>().Subject;
        effect.RuleName.Should().Be(_rule.Name);
        effect.ShippingSlip.Id.Should().Be(NextId);
        effect.ShippingSlip.PurchaseOrderId.Should().Be(order.Id);
        effect.ShippingSlip.CustomerId.Should().Be(customer.Id);
        effect.ShippingSlip.Address.Should().Be(TestAddresses.London());
        effect.ShippingSlip.GeneratedAt.Should().Be(TestClock.Now);
        effect.ShippingSlip.Items.Should().ContainSingle()
            .Which.Should().Be(new ShippingSlipItem(new ProductId(TestProducts.DefaultBookId), "The Girl on the train", 2));
        _shippingSlips.Received(1).Add(effect.ShippingSlip);
    }

    [Fact]
    public async Task Fails_for_a_customer_without_shipping_address_and_stages_nothing()
    {
        var customer = new CustomerBuilder().WithoutShippingAddress().Build();
        var order = new PurchaseOrderBuilder().ForCustomer(customer).WithBook().Build();
        var context = new PurchaseOrderProcessingContext(order, customer, TestClock.Now);

        var act = () => _rule.ApplyAsync(context, CancellationToken.None);

        await act.Should().ThrowAsync<BusinessRuleViolationException>().Where(ex => ex.Code == "customer.shipping_address.missing");
        context.Effects.Should().BeEmpty();
        _shippingSlips.DidNotReceive().Add(Arg.Any<ShippingSlip>());
    }

    [Fact]
    public async Task Rejects_null_context_and_dependencies()
    {
        var nullContext = () => _rule.ApplyAsync(null!, CancellationToken.None);
        var nullRepository = () => new ShippingSlipGenerationRule(null!, NullLogger<ShippingSlipGenerationRule>.Instance);
        var nullLogger = () => new ShippingSlipGenerationRule(_shippingSlips, null!);
        var nullOrder = () => _rule.AppliesTo(null!);

        await nullContext.Should().ThrowAsync<ArgumentNullException>();
        nullRepository.Should().Throw<ArgumentNullException>();
        nullLogger.Should().Throw<ArgumentNullException>();
        nullOrder.Should().Throw<ArgumentNullException>();
    }
}
