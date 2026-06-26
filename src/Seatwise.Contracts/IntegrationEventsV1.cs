namespace Seatwise.Contracts.V1;

// The messages that travel between the Booking service and the Payments service
// over RabbitMQ. These are the only types both services share, so changes here
// affect both — keep them additive. CorrelationId ties a request to its result.

/// <summary>Booking → Payments: "please charge this card for this booking".</summary>
public sealed record PaymentRequestedV1(
    Guid BookingId,
    decimal Amount,
    string Currency,
    string CardLast4,
    Guid CorrelationId);

/// <summary>Payments → Booking: "the charge went through".</summary>
public sealed record PaymentSucceededV1(
    Guid BookingId,
    Guid PaymentId,
    DateTimeOffset PaidAtUtc,
    Guid CorrelationId);

/// <summary>Payments → Booking: "the charge was declined".</summary>
public sealed record PaymentFailedV1(
    Guid BookingId,
    string Reason,
    Guid CorrelationId);
