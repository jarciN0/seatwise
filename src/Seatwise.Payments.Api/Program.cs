var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "payments" }))
   .WithTags("Health");

// STUB (blueprint §1.2 / §2.3): real contract, mock PSP behavior.
//   POST /payments/charge { orderId, amount, currency, idempotencyKey, cardLast4 }
//   rule: cardLast4 == "0000" -> decline; else succeed after artificial delay.
// TODO(M6): consume PaymentRequestedV1 off the bus and publish
// PaymentSucceededV1 / PaymentFailedV1 (idempotency key + retries + timeouts),
// driving the Ordering saga.
app.MapPost("/payments/charge", (ChargeRequest req) =>
{
    var declined = req.CardLast4 == "0000";
    // TODO(M6): publish the integration event instead of returning inline.
    return Results.Accepted(value: new { req.OrderId, status = declined ? "Declined" : "Accepted" });
}).WithTags("Payments");

app.Run();

internal sealed record ChargeRequest(Guid OrderId, decimal Amount, string Currency, string IdempotencyKey, string CardLast4);
