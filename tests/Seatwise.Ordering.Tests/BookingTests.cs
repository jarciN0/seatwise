using FluentAssertions;
using Seatwise.Ordering.Domain;
using Xunit;

namespace Seatwise.Ordering.Tests;

// Tests for the booking rules. They run entirely in memory — no database — by
// building a booking from its events and checking what each method allows.
public sealed class BookingTests
{
    private static readonly Guid FlightId = Guid.NewGuid();
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;
    private static readonly TimeSpan Hold = TimeSpan.FromSeconds(120);
    private static readonly string[] Seats = ["1A", "1B"];

    // Builds a booking that has already been held.
    private static Booking HeldBooking()
        => Booking.Create(Booking.Hold(Guid.NewGuid(), FlightId, "cust-1", Seats, Now, Hold));

    [Fact]
    public void Hold_sets_seats_and_expiry_and_starts_in_Held()
    {
        var booking = HeldBooking();

        booking.Status.Should().Be(BookingStatus.Held);
        booking.Seats.Should().BeEquivalentTo(Seats);
        booking.HoldExpiresAtUtc.Should().Be(Now.Add(Hold));
    }

    [Fact]
    public void Hold_with_no_seats_is_rejected()
    {
        var act = () => Booking.Hold(Guid.NewGuid(), FlightId, "cust-1", [], Now, Hold);
        act.Should().Throw<InvalidBookingTransition>();
    }

    [Fact]
    public void Checkout_moves_a_valid_hold_to_AwaitingPayment()
    {
        var booking = HeldBooking();

        var started = booking.StartCheckout(100m, "EUR", Now);
        booking.Apply(started);

        booking.Status.Should().Be(BookingStatus.AwaitingPayment);
    }

    [Fact]
    public void Checkout_after_the_hold_expired_is_rejected()
    {
        var booking = HeldBooking();

        var act = () => booking.StartCheckout(100m, "EUR", Now.Add(Hold).AddSeconds(1));
        act.Should().Throw<InvalidBookingTransition>();
    }

    [Fact]
    public void Confirm_after_payment_issues_a_ticket_per_seat()
    {
        var booking = HeldBooking();
        booking.Apply(booking.StartCheckout(100m, "EUR", Now));

        var confirmed = booking.Confirm(Guid.NewGuid(), Now, ["TKT-1A", "TKT-1B"]);
        booking.Apply(confirmed);

        booking.Status.Should().Be(BookingStatus.Confirmed);
        booking.TicketCodes.Should().HaveCount(Seats.Length);
    }

    [Fact]
    public void Confirm_before_checkout_is_rejected()
    {
        var booking = HeldBooking();

        var act = () => booking.Confirm(Guid.NewGuid(), Now, ["TKT"]);
        act.Should().Throw<InvalidBookingTransition>();
    }

    [Fact]
    public void Cancel_releases_a_held_booking()
    {
        var booking = HeldBooking();

        booking.Apply(booking.Cancel("changed mind"));

        booking.Status.Should().Be(BookingStatus.Cancelled);
    }

    [Fact]
    public void A_confirmed_booking_cannot_be_cancelled()
    {
        var booking = HeldBooking();
        booking.Apply(booking.StartCheckout(100m, "EUR", Now));
        booking.Apply(booking.Confirm(Guid.NewGuid(), Now, ["TKT-1A", "TKT-1B"]));

        var act = () => booking.Cancel("too late");
        act.Should().Throw<InvalidBookingTransition>();
    }

    [Fact]
    public void Expiring_a_confirmed_booking_does_nothing()
    {
        var booking = HeldBooking();
        booking.Apply(booking.StartCheckout(100m, "EUR", Now));
        booking.Apply(booking.Confirm(Guid.NewGuid(), Now, ["TKT-1A", "TKT-1B"]));

        booking.Expire(Now).Should().BeNull();
    }

    [Fact]
    public void A_held_booking_can_expire()
    {
        var booking = HeldBooking();

        booking.Expire(Now.Add(Hold).AddSeconds(1)).Should().NotBeNull();
    }
}
