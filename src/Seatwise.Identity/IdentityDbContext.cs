using Microsoft.EntityFrameworkCore;

namespace Seatwise.Identity;

/// <summary>
/// EF Core context backing OpenIddict's client/scope/token registrations.
/// OpenIddict adds its own entity sets via UseOpenIddict() in Program.cs.
/// </summary>
public sealed class IdentityDbContext(DbContextOptions<IdentityDbContext> options) : DbContext(options);
