using FluentAssertions;
using FunBooksAndVideos.Application.Processing;
using FunBooksAndVideos.Application.Processing.Rules;
using FunBooksAndVideos.Domain.Customers;
using FunBooksAndVideos.Domain.Orders;
using FunBooksAndVideos.Domain.Persistence;
using FunBooksAndVideos.TestKit;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace FunBooksAndVideos.Application.Tests.Processing.Rules;

public sealed class MembershipActivationRuleTests
{
    private readonly ICustomerRepository _customers = Substitute.For<ICustomerRepository>();
    private readonly MembershipActivationRule _rule;

    public MembershipActivationRuleTests()
    {
        _rule = new MembershipActivationRule(_customers, NullLogger<MembershipActivationRule>.Instance);
    }

    private static PurchaseOrderProcessingContext ContextFor(Customer customer, PurchaseOrder order) =>
        new(order, customer, TestClock.Now);

    [Fact]
    public void Is_named_after_business_rule_one()
    {
        _rule.Name.Should().Be("BR1.MembershipActivation");
        MembershipActivationRule.RuleName.Should().Be(_rule.Name);
    }

    [Fact]
    public void Applies_only_to_orders_with_membership_lines()
    {
        _rule.AppliesTo(new PurchaseOrderBuilder().WithMembership(MembershipType.BookClub).Build()).Should().BeTrue();
        _rule.AppliesTo(new PurchaseOrderBuilder().WithKataExampleLines().Build()).Should().BeTrue();
        _rule.AppliesTo(new PurchaseOrderBuilder().WithBook().WithVideo().Build()).Should().BeFalse();
    }

    [Fact]
    public async Task Activates_every_membership_immediately_and_stages_the_account_once()
    {
        var customer = new CustomerBuilder().Build();
        var order = new PurchaseOrderBuilder().ForCustomer(customer).WithMembership(MembershipType.BookClub).WithMembership(MembershipType.VideoClub).Build();
        var context = ContextFor(customer, order);

        await _rule.ApplyAsync(context, CancellationToken.None);

        customer.Memberships.Select(membership => membership.Type).Should().Equal(MembershipType.BookClub, MembershipType.VideoClub);
        customer.Memberships.Should().OnlyContain(membership => membership.ActivatedAt == TestClock.Now);
        context.Effects.Should().AllBeOfType<MembershipActivationEffect>().And.HaveCount(2);
        context.Effects.Cast<MembershipActivationEffect>().Select(effect => effect.Result.Outcome)
            .Should().OnlyContain(outcome => outcome == MembershipActivationOutcome.Activated);
        _customers.Received(1).Update(customer);
    }

    [Fact]
    public async Task Reports_an_already_active_membership_without_staging_a_write()
    {
        var customer = new CustomerBuilder().WithMembership(MembershipType.Premium).Build();
        var order = new PurchaseOrderBuilder().ForCustomer(customer).WithMembership(MembershipType.BookClub).Build();
        var context = ContextFor(customer, order);

        await _rule.ApplyAsync(context, CancellationToken.None);

        customer.Memberships.Should().HaveCount(1);
        context.Effects.Should().ContainSingle().Which.Should().BeOfType<MembershipActivationEffect>()
            .Which.Result.Should().Be(new MembershipActivationResult(MembershipType.BookClub, MembershipActivationOutcome.AlreadyActive, null));
        _customers.DidNotReceive().Update(Arg.Any<Customer>());
    }

    [Fact]
    public async Task Stages_a_write_when_at_least_one_membership_is_new()
    {
        var customer = new CustomerBuilder().WithMembership(MembershipType.BookClub).Build();
        var order = new PurchaseOrderBuilder().ForCustomer(customer).WithMembership(MembershipType.BookClub).WithMembership(MembershipType.VideoClub).Build();
        var context = ContextFor(customer, order);

        await _rule.ApplyAsync(context, CancellationToken.None);

        context.Effects.Cast<MembershipActivationEffect>().Select(effect => effect.Result.Outcome)
            .Should().Equal(MembershipActivationOutcome.AlreadyActive, MembershipActivationOutcome.Activated);
        customer.ActiveClubs.Should().Be(Club.Book | Club.Video);
        _customers.Received(1).Update(customer);
    }

    [Fact]
    public async Task Rejects_null_context()
    {
        var act = () => _rule.ApplyAsync(null!, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public void Rejects_null_dependencies()
    {
        var nullRepository = () => new MembershipActivationRule(null!, NullLogger<MembershipActivationRule>.Instance);
        var nullLogger = () => new MembershipActivationRule(_customers, null!);

        nullRepository.Should().Throw<ArgumentNullException>();
        nullLogger.Should().Throw<ArgumentNullException>();
        var nullOrder = () => _rule.AppliesTo(null!);
        nullOrder.Should().Throw<ArgumentNullException>();
    }
}
