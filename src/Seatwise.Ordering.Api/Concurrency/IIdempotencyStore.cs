using System.Collections.Concurrent;

namespace Seatwise.Ordering.Api.Concurrency;

/// <summary>
/// Idempotency (blueprint §2.6): every mutating endpoint requires an
/// Idempotency-Key. The first request stores its response; replays return the
/// stored response without re-executing. Defends against double-click and
/// broker redelivery (at-least-once -> effectively-once).
///
/// TODO(M5): back this with Redis `SET NX` + 24h EXPIRE (key idem:{key}).
/// </summary>
public interface IIdempotencyStore
{
    /// <summary>Returns a cached response id for the key, or null if first-seen.</summary>
    public Task<Guid?> TryGetAsync(string key, CancellationToken ct = default);

    /// <summary>Records the response id for an idempotency key (SET NX semantics).</summary>
    public Task SaveAsync(string key, Guid responseId, CancellationToken ct = default);
}

/// <summary>In-memory dev stub. TODO(M5): RedisIdempotencyStore.</summary>
public sealed class InMemoryIdempotencyStore : IIdempotencyStore
{
    private static readonly ConcurrentDictionary<string, Guid> Store = new();

    public Task<Guid?> TryGetAsync(string key, CancellationToken ct = default)
        => Task.FromResult(Store.TryGetValue(key, out var id) ? id : (Guid?)null);

    public Task SaveAsync(string key, Guid responseId, CancellationToken ct = default)
    {
        Store.TryAdd(key, responseId);
        return Task.CompletedTask;
    }
}
