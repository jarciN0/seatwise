using Marten;
using Seatwise.Contracts.V1;
using Seatwise.Ordering.Api.Persistence;
using Seatwise.Ordering.Domain;

namespace Seatwise.Ordering.Api.Messaging;

/// <summary>
/// Reacts to the payment results that come back from the Payments service over
/// RabbitMQ. Wolverine finds these Handle methods and calls them per message.
/// (The class name must end in "Handler" for Wolverine to discover it.)
/// </summary>
public static class PaymentResultHandler
{
    // Payment worked → confirm the booking, issue a ticket per seat, and mark the
    // seats permanently taken.
    public static async Task Handle(PaymentSucceededV1 msg, IDocumentSession db, CancellationToken ct)
    {
        var booking = await db.Events.AggregateStreamAsync<Booking>(msg.BookingId, token: ct);
        if (booking is null)
        {
            return;
        }

        try
        {
            var tickets = booking.Seats.Select(seat => TicketCode(msg.BookingId, seat)).ToList();
            db.Events.Append(msg.BookingId, booking.Confirm(msg.PaymentId, msg.PaidAtUtc, tickets));

            foreach (var seat in booking.Seats)
            {
                var hold = await db.LoadAsync<SeatHold>(SeatHold.KeyFor(booking.FlightId, seat), ct);
                if (hold is not null)
                {
                    hold.Confirmed = true;
                    db.Store(hold);
                }
            }

            await db.SaveChangesAsync(ct);
        }
        catch (InvalidBookingTransition)
        {
            // The booking already expired or was cancelled; ignore a late success.
        }
    }

    // Payment declined → cancel the booking and free the seats.
    public static async Task Handle(PaymentFailedV1 msg, IDocumentSession db, CancellationToken ct)
    {
        var booking = await db.Events.AggregateStreamAsync<Booking>(msg.BookingId, token: ct);
        if (booking is null)
        {
            return;
        }

        try
        {
            db.Events.Append(msg.BookingId, booking.Cancel(msg.Reason));
            foreach (var seat in booking.Seats)
            {
                db.Delete<SeatHold>(SeatHold.KeyFor(booking.FlightId, seat));
            }

            await db.SaveChangesAsync(ct);
        }
        catch (InvalidBookingTransition)
        {
            // Already in a final state; nothing to undo.
        }
    }

    // A short, human-readable ticket code, e.g. "A1B2C3-12A".
    private static string TicketCode(Guid bookingId, string seat)
        => $"{bookingId.ToString("N")[..6].ToUpperInvariant()}-{seat}";
}
