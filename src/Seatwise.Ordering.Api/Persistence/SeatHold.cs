namespace Seatwise.Ordering.Api.Persistence;

/// <summary>
/// One row per claimed seat. The trick that prevents overbooking: the Id is
/// "flightId:seat", and Marten uses Id as the primary key. So if two bookings
/// try to grab the same seat at the same moment, only the first insert succeeds —
/// the database rejects the second. No locks, no Redis, just a unique key.
/// </summary>
public sealed class SeatHold
{
    public string Id { get; set; } = "";
    public Guid FlightId { get; set; }
    public string Seat { get; set; } = "";
    public Guid BookingId { get; set; }

    /// <summary>True once the booking is paid for; a confirmed seat is never freed.</summary>
    public bool Confirmed { get; set; }

    /// <summary>When an unpaid hold gives the seat back up.</summary>
    public DateTimeOffset HoldExpiresAtUtc { get; set; }

    public static string KeyFor(Guid flightId, string seat) => $"{flightId}:{seat}";
}
