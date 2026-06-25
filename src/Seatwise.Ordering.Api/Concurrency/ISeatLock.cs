namespace Seatwise.Ordering.Api.Concurrency;

/// <summary>
/// Layer 1 of the concurrency model (blueprint §2.6): a per-seat distributed
/// lock guarding ONLY the short critical section that places a hold. The real
/// implementation is RedLock.net over StackExchange.Redis, acquiring all seat
/// keys all-or-nothing (sorted to avoid deadlock) with a short auto-extended TTL.
///
/// Kept behind this interface so the solution compiles and the domain/handler
/// flow can be unit-tested without Redis. See InMemorySeatLock for the dev stub.
/// </summary>
public interface ISeatLock
{
    /// <summary>
    /// Acquire an exclusive lock over every (showingId, seatId). Returns a
    /// disposable handle on success; null if any seat could not be locked
    /// (-> caller returns 409 SeatUnavailable). Never holds the lock for the
    /// whole 120s hold — only the critical section.
    /// </summary>
    public Task<ISeatLockHandle?> TryAcquireAsync(
        Guid showingId,
        IReadOnlyList<Guid> seatIds,
        CancellationToken ct = default);
}

public interface ISeatLockHandle : IAsyncDisposable
{
    public IReadOnlyList<Guid> LockedSeatIds { get; }
}
