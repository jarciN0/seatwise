namespace Seatwise.Ordering.Domain;

/// <summary>
/// A customer's booking of one or more seats on a flight.
///
/// The booking is stored as a stream of events (Marten event sourcing), not as a
/// single row. Two kinds of methods:
///   * The "decide" methods (Hold, StartCheckout, Confirm, Cancel, Expire) check
///     the current state and RETURN the event that should happen. They never change
///     the object themselves.
///   * The Create/Apply methods are how Marten rebuilds the booking from its events:
///     it calls Create for the first event and Apply for each one after.
/// Keeping those two apart means all the rules live in plain methods that are easy
/// to unit-test without a database.
/// </summary>
public sealed class Booking
{
    // Marten sets Id to the stream id, which is the booking id.
    public Guid Id { get; private set; }

    public Guid FlightId { get; private set; }
    public string CustomerRef { get; private set; } = "";
    public BookingStatus Status { get; private set; } = BookingStatus.Held;
    public IReadOnlyList<string> Seats { get; private set; } = [];
    public DateTimeOffset HoldExpiresAtUtc { get; private set; }
    public IReadOnlyList<string> TicketCodes { get; private set; } = [];

    // Marten needs a parameterless constructor to rebuild the object.
    public Booking() { }

    // ---- decide methods: validate, then return the event to store ----

    /// <summary>Starts a booking by holding the chosen seats for a short window.</summary>
    public static SeatsHeld Hold(
        Guid bookingId,
        Guid flightId,
        string customerRef,
        IReadOnlyList<string> seats,
        DateTimeOffset nowUtc,
        TimeSpan holdDuration)
    {
        if (seats is null || seats.Count == 0)
        {
            throw new InvalidBookingTransition("A booking must include at least one seat.");
        }

        return new SeatsHeld(bookingId, flightId, customerRef, seats, nowUtc.Add(holdDuration));
    }

    /// <summary>Moves a still-valid hold into "waiting for payment".</summary>
    public CheckoutStarted StartCheckout(decimal amount, string currency, DateTimeOffset nowUtc)
    {
        Require(BookingStatus.Held, nameof(StartCheckout));
        if (nowUtc >= HoldExpiresAtUtc)
        {
            throw new InvalidBookingTransition("The seat hold has expired; start a new booking.");
        }

        return new CheckoutStarted(Id, amount, currency);
    }

    /// <summary>Marks the booking paid and issues a ticket code per seat.</summary>
    public BookingConfirmed Confirm(Guid paymentId, DateTimeOffset nowUtc, IReadOnlyList<string> ticketCodes)
    {
        Require(BookingStatus.AwaitingPayment, nameof(Confirm));
        return new BookingConfirmed(Id, paymentId, Seats, ticketCodes, nowUtc);
    }

    /// <summary>Releases the seats after a failed payment (or a manual cancel).</summary>
    public BookingCancelled Cancel(string reason)
    {
        if (Status is BookingStatus.Confirmed or BookingStatus.Cancelled or BookingStatus.Expired)
        {
            throw new InvalidBookingTransition($"A {Status} booking can't be cancelled.");
        }

        return new BookingCancelled(Id, Seats, reason);
    }

    /// <summary>
    /// Expires a hold whose time ran out before payment. Returns null if the booking
    /// already reached a final state, so a late expiry never overrides a confirmation.
    /// </summary>
    public HoldExpired? Expire(DateTimeOffset nowUtc)
    {
        if (Status is BookingStatus.Confirmed or BookingStatus.Cancelled or BookingStatus.Expired)
        {
            return null;
        }

        return new HoldExpired(Id, Seats, nowUtc);
    }

    // ---- Create/Apply: how Marten rebuilds the booking from its events ----

    public static Booking Create(SeatsHeld e) => new()
    {
        Id = e.BookingId,
        FlightId = e.FlightId,
        CustomerRef = e.CustomerRef,
        Seats = [.. e.Seats],
        HoldExpiresAtUtc = e.HoldExpiresAtUtc,
        Status = BookingStatus.Held,
    };

    public void Apply(CheckoutStarted _) => Status = BookingStatus.AwaitingPayment;

    public void Apply(BookingConfirmed e)
    {
        Status = BookingStatus.Confirmed;
        TicketCodes = [.. e.TicketCodes];
    }

    public void Apply(BookingCancelled _) => Status = BookingStatus.Cancelled;

    public void Apply(HoldExpired _) => Status = BookingStatus.Expired;

    // ---- helpers ----

    private void Require(BookingStatus expected, string action)
    {
        if (Status != expected)
        {
            throw new InvalidBookingTransition($"{action} needs status {expected} but the booking is {Status}.");
        }
    }
}

/// <summary>Thrown when an action doesn't fit the booking's current state.</summary>
public sealed class InvalidBookingTransition(string message) : Exception(message);
