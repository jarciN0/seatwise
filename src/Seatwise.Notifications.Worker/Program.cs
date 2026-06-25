var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "notifications" }))
   .WithTags("Health");

// STUB (blueprint §1.2): a real consumer subscribing to OrderConfirmedV1 /
// HoldExpiredV1 that "sends" by writing a structured log line + an in-memory
// outbox the tests can assert against. No real email/SMS provider.
// TODO(M7): add MassTransit consumers with the inbox for redelivery dedupe.

app.Run();
