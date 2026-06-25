namespace Seatwise.Contracts.V1;

// Versioned integration-event contracts (ADR-0007). These are the ONLY types
// that cross the message bus. Domain events stay private inside Ordering.
//
// Evolution rule: additive-only within V1. A breaking change becomes a new V2
// record + a dual-publish window. Every message carries a CorrelationId (saga
// correlation + OTel trace linkage) and UTC timestamps.

// Ordering -> bus (consumed by Catalog availability + Notifications)
public sealed record SeatsReservedV1(
    Guid OrderId,
    Guid ShowingId,
    Guid CustomerId,
    IReadOnlyList<Guid> SeatIds,
    DateTimeOffset ReservedAtUtc,
    Guid CorrelationId);

public sealed record HoldExpiredV1(
    Guid OrderId,
    Guid ShowingId,
    IReadOnlyList<Guid> SeatIds,
    DateTimeOffset ExpiredAtUtc,
    Guid CorrelationId);

public sealed record OrderConfirmedV1(
    Guid OrderId,
    Guid ShowingId,
    Guid CustomerId,
    IReadOnlyList<Guid> SeatIds,
    IReadOnlyList<string> TicketCodes,
    DateTimeOffset ConfirmedAtUtc,
    Guid CorrelationId);

public sealed record OrderCancelledV1(
    Guid OrderId,
    Guid ShowingId,
    IReadOnlyList<Guid> SeatIds,
    string Reason,
    Guid CorrelationId);

// Ordering saga -> Payments
public sealed record PaymentRequestedV1(
    Guid OrderId,
    decimal Amount,
    string Currency,
    string IdempotencyKey,
    string CardLast4,
    Guid CorrelationId);

// Payments -> Ordering saga
public sealed record PaymentSucceededV1(
    Guid OrderId,
    Guid PaymentId,
    DateTimeOffset PaidAtUtc,
    Guid CorrelationId);

public sealed record PaymentFailedV1(
    Guid OrderId,
    string Reason,
    Guid CorrelationId);
