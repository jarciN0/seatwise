using JasperFx;
using Marten;
using Seatwise.Contracts.V1;
using Seatwise.Ordering.Api.Endpoints;
using Seatwise.Ordering.Api.Hosting;
using Seatwise.Ordering.Api.Persistence;
using Seatwise.Ordering.Domain;
using Weasel.Core;
using Wolverine;
using Wolverine.Marten;
using Wolverine.RabbitMQ;

var builder = WebApplication.CreateBuilder(args);

var postgres = builder.Configuration.GetConnectionString("postgres")
    ?? "Host=localhost;Port=5432;Database=seatwise;Username=seatwise;Password=seatwise";
var rabbit = builder.Configuration["RABBITMQ_URI"] ?? "amqp://guest:guest@localhost:5672";

// Marten = the Postgres event store (bookings) + document store (flights, seat holds).
builder.Services.AddMarten(opts =>
{
    opts.Connection(postgres);

    // Create/upgrade the database tables automatically. Fine for a demo; a real
    // deployment would run migrations explicitly instead.
    opts.AutoCreateSchemaObjects = AutoCreate.CreateOrUpdate;

    // Rebuild a Booking on demand by replaying its events.
    opts.Projections.LiveStreamAggregation<Booking>();

    // SeatHold's string Id ("flightId:seat") is the primary key — that uniqueness
    // is what stops two bookings from taking the same seat.
    opts.Schema.For<SeatHold>();
    opts.Schema.For<Flight>();
})
.IntegrateWithWolverine(); // lets message handlers use a Marten session

// Wolverine = message handling. Here it talks to the Payments service over RabbitMQ.
builder.Host.UseWolverine(opts =>
{
    opts.UseRabbitMq(new Uri(rabbit)).AutoProvision();
    opts.PublishMessage<PaymentRequestedV1>().ToRabbitQueue("payment-requests");
    opts.ListenToRabbitQueue("payment-results");
});

builder.Services.AddHostedService<FlightSeeder>();        // sample flights on startup
builder.Services.AddHostedService<HoldExpirySweeper>();   // frees seats when holds expire
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "ordering" }));
app.MapBookingApi();

await app.RunJasperFxCommands(args);
