using Seatwise.Contracts.V1;

namespace Seatwise.Payments.Api;

/// <summary>
/// Stands in for a real card processor. Wolverine calls Handle for each charge
/// request that arrives on the queue; whatever message it returns is sent back to
/// the Booking service automatically.
/// </summary>
public static class PaymentHandler
{
    public static object Handle(PaymentRequestedV1 request)
    {
        // Demo rule: a card ending in 0000 is declined, anything else is approved.
        return request.CardLast4 == "0000"
            ? new PaymentFailedV1(request.BookingId, "Card declined.", request.CorrelationId)
            : new PaymentSucceededV1(request.BookingId, Guid.NewGuid(), DateTimeOffset.UtcNow, request.CorrelationId);
    }
}
