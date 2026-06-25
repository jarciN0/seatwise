using Marten;
using Seatwise.Ordering.Api.Concurrency;
using Seatwise.Ordering.Domain;

namespace Seatwise.Ordering.Api.Endpoints;

/// <summary>
/// The three core Minimal API endpoints (blueprint §2.3): hold seats, confirm
/// order, get order. Written against Marten's IDocumentSession directly for the
/// rough slice; the Wolverine command bus is wired in Program.cs and is the
/// intended home for these handlers + the saga/outbox as the service matures.
/// </summary>
public static class OrderEndpoints
{
    private static readonly TimeSpan HoldDuration = TimeSpan.FromSeconds(120);

    public static void MapOrderEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/orders").WithTags("Orders");

        group.MapPost("/holds", HoldSeatsAsync);
        group.MapPost("/{orderId:guid}/reserve", ReserveAsync);
        group.MapPost("/{orderId:guid}/checkout", CheckoutAsync);
        group.MapGet("/{orderId:guid}", GetOrderAsync);
    }

    // POST /orders/holds  — the oversell-prevention path.
    private static async Task<IResult> HoldSeatsAsync(
        HoldSeatsRequest request,
        IDocumentSession session,
        ISeatLock seatLock,
        IIdempotencyStore idempotency,
        HttpContext http,
        CancellationToken ct)
    {
        var idemKey = http.Request.Headers["Idempotency-Key"].ToString();
        if (string.IsNullOrWhiteSpace(idemKey))
        {
            return Results.Problem(
                title: "Missing Idempotency-Key",
                detail: "All mutating endpoints require an Idempotency-Key header.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        // Idempotent replay: return the previously created order id.
        if (await idempotency.TryGetAsync(idemKey, ct) is { } existing)
        {
            return Results.Ok(new HoldResponse(existing, request.SeatIds, default));
        }

        // Layer 1: acquire the per-seat lock for the critical section only.
        await using var handle = await seatLock.TryAcquireAsync(request.ShowingId, request.SeatIds, ct);
        if (handle is null)
        {
            return Results.Problem(
                title: "Seats unavailable",
                detail: "One or more requested seats are currently held by another customer.",
                statusCode: StatusCodes.Status409Conflict,
                extensions: new Dictionary<string, object?> { ["conflictingSeatIds"] = request.SeatIds });
        }

        // TODO(M4): pre-check the ShowingAvailability projection + existing hold
        // record before appending, so already-sold seats short-circuit to 409.

        var orderId = Guid.CreateVersion7();
        var customerId = ResolveCustomerId(http);
        var correlationId = Guid.NewGuid();

        var held = Order.Hold(
            orderId, request.ShowingId, customerId, request.SeatIds,
            DateTimeOffset.UtcNow, HoldDuration, correlationId);

        // Start the event stream. Marten folds via Order.Create/Apply.
        session.Events.StartStream<Order>(orderId, held);
        await session.SaveChangesAsync(ct);

        await idempotency.SaveAsync(idemKey, orderId, ct);

        // TODO(M4): write Redis hold:{orderId} hash with EXPIRE=120s + register
        // in hold:expiry:zset so the HoldExpirySweeper can fire HoldExpired.

        return Results.Created(
            $"/orders/{orderId}",
            new HoldResponse(orderId, request.SeatIds, held.HoldExpiresAtUtc));
    }

    // POST /orders/{id}/reserve — promote a hold to a reservation.
    private static async Task<IResult> ReserveAsync(
        Guid orderId,
        IDocumentSession session,
        CancellationToken ct)
    {
        var order = await session.Events.AggregateStreamAsync<Order>(orderId, token: ct);
        if (order is null)
        {
            return Results.NotFound();
        }

        try
        {
            var reserved = order.Reserve(DateTimeOffset.UtcNow);
            // Optimistic concurrency (Layer 3): append conditioned on stream version.
            session.Events.Append(orderId, reserved);
            await session.SaveChangesAsync(ct);
            return Results.Ok(ToDto(orderId, await session.Events.AggregateStreamAsync<Order>(orderId, token: ct)));
        }
        catch (InvalidOrderTransitionException ex)
        {
            return Results.Problem(title: "Cannot reserve", detail: ex.Message, statusCode: StatusCodes.Status409Conflict);
        }
    }

    // POST /orders/{id}/checkout — kicks the (future) saga.
    private static async Task<IResult> CheckoutAsync(
        Guid orderId,
        CheckoutRequest request,
        IDocumentSession session,
        IIdempotencyStore idempotency,
        HttpContext http,
        CancellationToken ct)
    {
        var idemKey = http.Request.Headers["Idempotency-Key"].ToString();
        if (string.IsNullOrWhiteSpace(idemKey))
        {
            return Results.Problem(title: "Missing Idempotency-Key", statusCode: StatusCodes.Status400BadRequest);
        }

        var order = await session.Events.AggregateStreamAsync<Order>(orderId, token: ct);
        if (order is null)
        {
            return Results.NotFound();
        }

        try
        {
            var paymentRequested = order.RequestPayment(request.Amount, request.Currency, idemKey);
            session.Events.Append(orderId, paymentRequested);
            await session.SaveChangesAsync(ct);

            // TODO(M6): publish PaymentRequestedV1 via the MassTransit outbox in
            // the same transaction; the saga then handles PaymentSucceeded/Failed
            // -> OrderConfirmed / OrderCancelled compensation.

            return Results.Accepted($"/orders/{orderId}", new { orderId, status = "AwaitingPayment" });
        }
        catch (InvalidOrderTransitionException ex)
        {
            return Results.Problem(title: "Cannot checkout", detail: ex.Message, statusCode: StatusCodes.Status409Conflict);
        }
    }

    // GET /orders/{id}
    private static async Task<IResult> GetOrderAsync(Guid orderId, IQuerySession session, CancellationToken ct)
    {
        // TODO(M4): read the OrderSummaryProjection document instead of live
        // aggregation once the inline projection is registered.
        var order = await session.Events.AggregateStreamAsync<Order>(orderId, token: ct);
        return order is null ? Results.NotFound() : Results.Ok(ToDto(orderId, order));
    }

    private static OrderDto ToDto(Guid orderId, Order? o) => o is null
        ? throw new InvalidOperationException("Order disappeared mid-request.")
        : new OrderDto(orderId, o.ShowingId, o.CustomerId, o.Status.ToString(), o.SeatIds, o.HoldExpiresAtUtc, o.TicketCodes);

    // TODO(M1/M2): derive from the validated JWT `sub` claim. Anonymous fallback
    // for the rough slice so the endpoint is exercisable without Identity wired.
    private static Guid ResolveCustomerId(HttpContext http)
        => Guid.TryParse(http.User.FindFirst("sub")?.Value, out var sub) ? sub : Guid.Empty;
}
