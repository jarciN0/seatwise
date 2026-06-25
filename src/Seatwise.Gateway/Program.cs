var builder = WebApplication.CreateBuilder(args);

// YARP reverse proxy — the single front door (blueprint §2.3 / §2.8).
// Routes + clusters are loaded from the "ReverseProxy" config section.
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// TODO(M2): AddAuthentication().AddJwtBearer(...) — first-pass JWT validation at
// the edge (issuer + audience seatwise-api), scope policies (booking / catalog.admin),
// per-subject rate limiting (Microsoft.AspNetCore.RateLimiting), OTel.

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "gateway" }))
   .WithTags("Health");

app.MapReverseProxy();

app.Run();
