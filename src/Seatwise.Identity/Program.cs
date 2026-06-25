using Microsoft.EntityFrameworkCore;
using Seatwise.Identity;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("identity")
    ?? "Host=localhost;Port=5432;Database=seatwise_identity;Username=seatwise;Password=seatwise";

// OpenIddict stores its registrations in EF Core (blueprint §2.8, ADR-0010:
// OpenIddict is fully OSS — no production-license trap unlike Duende).
builder.Services.AddDbContext<IdentityDbContext>(options =>
{
    options.UseNpgsql(connectionString);
    options.UseOpenIddict();
});

builder.Services.AddOpenIddict()
    .AddCore(options =>
    {
        options.UseEntityFrameworkCore().UseDbContext<IdentityDbContext>();
    })
    .AddServer(options =>
    {
        // OIDC endpoints (blueprint §2.8): authorization_code + PKCE for the SPA,
        // client_credentials for service-to-service. JWKS exposed for decentralized
        // token validation at the gateway + each service.
        options.SetAuthorizationEndpointUris("connect/authorize")
               .SetTokenEndpointUris("connect/token");

        options.AllowAuthorizationCodeFlow()
               .RequireProofKeyForCodeExchange()
               .AllowClientCredentialsFlow();

        options.RegisterScopes("openid", "profile", "booking", "catalog.read", "catalog.admin");

        // Dev-only signing/encryption material. TODO(M1): real certs via env/secret.
        options.AddDevelopmentEncryptionCertificate()
               .AddDevelopmentSigningCertificate();

        options.UseAspNetCore()
               .EnableAuthorizationEndpointPassthrough()
               .EnableTokenEndpointPassthrough();
    });

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "identity" }))
   .WithTags("Health");

// TODO(M1): seed clients (storefront SPA public client; service clients) + scopes
// on startup, and add the /connect/authorize + /connect/token handlers that issue
// JWTs with aud=seatwise-api. The skeleton above stands up the OIDC server; the
// interactive login UI + client seeding are the remaining M1 work.

app.Run();
