namespace Seatwise.Ordering.Api.Endpoints;

// The request and response shapes for the HTTP API. Plain records, mapped by hand.

public sealed record HoldRequest(Guid FlightId, string CustomerRef, IReadOnlyList<string> Seats);

public sealed record HoldResponse(Guid BookingId, IReadOnlyList<string> Seats, DateTimeOffset HoldExpiresAtUtc);

public sealed record CheckoutRequest(string CardLast4);

public sealed record BookingResponse(
    Guid BookingId,
    Guid FlightId,
    string Status,
    IReadOnlyList<string> Seats,
    DateTimeOffset HoldExpiresAtUtc,
    IReadOnlyList<string> TicketCodes);

public sealed record FlightSummary(
    Guid Id,
    string Number,
    string Origin,
    string Destination,
    DateTimeOffset DepartureUtc,
    decimal PricePerSeat,
    string Currency,
    int SeatsAvailable);

public sealed record FlightDetail(
    Guid Id,
    string Number,
    string Origin,
    string Destination,
    DateTimeOffset DepartureUtc,
    string Aircraft,
    decimal PricePerSeat,
    string Currency,
    IReadOnlyList<string> AvailableSeats);
