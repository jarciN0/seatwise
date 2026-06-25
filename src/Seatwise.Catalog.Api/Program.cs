using Microsoft.EntityFrameworkCore;
using Seatwise.Catalog.Api;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("catalog")
    ?? "Host=localhost;Port=5432;Database=seatwise_catalog;Username=seatwise;Password=seatwise";

builder.Services.AddDbContext<CatalogDbContext>(o => o.UseNpgsql(connectionString));
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "catalog" }))
   .WithTags("Health");

// TODO(M3): implement blueprint §2.3 Catalog endpoints:
//   GET  /catalog/showings?from=&to=&q=
//   GET  /catalog/showings/{id}
//   GET  /catalog/showings/{id}/seatmap   (ETag + Redis cache)
//   POST /catalog/showings                (scope: catalog.admin)
// Plus MassTransit consumers for SeatsReservedV1 / OrderConfirmedV1 /
// OrderCancelledV1 / HoldExpiredV1 to project seat availability idempotently.
app.MapGet("/catalog/showings", () => Results.Ok(Array.Empty<object>()))
   .WithTags("Catalog");

app.Run();
