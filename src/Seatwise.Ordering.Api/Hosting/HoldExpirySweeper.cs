using Marten;
using Seatwise.Ordering.Api.Persistence;
using Seatwise.Ordering.Domain;

namespace Seatwise.Ordering.Api.Hosting;

/// <summary>
/// Every few seconds, frees seats whose hold ran out before the customer paid:
/// it deletes the expired SeatHold rows and records a HoldExpired event on the
/// booking. This is what makes a temporarily-held seat bookable again.
/// </summary>
public sealed class HoldExpirySweeper(IDocumentStore store, ILogger<HoldExpirySweeper> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SweepAsync(stoppingToken);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogError(ex, "Hold expiry sweep failed; will retry.");
            }

            await Task.Delay(Interval, stoppingToken);
        }
    }

    private async Task SweepAsync(CancellationToken ct)
    {
        await using var session = store.LightweightSession();
        var now = DateTimeOffset.UtcNow;

        var expired = await session.Query<SeatHold>()
            .Where(h => !h.Confirmed && h.HoldExpiresAtUtc < now)
            .ToListAsync(ct);
        if (expired.Count == 0)
        {
            return;
        }

        foreach (var perBooking in expired.GroupBy(h => h.BookingId))
        {
            var booking = await session.Events.AggregateStreamAsync<Booking>(perBooking.Key, token: ct);
            var expiredEvent = booking?.Expire(now);
            if (expiredEvent is not null)
            {
                session.Events.Append(perBooking.Key, expiredEvent);
            }

            foreach (var hold in perBooking)
            {
                session.Delete(hold);
            }
        }

        await session.SaveChangesAsync(ct);
    }
}
