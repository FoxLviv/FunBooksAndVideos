using FluentAssertions;
using FunBooksAndVideos.Domain.Common;
using FunBooksAndVideos.Domain.Customers;
using FunBooksAndVideos.TestKit;

namespace FunBooksAndVideos.Domain.Tests.Customers;

public sealed class CustomerTests
{
    private static readonly DateTimeOffset Now = TestClock.Now;

    [Fact]
    public void Create_produces_a_customer_without_memberships()
    {
        var customer = Customer.Create(new CustomerId(4567890), "  Jane Doe ", TestAddresses.London());

        customer.Id.Value.Should().Be(4567890);
        customer.Name.Should().Be("Jane Doe");
        customer.ShippingAddress.Should().Be(TestAddresses.London());
        customer.Memberships.Should().BeEmpty();
        customer.ActiveClubs.Should().Be(Club.None);
        customer.Version.Should().Be(0);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Create_requires_a_name(string? name)
    {
        var act = () => Customer.Create(new CustomerId(1), name!, null);

        act.Should().Throw<DomainValidationException>().Which.Code.Should().Be("customer.name.invalid");
    }

    [Fact]
    public void Create_limits_the_name_length()
    {
        var act = () => Customer.Create(new CustomerId(1), new string('n', Customer.MaxNameLength + 1), null);

        act.Should().Throw<DomainValidationException>().Which.Code.Should().Be("customer.name.invalid");
    }

    [Fact]
    public void Customer_ids_must_be_positive()
    {
        var act = () => new CustomerId(0);

        act.Should().Throw<DomainValidationException>().Which.Code.Should().Be("customer_id.invalid");
    }

    [Fact]
    public void Activating_a_new_membership_records_it_with_the_activation_time()
    {
        var customer = new CustomerBuilder().Build();

        var result = customer.ActivateMembership(MembershipType.BookClub, Now);

        result.Should().Be(new MembershipActivationResult(MembershipType.BookClub, MembershipActivationOutcome.Activated, Now));
        customer.Memberships.Should().ContainSingle().Which.Should().Be(new Membership(MembershipType.BookClub, Now));
        customer.ActiveClubs.Should().Be(Club.Book);
        customer.HasAccessTo(Club.Book).Should().BeTrue();
        customer.HasAccessTo(Club.Video).Should().BeFalse();
    }

    [Fact]
    public void Activating_the_same_membership_twice_is_idempotent()
    {
        var customer = new CustomerBuilder().WithMembership(MembershipType.BookClub).Build();

        var result = customer.ActivateMembership(MembershipType.BookClub, Now);

        result.Outcome.Should().Be(MembershipActivationOutcome.AlreadyActive);
        result.ActivatedAt.Should().BeNull();
        customer.Memberships.Should().HaveCount(1);
    }

    [Fact]
    public void Premium_on_top_of_book_club_is_an_upgrade()
    {
        var customer = new CustomerBuilder().WithMembership(MembershipType.BookClub).Build();

        var result = customer.ActivateMembership(MembershipType.Premium, Now);

        result.Outcome.Should().Be(MembershipActivationOutcome.Activated);
        customer.Memberships.Should().HaveCount(2);
        customer.ActiveClubs.Should().Be(Club.Book | Club.Video);
        customer.HasAccessTo(Club.Book | Club.Video).Should().BeTrue();
    }

    [Theory]
    [InlineData(MembershipType.BookClub)]
    [InlineData(MembershipType.VideoClub)]
    [InlineData(MembershipType.Premium)]
    public void Any_membership_is_already_active_for_a_premium_member(MembershipType requested)
    {
        var customer = new CustomerBuilder().WithMembership(MembershipType.Premium).Build();

        var result = customer.ActivateMembership(requested, Now);

        result.Outcome.Should().Be(MembershipActivationOutcome.AlreadyActive);
        customer.Memberships.Should().HaveCount(1);
    }

    [Fact]
    public void Book_club_and_video_club_together_grant_the_same_access_as_premium()
    {
        var customer = new CustomerBuilder().WithMembership(MembershipType.BookClub).Build();

        customer.ActivateMembership(MembershipType.VideoClub, Now).Outcome.Should().Be(MembershipActivationOutcome.Activated);

        customer.ActiveClubs.Should().Be(Club.Book | Club.Video);
        customer.ActivateMembership(MembershipType.Premium, Now).Outcome.Should().Be(MembershipActivationOutcome.AlreadyActive);
    }

    [Fact]
    public void HasAccessTo_none_is_always_false()
    {
        new CustomerBuilder().WithMembership(MembershipType.Premium).Build().HasAccessTo(Club.None).Should().BeFalse();
        new CustomerBuilder().Build().HasAccessTo(Club.None).Should().BeFalse();
    }

    [Fact]
    public void Activating_an_undefined_membership_type_fails_without_changing_the_account()
    {
        var customer = new CustomerBuilder().Build();

        var act = () => customer.ActivateMembership((MembershipType)123, Now);

        act.Should().Throw<DomainValidationException>().Which.Code.Should().Be("membership_type.unsupported");
        customer.Memberships.Should().BeEmpty();
    }

    [Fact]
    public void Rehydrate_restores_memberships_and_version()
    {
        var memberships = new[] { new Membership(MembershipType.VideoClub, Now.AddDays(-1)) };

        var customer = Customer.Rehydrate(new CustomerId(5), "Jane", null, memberships, version: 3);

        customer.Version.Should().Be(3);
        customer.Memberships.Should().BeEquivalentTo(memberships);
        customer.ShippingAddress.Should().BeNull();
    }

    [Fact]
    public void Rehydrate_rejects_null_membership_collections_and_entries()
    {
        var nullCollection = () => Customer.Rehydrate(new CustomerId(5), "Jane", null, null!, 0);
        var nullEntry = () => Customer.Rehydrate(new CustomerId(5), "Jane", null, [null!], 0);

        nullCollection.Should().Throw<ArgumentNullException>();
        nullEntry.Should().Throw<DomainValidationException>().Which.Code.Should().Be("customer.memberships.invalid");
    }

    [Fact]
    public void Memberships_cannot_be_modified_from_outside()
    {
        var customer = new CustomerBuilder().Build();

        customer.Memberships.Should().BeAssignableTo<IReadOnlyList<Membership>>();
        customer.Memberships.Should().NotBeAssignableTo<List<Membership>>();
    }
}
