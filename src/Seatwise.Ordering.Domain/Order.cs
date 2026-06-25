namespace Seatwise.Ordering.Domain;

/// <summary>
/// The event-sourced Order aggregate — the crown jewel.
///
/// Two halves:
///  * DECISION methods (Hold/Reserve/Confirm/...) — pure functions that validate
///    the current state and RETURN the domain event(s) to append. They never
///    mutate state directly; the caller appends the returned event to the Marten
///    stream (with an expected-version for optimistic concurrency, blueprint §2.6
///    Layer 3) and then folds it back via Apply().
///  * FOLD methods (Create/Apply) — Marten convention. Marten rehydrates the
///    aggregate by calling Create() for the first event and Apply() for the rest.
///
/// This split keeps the domain pure and unit-testable with zero infrastructure.
/// </summary>
public sealed class Order
{
    // Marten sets Id to the stream id (= OrderId).
    public Guid Id { get; private set; }

    public Guid ShowingId { get; private set; }
    public Guid CustomerId { get; private set; }
    public OrderStatus Status { get; private set; } = OrderStatus.Drafting;
    public List<Guid> SeatIds { get; private set; } = [];
    public DateTimeOffset HoldExpiresAtUtc { get; private set; }
    public IReadOnlyList<string> TicketCodes { get; private set; } = [];

    // Marten needs a parameterless ctor for rehydration.
    public Order() { }

    // ---- DECISION methods (return events; never mutate) ----

    /// <summary>First hold acquisition. Produces the initial SeatsHeld event.</summary>
    public static SeatsHeld Hold(
        Guid orderId,
        Guid showingId,
        Guid customerId,
        IReadOnlyList<Guid> seatIds,
        DateTimeOffset nowUtc,
        TimeSpan holdDuration,
        Guid correlationId)
    {
        if (seatIds is null || seatIds.Count == 0)
        {
            throw new InvalidOrderTransitionException("Cannot hold zero seats.");
        }

        return new SeatsHeld(
            orderId, showingId, customerId, seatIds,
            nowUtc.Add(holdDuration), correlationId);
    }

    /// <summary>Promote an active hold to a reservation (checkout begun).</summary>
    public SeatsReserved Reserve(DateTimeOffset nowUtc)
    {
        EnsureStatus(OrderStatus.Drafting, nameof(Reserve));
        if (nowUtc >= HoldExpiresAtUtc)
        {
            throw new InvalidOrderTransitionException("Hold has expired; cannot reserve.");
        }

        return new SeatsReserved(Id, SeatIds, nowUtc);
    }

    /// <summary>Saga requests payment for a reserved order.</summary>
    public PaymentRequested RequestPayment(decimal amount, string currency, string idempotencyKey)
    {
        EnsureStatus(OrderStatus.Reserved, nameof(RequestPayment));
        return new PaymentRequested(Id, amount, currency, idempotencyKey);
    }

    /// <summary>Terminal happy path. Idempotent if already confirmed.</summary>
    public OrderConfirmed Confirm(
        Guid paymentId,
        DateTimeOffset nowUtc,
        IReadOnlyList<string> ticketCodes)
    {
        // Guard against confirm-after-expire / double-confirm (blueprint §2.9).
        if (Status is OrderStatus.AwaitingPayment or OrderStatus.Reserved)
        {
            return new OrderConfirmed(Id, SeatIds, nowUtc, ticketCodes);
        }

        throw new InvalidOrderTransitionException(
            $"Cannot confirm an order in status {Status}.");
    }

    /// <summary>Compensation after a declined payment — releases the seats.</summary>
    public OrderCancelled Cancel(string reason)
    {
        if (Status is OrderStatus.Confirmed or OrderStatus.Cancelled or OrderStatus.Expired)
        {
            throw new InvalidOrderTransitionException(
                $"Cannot cancel an order in terminal status {Status}.");
        }

        return new OrderCancelled(Id, SeatIds, reason);
    }

    /// <summary>
    /// Expire a hold whose TTL elapsed before reservation/payment. Terminal-state
    /// guarded: confirming wins over expiry (blueprint §2.9 expiry-vs-confirm race).
    /// Returns null when expiry should be a no-op.
    /// </summary>
    public HoldExpired? Expire(DateTimeOffset nowUtc)
    {
        if (Status is OrderStatus.Confirmed or OrderStatus.Cancelled or OrderStatus.Expired)
        {
            return null; // already terminal; expiry is a no-op
        }

        return new HoldExpired(Id, SeatIds, nowUtc);
    }

    // ---- FOLD methods (Marten convention) ----

    public static Order Create(SeatsHeld e) => new()
    {
        Id = e.OrderId,
        ShowingId = e.ShowingId,
        CustomerId = e.CustomerId,
        SeatIds = [.. e.SeatIds],
        HoldExpiresAtUtc = e.HoldExpiresAtUtc,
        Status = OrderStatus.Drafting,
    };

    public void Apply(SeatAddedToHold e) => SeatIds.Add(e.SeatId);

    public void Apply(SeatsReserved _) => Status = OrderStatus.Reserved;

    public void Apply(PaymentRequested _) => Status = OrderStatus.AwaitingPayment;

    public void Apply(PaymentFailed _) => Status = OrderStatus.PaymentDeclined;

    public void Apply(OrderConfirmed e)
    {
        Status = OrderStatus.Confirmed;
        TicketCodes = [.. e.TicketCodes];
    }

    public void Apply(OrderCancelled _) => Status = OrderStatus.Cancelled;

    public void Apply(HoldExpired _) => Status = OrderStatus.Expired;

    public void Apply(OrderRefunded _) => Status = OrderStatus.Refunded;

    // ---- helpers ----

    private void EnsureStatus(OrderStatus expected, string action)
    {
        if (Status != expected)
        {
            throw new InvalidOrderTransitionException(
                $"{action} requires status {expected} but order is {Status}.");
        }
    }
}

/// <summary>Raised when a command is invalid for the aggregate's current state.</summary>
public sealed class InvalidOrderTransitionException(string message) : Exception(message);
