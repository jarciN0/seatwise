namespace Seatwise.Ordering.Domain;

// The things that can happen to a booking, in order. Marten stores these in the
// booking's event stream and replays them to rebuild the current state.
// They live only inside this service; the messages other services see are the
// separate records in Seatwise.Contracts.V1.

/// <summary>Seats were held for a customer until <see cref="HoldExpiresAtUtc"/>.</summary>
public sealed record SeatsHeld(
    Guid BookingId,
    Guid FlightId,
    string CustomerRef,
    IReadOnlyList<string> Seats,
    DateTimeOffset HoldExpiresAtUtc);

/// <summary>The customer started paying; we're now waiting for the payment result.</summary>
public sealed record CheckoutStarted(Guid BookingId, decimal Amount, string Currency);

/// <summary>Payment succeeded; the booking is final and tickets were issued.</summary>
public sealed record BookingConfirmed(
    Guid BookingId,
    Guid PaymentId,
    IReadOnlyList<string> Seats,
    IReadOnlyList<string> TicketCodes,
    DateTimeOffset ConfirmedAtUtc);

/// <summary>The booking was cancelled (e.g. payment failed) and the seats freed.</summary>
public sealed record BookingCancelled(Guid BookingId, IReadOnlyList<string> Seats, string Reason);

/// <summary>The hold ran out before payment, so the seats were freed.</summary>
public sealed record HoldExpired(Guid BookingId, IReadOnlyList<string> Seats, DateTimeOffset ExpiredAtUtc);
