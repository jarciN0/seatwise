namespace Seatwise.Ordering.Domain;

/// <summary>
/// A scheduled flight people can book seats on. Stored as a Marten document
/// (a plain row), separate from the booking event streams. This is the catalog
/// the booking flow reads from; in a bigger system it could be its own service.
/// </summary>
public sealed class Flight
{
    public Guid Id { get; set; }

    /// <summary>Airline flight number, e.g. "LO281".</summary>
    public string Number { get; set; } = "";

    /// <summary>Departure airport IATA code, e.g. "WAW".</summary>
    public string Origin { get; set; } = "";

    /// <summary>Arrival airport IATA code, e.g. "JFK".</summary>
    public string Destination { get; set; } = "";

    public DateTimeOffset DepartureUtc { get; set; }

    public string Aircraft { get; set; } = "";

    /// <summary>Every seat number on the aircraft, e.g. "1A", "1B" … "30F".</summary>
    public IReadOnlyList<string> Seats { get; set; } = [];

    public decimal PricePerSeat { get; set; }

    public string Currency { get; set; } = "EUR";

    /// <summary>
    /// Builds a simple seat map: <paramref name="rows"/> rows, each with the given
    /// letters across (e.g. "ABCDEF" → six seats per row).
    /// </summary>
    public static IReadOnlyList<string> BuildSeatMap(int rows, string lettersAcross)
    {
        var seats = new List<string>(rows * lettersAcross.Length);
        for (var row = 1; row <= rows; row++)
        {
            foreach (var letter in lettersAcross)
            {
                seats.Add($"{row}{letter}");
            }
        }

        return seats;
    }
}
