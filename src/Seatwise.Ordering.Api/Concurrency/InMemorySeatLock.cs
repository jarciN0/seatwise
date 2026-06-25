using System.Collections.Concurrent;

namespace Seatwise.Ordering.Api.Concurrency;

/// <summary>
/// Dev/test stub for <see cref="ISeatLock"/>. Single-process semaphore per seat
/// key — enough to exercise the hold flow and a single-node oversell test.
///
/// TODO(M5): replace with RedLockSeatLock (RedLock.net + StackExchange.Redis) so
/// the guarantee holds across scaled-out Ordering replicas. This in-memory lock
/// does NOT protect against multi-instance races — it is the compile-safe
/// placeholder the blueprint allows for the lock layer.
/// </summary>
public sealed class InMemorySeatLock : ISeatLock
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> Gates = new();

    public async Task<ISeatLockHandle?> TryAcquireAsync(
        Guid showingId,
        IReadOnlyList<Guid> seatIds,
        CancellationToken ct = default)
    {
        // Sort to avoid deadlock when two requests overlap on a seat set.
        var ordered = seatIds.OrderBy(s => s).ToList();
        var acquired = new List<SemaphoreSlim>();

        foreach (var seatId in ordered)
        {
            var gate = Gates.GetOrAdd($"{showingId}:{seatId}", _ => new SemaphoreSlim(1, 1));
            if (await gate.WaitAsync(TimeSpan.FromMilliseconds(50), ct).ConfigureAwait(false))
            {
                acquired.Add(gate);
            }
            else
            {
                // All-or-nothing: release what we took, signal conflict.
                foreach (var g in acquired)
                {
                    g.Release();
                }

                return null;
            }
        }

        return new Handle(ordered, acquired);
    }

    private sealed class Handle(IReadOnlyList<Guid> seatIds, List<SemaphoreSlim> gates) : ISeatLockHandle
    {
        public IReadOnlyList<Guid> LockedSeatIds { get; } = seatIds;

        public ValueTask DisposeAsync()
        {
            foreach (var g in gates)
            {
                g.Release();
            }

            return ValueTask.CompletedTask;
        }
    }
}
