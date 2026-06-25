namespace Seatwise.Ordering.Domain;

// Domain events — the Order aggregate's PRIVATE vocabulary (ADR-0007).
// These are appended to the Marten event stream. They are distinct from the
// versioned integration events in Seatwise.Contracts.V1 that cross the bus.
//
// Canonical names from the blueprint §2.2 — used in code, tests, and CLAUDE.md.

public sealed record SeatsHeld(
    Guid OrderId,
    Guid ShowingId,
    Guid CustomerId,
    IReadOnlyList<Guid> SeatIds,
    DateTimeOffset HoldExpiresAtUtc,
    Guid CorrelationId);

public sealed record SeatAddedToHold(Guid OrderId, Guid SeatId);

public sealed record SeatsReserved(
    Guid OrderId,
    IReadOnlyList<Guid> SeatIds,
    DateTimeOffset ReservedAtUtc);

public sealed record PaymentRequested(
    Guid OrderId,
    decimal Amount,
    string Currency,
    string IdempotencyKey);

public sealed record PaymentSucceeded(Guid OrderId, Guid PaymentId, DateTimeOffset PaidAtUtc);

public sealed record PaymentFailed(Guid OrderId, string Reason);

public sealed record OrderConfirmed(
    Guid OrderId,
    IReadOnlyList<Guid> SeatIds,
    DateTimeOffset ConfirmedAtUtc,
    IReadOnlyList<string> TicketCodes);

public sealed record HoldExpired(
    Guid OrderId,
    IReadOnlyList<Guid> SeatIds,
    DateTimeOffset ExpiredAtUtc);

public sealed record OrderCancelled(
    Guid OrderId,
    IReadOnlyList<Guid> SeatIds,
    string Reason);

// STRETCH — designed, deferred (blueprint §3.8). Event exists; no handler yet.
public sealed record OrderRefunded(Guid OrderId, Guid RefundId, DateTimeOffset RefundedAtUtc);
