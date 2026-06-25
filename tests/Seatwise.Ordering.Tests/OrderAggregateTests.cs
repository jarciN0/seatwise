using FluentAssertions;
using Seatwise.Ordering.Domain;
using Xunit;

namespace Seatwise.Ordering.Tests;

// Pure unit tests of the Order aggregate's decisions + folds. No infrastructure
// (blueprint §2.12 unit layer). These encode the state machine as executable
// specs and protect the oversell/lifecycle invariants.
public sealed class OrderAggregateTests
{
    private static readonly Guid ShowingId = Guid.NewGuid();
    private static readonly Guid CustomerId = Guid.NewGuid();
    private static readonly DateTimeOffset Now = new(2026, 6, 25, 12, 0, 0, TimeSpan.Zero);

    private static Order HeldOrder(out Guid orderId)
    {
        orderId = Guid.NewGuid();
        var held = Order.Hold(orderId, ShowingId, CustomerId, [Guid.NewGuid()], Now, TimeSpan.FromSeconds(120), Guid.NewGuid());
        return Order.Create(held);
    }

    [Fact]
    public void Hold_sets_drafting_state_and_expiry()
    {
        var order = HeldOrder(out var orderId);

        order.Id.Should().Be(orderId);
        order.Status.Should().Be(OrderStatus.Drafting);
        order.HoldExpiresAtUtc.Should().Be(Now.AddSeconds(120));
    }

    [Fact]
    public void Hold_with_no_seats_is_rejected()
    {
        var act = () => Order.Hold(Guid.NewGuid(), ShowingId, CustomerId, [], Now, TimeSpan.FromSeconds(120), Guid.NewGuid());
        act.Should().Throw<InvalidOrderTransitionException>();
    }

    [Fact]
    public void Reserve_before_expiry_moves_to_reserved()
    {
        var order = HeldOrder(out _);

        var reserved = order.Reserve(Now.AddSeconds(10));
        order.Apply(reserved);

        order.Status.Should().Be(OrderStatus.Reserved);
    }

    [Fact]
    public void Reserve_after_expiry_is_rejected()
    {
        var order = HeldOrder(out _);

        var act = () => order.Reserve(Now.AddSeconds(121));
        act.Should().Throw<InvalidOrderTransitionException>();
    }

    [Fact]
    public void Full_happy_path_reaches_confirmed()
    {
        var order = HeldOrder(out _);
        order.Apply(order.Reserve(Now.AddSeconds(5)));
        order.Apply(order.RequestPayment(50m, "EUR", "idem-1"));

        var confirmed = order.Confirm(Guid.NewGuid(), Now.AddSeconds(20), ["TICKET-001"]);
        order.Apply(confirmed);

        order.Status.Should().Be(OrderStatus.Confirmed);
        order.TicketCodes.Should().ContainSingle().Which.Should().Be("TICKET-001");
    }

    [Fact]
    public void Expire_is_noop_once_confirmed()
    {
        var order = HeldOrder(out _);
        order.Apply(order.Reserve(Now.AddSeconds(5)));
        order.Apply(order.RequestPayment(50m, "EUR", "idem-1"));
        order.Apply(order.Confirm(Guid.NewGuid(), Now.AddSeconds(20), ["T1"]));

        // Expiry-vs-confirm race: confirm wins, expiry is a no-op (blueprint §2.9).
        order.Expire(Now.AddSeconds(121)).Should().BeNull();
    }

    [Fact]
    public void Decline_then_cancel_releases_seats()
    {
        var order = HeldOrder(out _);
        order.Apply(order.Reserve(Now.AddSeconds(5)));
        order.Apply(order.RequestPayment(50m, "EUR", "idem-1"));
        order.Apply(new PaymentFailed(order.Id, "card declined"));

        var cancelled = order.Cancel("payment declined");
        order.Apply(cancelled);

        order.Status.Should().Be(OrderStatus.Cancelled);
        cancelled.SeatIds.Should().BeEquivalentTo(order.SeatIds);
    }
}
