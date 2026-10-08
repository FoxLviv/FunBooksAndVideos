using FluentAssertions;
using FunBooksAndVideos.Application.Processing;
using FunBooksAndVideos.Domain.Common;
using FunBooksAndVideos.Domain.Customers;
using FunBooksAndVideos.Domain.Orders;
using FunBooksAndVideos.TestKit;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;

namespace FunBooksAndVideos.Application.Tests.Processing;

public sealed class PurchaseOrderProcessorTests
{
    private readonly FakeTimeProvider _time = TestClock.Create();
    private readonly Customer _customer = new CustomerBuilder().Build();
    private readonly List<string> _log = [];

    private PurchaseOrderProcessor CreateProcessor(params IPurchaseOrderRule[] rules) =>
        new(rules, _time, NullLogger<PurchaseOrderProcessor>.Instance);

    private PurchaseOrder KataOrder() => new PurchaseOrderBuilder().ForCustomer(_customer).WithKataExampleLines().Build();

    [Fact]
    public async Task Applies_only_the_applicable_rules_in_registration_order_and_marks_the_order_processed()
    {
        var order = KataOrder();
        var first = new FakeRule("first", _ => true, _log);
        var skipped = new FakeRule("skipped", _ => false, _log);
        var second = new FakeRule("second", o => o.RequiresShipping, _log);

        var result = await CreateProcessor(first, skipped, second).ProcessAsync(order, _customer, CancellationToken.None);

        _log.Should().Equal("first", "second");
        result.AppliedRules.Should().Equal("first", "second");
        result.Effects.Select(effect => effect.RuleName).Should().Equal("first", "second");
        result.Order.Should().BeSameAs(order);
        order.Status.Should().Be(PurchaseOrderStatus.Processed);
        order.ProcessedAt.Should().Be(TestClock.Now);
    }

    [Fact]
    public async Task Every_rule_sees_the_same_timestamp_that_ends_up_on_the_order()
    {
        var order = KataOrder();
        DateTimeOffset? seen = null;
        var rule = new FakeRule("rule", _ => true, _log, onApply: context => seen = context.ProcessedAt);
        _time.Advance(TimeSpan.FromMinutes(5));

        await CreateProcessor(rule).ProcessAsync(order, _customer, CancellationToken.None);

        seen.Should().Be(TestClock.Now.AddMinutes(5));
        order.ProcessedAt.Should().Be(seen);
    }

