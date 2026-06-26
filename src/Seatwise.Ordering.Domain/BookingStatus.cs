namespace Seatwise.Ordering.Domain;

/// <summary>
/// Where a booking is in its life:
/// Held → AwaitingPayment → Confirmed, with Cancelled (payment failed) and
/// Expired (hold timed out) as the two ways it can end early.
/// </summary>
public enum BookingStatus
{
    Held,
    AwaitingPayment,
    Confirmed,
    Cancelled,
    Expired,
}
