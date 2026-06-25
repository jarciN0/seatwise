using Microsoft.EntityFrameworkCore;

namespace Seatwise.Catalog.Api;

// Catalog read model (blueprint §2.5). Read-heavy CQRS query side; learns that
// seats are taken via integration events (eventually consistent with Ordering).

public sealed record Venue(Guid Id, string Name);

public sealed record Showing(Guid Id, Guid VenueId, string Title, DateTimeOffset StartsAt, decimal Price, string Status);

public sealed record Seat(Guid Id, Guid ShowingId, string Section, string Row, int Number, string Status, long LastEventSeq);

public sealed class CatalogDbContext(DbContextOptions<CatalogDbContext> options) : DbContext(options)
{
    public DbSet<Venue> Venues => Set<Venue>();
    public DbSet<Showing> Showings => Set<Showing>();
    public DbSet<Seat> Seats => Set<Seat>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Venue>().ToTable("venues").HasKey(v => v.Id);
        b.Entity<Showing>().ToTable("showings").HasKey(s => s.Id);
        b.Entity<Seat>().ToTable("seats").HasKey(s => s.Id);
        // TODO(M3): indexes, seat-status enum mapping, seed fixtures, Redis
        // seat-map cache + ETag. seats.LastEventSeq guards out-of-order
        // projection updates (idempotent availability, blueprint §2.5).
    }
}
