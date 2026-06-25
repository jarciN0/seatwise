namespace Seatwise.Ordering.Domain;

/// <summary>
/// The Order aggregate's lifecycle (blueprint §2.2 state machine):
/// Drafting -> Reserved -> AwaitingPayment -> Confirmed
///          \-> Expired   \-> Expired        \-> PaymentDeclined -> Cancelled
/// </summary>
public enum OrderStatus
{
    Drafting,
    Reserved,
    AwaitingPayment,
    Confirmed,
    PaymentDeclined,
    Cancelled,
    Expired,
    Refunded // stretch
}
