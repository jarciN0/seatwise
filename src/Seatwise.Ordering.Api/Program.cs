using JasperFx;
using Marten;
using Seatwise.Ordering.Api.Concurrency;
using Seatwise.Ordering.Api.Endpoints;
using Seatwise.Ordering.Domain;
using Wolverine;
using Wolverine.Marten;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("ordering")
    ?? "Host=localhost;Port=5432;Database=seatwise_ordering;Username=seatwise;Password=seatwise";

// ---- Marten: event store + projections (blueprint §2.2 / §2.5) ----
builder.Services.AddMarten(opts =>
{
    opts.Connection(connectionString);

    // The Order aggregate is rehydrated via live aggregation (Create/Apply).
    // TODO(M4): register OrderSummaryProjection (inline) and
    // ShowingAvailabilityProjection (async) here.
    opts.Projections.LiveStreamAggregation<Order>();
})
// Auto-create schema in dev only.
.IntegrateWithWolverine();

// ---- Wolverine: in-proc CQRS + (future) outbox (ADR-0010: NOT MediatR) ----
builder.Host.UseWolverine(opts =>
{
    // TODO(M6): add the MassTransit transport + transactional outbox so
    // PaymentRequestedV1 publishes atomically with the event append.
    opts.Policies.AutoApplyTransactions();
});

// Concurrency primitives — in-memory stubs for the rough slice (blueprint §2.6).
// TODO(M5): swap for RedLock + Redis-backed implementations.
builder.Services.AddSingleton<ISeatLock, InMemorySeatLock>();
builder.Services.AddSingleton<IIdempotencyStore, InMemoryIdempotencyStore>();

builder.Services.AddOpenApi();

// TODO(M2): AddAuthentication().AddJwtBearer(...) validating against the IdP's
// JWKS (issuer + audience seatwise-api); deny-by-default scope policies.

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "ordering" }))
   .WithTags("Health");

app.MapOrderEndpoints();

await app.RunJasperFxCommands(args);