    [Fact]
    public async Task Rules_receive_the_order_and_the_customer_through_the_context()
    {
        var order = KataOrder();
        PurchaseOrderProcessingContext? captured = null;
        var rule = new FakeRule("rule", _ => true, _log, onApply: context => captured = context);

        await CreateProcessor(rule).ProcessAsync(order, _customer, CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.Order.Should().BeSameAs(order);
        captured.Customer.Should().BeSameAs(_customer);
    }

    [Fact]
    public async Task No_rules_still_marks_the_order_processed()
    {
        var order = KataOrder();

        var result = await CreateProcessor().ProcessAsync(order, _customer, CancellationToken.None);

        result.AppliedRules.Should().BeEmpty();
        result.Effects.Should().BeEmpty();
        order.Status.Should().Be(PurchaseOrderStatus.Processed);
    }

    [Fact]
    public async Task Refuses_to_process_an_order_twice()
    {
        var order = KataOrder();
        var rule = new FakeRule("rule", _ => true, _log);
        var processor = CreateProcessor(rule);
        await processor.ProcessAsync(order, _customer, CancellationToken.None);

        var act = () => processor.ProcessAsync(order, _customer, CancellationToken.None);

        await act.Should().ThrowAsync<BusinessRuleViolationException>().Where(ex => ex.Code == "purchase_order.already_processed");
        _log.Should().HaveCount(1);
    }

    [Fact]
    public async Task Rejects_a_customer_that_does_not_own_the_order()
    {
        var order = new PurchaseOrderBuilder().ForCustomer(_customer.Id.Value + 1).Build();

        var act = () => CreateProcessor().ProcessAsync(order, _customer, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>();
        order.Status.Should().Be(PurchaseOrderStatus.Pending);
    }

    [Fact]
    public async Task A_failing_rule_leaves_the_order_pending_and_stops_the_chain()
    {
        var order = KataOrder();
        var failing = new FakeRule("failing", _ => true, _log, failure: new BusinessRuleViolationException("test.failure", "boom"));
        var never = new FakeRule("never", _ => true, _log);

        var act = () => CreateProcessor(failing, never).ProcessAsync(order, _customer, CancellationToken.None);

        await act.Should().ThrowAsync<BusinessRuleViolationException>().Where(ex => ex.Code == "test.failure");
        _log.Should().BeEmpty();
        order.Status.Should().Be(PurchaseOrderStatus.Pending);
        order.ProcessedAt.Should().BeNull();
    }

    [Fact]
    public async Task Honours_cancellation_before_applying_a_rule()
    {
        var order = KataOrder();
        var rule = new FakeRule("rule", _ => true, _log);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        var act = () => CreateProcessor(rule).ProcessAsync(order, _customer, cancellation.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
        _log.Should().BeEmpty();
        order.Status.Should().Be(PurchaseOrderStatus.Pending);
    }

    [Fact]
    public void Rule_names_are_exposed_in_execution_order()
    {
        var processor = CreateProcessor(new FakeRule("b", _ => true, _log), new FakeRule("a", _ => true, _log));

        processor.RuleNames.Should().Equal("b", "a");
    }

    [Fact]
    public void Constructor_rejects_duplicate_rule_names()
    {
        var act = () => CreateProcessor(new FakeRule("same", _ => true, _log), new FakeRule("same", _ => true, _log));

        act.Should().Throw<ArgumentException>().WithMessage("*same*");
    }

    [Fact]
    public void Constructor_rejects_null_rules_and_dependencies()
    {
        var nullEntry = () => CreateProcessor(new FakeRule("a", _ => true, _log), null!);
        var nullRules = () => new PurchaseOrderProcessor(null!, _time, NullLogger<PurchaseOrderProcessor>.Instance);
        var nullTime = () => new PurchaseOrderProcessor([], null!, NullLogger<PurchaseOrderProcessor>.Instance);
        var nullLogger = () => new PurchaseOrderProcessor([], _time, null!);

        nullEntry.Should().Throw<ArgumentException>();
        nullRules.Should().Throw<ArgumentNullException>();
        nullTime.Should().Throw<ArgumentNullException>();
        nullLogger.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task ProcessAsync_rejects_null_arguments()
    {
        var processor = CreateProcessor();

        var nullOrder = () => processor.ProcessAsync(null!, _customer, CancellationToken.None);
        var nullCustomer = () => processor.ProcessAsync(KataOrder(), null!, CancellationToken.None);

        await nullOrder.Should().ThrowAsync<ArgumentNullException>();
        await nullCustomer.Should().ThrowAsync<ArgumentNullException>();
    }

    private sealed class FakeRule(
        string name,
        Func<PurchaseOrder, bool> applies,
        List<string> log,
        Action<PurchaseOrderProcessingContext>? onApply = null,
        Exception? failure = null) : IPurchaseOrderRule
    {
        public string Name => name;

        public bool AppliesTo(PurchaseOrder order) => applies(order);

        public Task ApplyAsync(PurchaseOrderProcessingContext context, CancellationToken cancellationToken)
        {
            if (failure is not null)
            {
                throw failure;
            }

            log.Add(name);
            onApply?.Invoke(context);
            context.AddEffect(new FakeEffect(name));
            return Task.CompletedTask;
        }
    }

    private sealed record FakeEffect : ProcessingEffect
    {
        public FakeEffect(string ruleName)
            : base(ruleName)
        {
        }

        public override string Description => $"fake effect of {RuleName}";
    }
}
