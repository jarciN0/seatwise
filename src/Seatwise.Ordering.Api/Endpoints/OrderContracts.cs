namespace Seatwise.Ordering.Api.Endpoints;

// API DTOs (blueprint §2.3). Hand-written records — no AutoMapper (commercial,
// ADR-0010). ProblemDetails (RFC 9457) is the uniform error shape.

public sealed record HoldSeatsRequest(Guid ShowingId, IReadOnlyList<Guid> SeatIds);

public sealed record HoldResponse(Guid OrderId, IReadOnlyList<Guid> HeldSeatIds, DateTimeOffset HoldExpiresAtUtc);

public sealed record CheckoutRequest(string PaymentToken, decimal Amount, string Currency, string CardLast4);

public sealed record OrderDto(
    Guid OrderId,
    Guid ShowingId,
    Guid CustomerId,
    string Status,
    IReadOnlyList<Guid> SeatIds,
    DateTimeOffset HoldExpiresAtUtc,
    IReadOnlyList<string> TicketCodes);
