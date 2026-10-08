using FluentAssertions;
using FunBooksAndVideos.Application.Processing;
using FunBooksAndVideos.Domain.Customers;
using FunBooksAndVideos.Domain.Shipping;
using FunBooksAndVideos.TestKit;

namespace FunBooksAndVideos.Application.Tests.Processing;

public sealed class ProcessingContextAndEffectTests
{
    [Fact]
    public void Context_rejects_null_order_customer_and_effects()
    {
        var customer = new CustomerBuilder().Build();
        var order = new PurchaseOrderBuilder().ForCustomer(customer).Build();

        var nullOrder = () => new PurchaseOrderProcessingContext(null!, customer, TestClock.Now);
        var nullCustomer = () => new PurchaseOrderProcessingContext(order, null!, TestClock.Now);
        var nullEffect = () => new PurchaseOrderProcessingContext(order, customer, TestClock.Now).AddEffect(null!);

        nullOrder.Should().Throw<ArgumentNullException>();
        nullCustomer.Should().Throw<ArgumentNullException>();
        nullEffect.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Context_collects_effects_in_order()
    {
        var customer = new CustomerBuilder().Build();
        var order = new PurchaseOrderBuilder().ForCustomer(customer).Build();
        var context = new PurchaseOrderProcessingContext(order, customer, TestClock.Now);
        var first = new MembershipActivationEffect("r", new MembershipActivationResult(MembershipType.BookClub, MembershipActivationOutcome.Activated, TestClock.Now));
        var second = new MembershipActivationEffect("r", new MembershipActivationResult(MembershipType.VideoClub, MembershipActivationOutcome.AlreadyActive, null));

        context.AddEffect(first);
        context.AddEffect(second);

        context.Effects.Should().Equal(first, second);
        context.AppliedRules.Should().BeEmpty();
    }

    [Fact]
    public void Membership_effect_describes_the_outcome()
    {
        var activated = new MembershipActivationEffect("BR1", new MembershipActivationResult(MembershipType.BookClub, MembershipActivationOutcome.Activated, TestClock.Now));
        var alreadyActive = new MembershipActivationEffect("BR1", new MembershipActivationResult(MembershipType.Premium, MembershipActivationOutcome.AlreadyActive, null));

        activated.Description.Should().Be("Book Club Membership activated.");
        alreadyActive.Description.Should().Be("Premium Membership was already active; the account was not changed.");
        activated.RuleName.Should().Be("BR1");
    }

    [Fact]
    public void Shipping_slip_effect_describes_the_slip()
    {
        var customer = new CustomerBuilder().Build();
        var order = new PurchaseOrderBuilder().ForCustomer(customer).WithBook().WithBook().Build();
        var slip = ShippingSlipFactory.CreateFor(new ShippingSlipId(9), order, customer, TestClock.Now);

        var effect = new ShippingSlipGeneratedEffect("BR2", slip);

        effect.Description.Should().Be("Shipping slip 9 generated with 1 item(s).");
        effect.ShippingSlip.Should().BeSameAs(slip);
    }

    [Fact]
    public void Effects_validate_their_arguments()
    {
        var blankRule = () => new MembershipActivationEffect(" ", new MembershipActivationResult(MembershipType.BookClub, MembershipActivationOutcome.Activated, null));
        var nullResult = () => new MembershipActivationEffect("BR1", null!);
        var nullSlip = () => new ShippingSlipGeneratedEffect("BR2", null!);

        blankRule.Should().Throw<ArgumentException>();
        nullResult.Should().Throw<ArgumentNullException>();
        nullSlip.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Result_filters_effects_by_type_and_validates_arguments()
    {
        var customer = new CustomerBuilder().Build();
        var order = new PurchaseOrderBuilder().ForCustomer(customer).WithBook().Build();
        var slip = ShippingSlipFactory.CreateFor(new ShippingSlipId(1), order, customer, TestClock.Now);
        var membership = new MembershipActivationEffect("BR1", new MembershipActivationResult(MembershipType.BookClub, MembershipActivationOutcome.Activated, TestClock.Now));
        var shipping = new ShippingSlipGeneratedEffect("BR2", slip);

        var result = new PurchaseOrderProcessingResult(order, [membership, shipping], ["BR1", "BR2"]);

        result.EffectsOf<MembershipActivationEffect>().Should().ContainSingle().Which.Should().BeSameAs(membership);
        result.EffectsOf<ShippingSlipGeneratedEffect>().Should().ContainSingle().Which.Should().BeSameAs(shipping);

        var nullOrder = () => new PurchaseOrderProcessingResult(null!, [], []);
        var nullEffects = () => new PurchaseOrderProcessingResult(order, null!, []);
        var nullRules = () => new PurchaseOrderProcessingResult(order, [], null!);

        nullOrder.Should().Throw<ArgumentNullException>();
        nullEffects.Should().Throw<ArgumentNullException>();
        nullRules.Should().Throw<ArgumentNullException>();
    }
}
