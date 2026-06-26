using Marten;
using Npgsql;
using Seatwise.Contracts.V1;
using Seatwise.Ordering.Api.Persistence;
using Seatwise.Ordering.Domain;
using Wolverine;

namespace Seatwise.Ordering.Api.Endpoints;

/// <summary>
/// The HTTP API: browse flights, hold seats, pay, and check a booking.
/// </summary>
public static class BookingEndpoints
{
    private static readonly TimeSpan HoldWindow = TimeSpan.FromSeconds(120);

    public static void MapBookingApi(this IEndpointRouteBuilder app)
    {
        var flights = app.MapGroup("/flights").WithTags("Flights");
        flights.MapGet("/", ListFlightsAsync);
        flights.MapGet("/{id:guid}", GetFlightAsync);

        var bookings = app.MapGroup("/bookings").WithTags("Bookings");
        bookings.MapPost("/", HoldAsync);
        bookings.MapPost("/{id:guid}/checkout", CheckoutAsync);
        bookings.MapGet("/{id:guid}", GetBookingAsync);
    }

    // GET /flights — every flight with how many seats are still free.
    private static async Task<IResult> ListFlightsAsync(IQuerySession db, CancellationToken ct)
    {
        var flights = await db.Query<Flight>().ToListAsync(ct);
        var now = DateTimeOffset.UtcNow;

        var result = new List<FlightSummary>(flights.Count);
        foreach (var f in flights)
        {
            var taken = await CountTakenSeatsAsync(db, f.Id, now, ct);
            result.Add(new FlightSummary(
                f.Id, f.Number, f.Origin, f.Destination, f.DepartureUtc,
                f.PricePerSeat, f.Currency, f.Seats.Count - taken));
        }

        return Results.Ok(result);
    }

    // GET /flights/{id} — flight details plus the list of seats you can still book.
    private static async Task<IResult> GetFlightAsync(Guid id, IQuerySession db, CancellationToken ct)
    {
        var flight = await db.LoadAsync<Flight>(id, ct);
        if (flight is null)
        {
            return Results.NotFound();
        }

        var taken = await TakenSeatNumbersAsync(db, id, DateTimeOffset.UtcNow, ct);
        var available = flight.Seats.Where(s => !taken.Contains(s)).ToList();

        return Results.Ok(new FlightDetail(
            flight.Id, flight.Number, flight.Origin, flight.Destination, flight.DepartureUtc,
            flight.Aircraft, flight.PricePerSeat, flight.Currency, available));
    }

    // POST /bookings — hold the chosen seats. This is the part that must never oversell.
    private static async Task<IResult> HoldAsync(HoldRequest req, IDocumentSession db, CancellationToken ct)
    {
        var flight = await db.LoadAsync<Flight>(req.FlightId, ct);
        if (flight is null)
        {
            return Results.NotFound();
        }

        if (req.Seats is null || req.Seats.Count == 0)
        {
            return Results.BadRequest(new { error = "Choose at least one seat." });
        }

        var unknown = req.Seats.Where(s => !flight.Seats.Contains(s)).ToList();
        if (unknown.Count > 0)
        {
            return Results.BadRequest(new { error = "Some seats don't exist on this flight.", seats = unknown });
        }

        var now = DateTimeOffset.UtcNow;

        // Friendly check first: are any requested seats already actively held?
        var keys = req.Seats.Select(s => SeatHold.KeyFor(req.FlightId, s)).ToList();
        var clashing = (await db.Query<SeatHold>().Where(h => keys.Contains(h.Id)).ToListAsync(ct))
            .Where(h => h.Confirmed || h.HoldExpiresAtUtc > now)
            .Select(h => h.Seat)
            .ToList();
        if (clashing.Count > 0)
        {
            return Conflict(clashing);
        }

        var bookingId = Guid.CreateVersion7();
        foreach (var seat in req.Seats)
        {
            // Insert (not upsert): if another request grabbed this seat a moment ago,
            // the database rejects this insert and we return 409 below.
            db.Insert(new SeatHold
            {
                Id = SeatHold.KeyFor(req.FlightId, seat),
                FlightId = req.FlightId,
                Seat = seat,
                BookingId = bookingId,
                Confirmed = false,
                HoldExpiresAtUtc = now.Add(HoldWindow),
            });
        }

        var held = Booking.Hold(bookingId, req.FlightId, req.CustomerRef ?? "", req.Seats, now, HoldWindow);
        db.Events.StartStream<Booking>(bookingId, held);

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (Exception ex) when (IsSeatTaken(ex))
        {
            // Lost the race against a simultaneous booking for the same seat.
            return Conflict(req.Seats);
        }

        return Results.Created($"/bookings/{bookingId}", new HoldResponse(bookingId, req.Seats, held.HoldExpiresAtUtc));
    }

