using Seatwise.Contracts.V1;
using Wolverine;
using Wolverine.RabbitMQ;

var builder = WebApplication.CreateBuilder(args);

var rabbit = builder.Configuration["RABBITMQ_URI"] ?? "amqp://guest:guest@localhost:5672";

// Listen for charge requests from the Booking service and send the result back,
// both over RabbitMQ. The actual handling is in PaymentHandler.
builder.Host.UseWolverine(opts =>
{
    opts.UseRabbitMq(new Uri(rabbit)).AutoProvision();
    opts.ListenToRabbitQueue("payment-requests");
    opts.PublishMessage<PaymentSucceededV1>().ToRabbitQueue("payment-results");
    opts.PublishMessage<PaymentFailedV1>().ToRabbitQueue("payment-results");
});

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "payments" }));

app.Run();
