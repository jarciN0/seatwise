using Marten;
using Seatwise.Ordering.Domain;

namespace Seatwise.Ordering.Api.Hosting;

/// <summary>
/// Puts a few sample flights in the database on startup so the API has something
/// to book straight away. Runs once — it does nothing if flights already exist.
/// The "demo" flight has only three seats, which makes the no-overbooking test
/// easy to see.
/// </summary>
public sealed class FlightSeeder(IDocumentStore store) : IHostedService
{
    public async Task StartAsync(CancellationToken ct)
    {
        await using var session = store.LightweightSession();
        if (await session.Query<Flight>().AnyAsync(ct))
        {
            return;
        }

        session.Store(SampleFlights());
        await session.SaveChangesAsync(ct);
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;

    private static Flight[] SampleFlights() =>
    [
        new Flight
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Number = "LO281", Origin = "WAW", Destination = "JFK",
            DepartureUtc = DateTimeOffset.UtcNow.AddDays(7),
            Aircraft = "Boeing 787-9",
            Seats = Flight.BuildSeatMap(rows: 30, lettersAcross: "ABCDEF"),
            PricePerSeat = 499m, Currency = "EUR",
        },
        new Flight
        {
            Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            Number = "BA442", Origin = "LHR", Destination = "FRA",
            DepartureUtc = DateTimeOffset.UtcNow.AddDays(3),
            Aircraft = "Airbus A320",
            Seats = Flight.BuildSeatMap(rows: 20, lettersAcross: "ABCDEF"),
            PricePerSeat = 129m, Currency = "EUR",
        },
        new Flight
        {
            Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
            Number = "XX001", Origin = "WAW", Destination = "KRK",
            DepartureUtc = DateTimeOffset.UtcNow.AddDays(1),
            Aircraft = "Demo (3 seats, for the overbooking test)",
            Seats = ["1A", "1B", "1C"],
            PricePerSeat = 99m, Currency = "EUR",
        },
    ];
}