    // POST /bookings/{id}/checkout — start payment for a held booking.
    private static async Task<IResult> CheckoutAsync(
        Guid id, CheckoutRequest req, IDocumentSession db, IMessageBus bus, CancellationToken ct)
    {
        var booking = await db.Events.AggregateStreamAsync<Booking>(id, token: ct);
        if (booking is null)
        {
            return Results.NotFound();
        }

        var flight = await db.LoadAsync<Flight>(booking.FlightId, ct);
        var amount = (flight?.PricePerSeat ?? 0m) * booking.Seats.Count;
        var currency = flight?.Currency ?? "EUR";

        try
        {
            db.Events.Append(id, booking.StartCheckout(amount, currency, DateTimeOffset.UtcNow));
            await db.SaveChangesAsync(ct);
        }
        catch (InvalidBookingTransition ex)
        {
            return Results.Problem(title: "Can't check out", detail: ex.Message, statusCode: StatusCodes.Status409Conflict);
        }

        // Ask the Payments service to charge the card. The reply comes back over
        // RabbitMQ and is handled in PaymentResultHandlers.
        await bus.PublishAsync(new PaymentRequestedV1(id, amount, currency, req.CardLast4, Guid.NewGuid()));

        return Results.Accepted($"/bookings/{id}", new { bookingId = id, status = "AwaitingPayment" });
    }

    // GET /bookings/{id}
    private static async Task<IResult> GetBookingAsync(Guid id, IQuerySession db, CancellationToken ct)
    {
        var booking = await db.Events.AggregateStreamAsync<Booking>(id, token: ct);
        return booking is null
            ? Results.NotFound()
            : Results.Ok(new BookingResponse(
                id, booking.FlightId, booking.Status.ToString(), booking.Seats,
                booking.HoldExpiresAtUtc, booking.TicketCodes));
    }

    // ---- helpers ----

    private static IResult Conflict(IReadOnlyList<string> seats) => Results.Problem(
        title: "Seats unavailable",
        detail: "One or more of those seats were just taken.",
        statusCode: StatusCodes.Status409Conflict,
        extensions: new Dictionary<string, object?> { ["seats"] = seats });

    // Seats counted as taken = confirmed, or still within their hold window.
    private static async Task<HashSet<string>> TakenSeatNumbersAsync(
        IQuerySession db, Guid flightId, DateTimeOffset now, CancellationToken ct)
    {
        var holds = await db.Query<SeatHold>().Where(h => h.FlightId == flightId).ToListAsync(ct);
        return holds.Where(h => h.Confirmed || h.HoldExpiresAtUtc > now).Select(h => h.Seat).ToHashSet();
    }

    private static async Task<int> CountTakenSeatsAsync(
        IQuerySession db, Guid flightId, DateTimeOffset now, CancellationToken ct)
        => (await TakenSeatNumbersAsync(db, flightId, now, ct)).Count;

    // True when SaveChanges failed because the seat's unique key already exists.
    private static bool IsSeatTaken(Exception ex)
    {
        for (var e = ex; e is not null; e = e.InnerException)
        {
            if (e is PostgresException pg && pg.SqlState == PostgresErrorCodes.UniqueViolation)
            {
                return true;
            }
        }

        return false;
    }
}
