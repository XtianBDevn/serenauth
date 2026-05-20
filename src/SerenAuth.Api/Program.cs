using System.Text;
using System.Threading.RateLimiting;
using HotChocolate.AspNetCore;
using HotChocolate.Execution.Configuration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using SerenAuth.Api.Authorization;
using SerenAuth.Api.GraphQL;
using SerenAuth.Api.Middleware;
using SerenAuth.Application;
using SerenAuth.Application.Abstractions;
using SerenAuth.Domain.Enums;
using SerenAuth.Infrastructure;
using SerenAuth.Infrastructure.Options;

var builder = WebApplication.CreateBuilder(args);

// ------------------------------------------------------------------
// Logging — Serilog, structured + console + correlation enrichment.
// ------------------------------------------------------------------
builder.Host.UseSerilog((ctx, lc) => lc
    .ReadFrom.Configuration(ctx.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithEnvironmentName()
    .WriteTo.Console(outputTemplate:
        "[{Timestamp:HH:mm:ss} {Level:u3}] {CorrelationId} {Message:lj}{NewLine}{Exception}"));

// ------------------------------------------------------------------
// Configuration: bind options from env-prefixed values (Mongo__, Jwt__).
// ------------------------------------------------------------------
builder.Configuration.AddEnvironmentVariables();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HttpCurrentUser>();

// Domain + Application + Infrastructure.
builder.Services.AddSerenAuthApplication();
builder.Services.AddSerenAuthInfrastructure(builder.Configuration);

// ------------------------------------------------------------------
// Authentication — JWT Bearer (HS256). Issuer + audience validated.
// ------------------------------------------------------------------
var jwt = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
          ?? throw new InvalidOperationException("Jwt configuration is missing.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        options.SaveToken = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

// ------------------------------------------------------------------
// Authorization — explicit, named policies. No magic strings outside this block.
// ------------------------------------------------------------------
builder.Services.AddAuthorizationBuilder()
    .AddPolicy(Policies.RequireOrgScope, p => p
        .RequireAuthenticatedUser()
        .RequireClaim("org"))
    .AddPolicy(Policies.RequirePaRead, p => p
        .RequireAuthenticatedUser()
        .RequireClaim("org")
        .RequireRole(nameof(Role.Viewer), nameof(Role.Intake), nameof(Role.Clinician), nameof(Role.Admin)))
    .AddPolicy(Policies.RequirePaWrite, p => p
        .RequireAuthenticatedUser()
        .RequireClaim("org")
        .RequireRole(nameof(Role.Intake), nameof(Role.Clinician), nameof(Role.Admin)))
    .AddPolicy(Policies.RequirePaSubmit, p => p
        .RequireAuthenticatedUser()
        .RequireClaim("org")
        .RequireRole(nameof(Role.Clinician), nameof(Role.Admin)))
    .AddPolicy(Policies.RequireAdmin, p => p
        .RequireAuthenticatedUser()
        .RequireRole(nameof(Role.Admin)));

// ------------------------------------------------------------------
// CORS — strict allowlist from configuration.
// ------------------------------------------------------------------
var allowedOrigins = (builder.Configuration["Cors:AllowedOrigins"] ?? string.Empty)
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

builder.Services.AddCors(o => o.AddDefaultPolicy(p => p
    .WithOrigins(allowedOrigins)
    .AllowAnyHeader()
    .AllowAnyMethod()));

// ------------------------------------------------------------------
// Rate limiting — fixed window per IP.
// ------------------------------------------------------------------
var rlPermit = builder.Configuration.GetValue("RateLimit:PermitLimit", 100);
var rlWindow = builder.Configuration.GetValue("RateLimit:WindowSeconds", 60);

builder.Services.AddRateLimiter(opts =>
{
    opts.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpCtx =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpCtx.Connection.RemoteIpAddress?.ToString() ?? "anon",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = rlPermit,
                Window = TimeSpan.FromSeconds(rlWindow),
                QueueLimit = 0
            }));
    opts.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

// ------------------------------------------------------------------
// HotChocolate GraphQL.
// ------------------------------------------------------------------
var gql = builder.Services
    .AddGraphQLServer()
    .AddAuthorization()
    .AddQueryType<Query>()
    .AddMutationType<Mutation>()
    .AddProjections()
    .AddFiltering()
    .AddSorting()
    .AddErrorFilter<SafeErrorFilter>()
    .AddMaxExecutionDepthRule(8, skipIntrospectionFields: true)
    .ModifyRequestOptions(o =>
    {
        o.IncludeExceptionDetails = builder.Environment.IsDevelopment();
    });

if (!builder.Environment.IsDevelopment())
{
    gql.DisableIntrospection();
}

// Health & readiness — surfaces Mongo connectivity at /health/ready.
builder.Services.AddHealthChecks();

var app = builder.Build();

// ------------------------------------------------------------------
// Middleware pipeline — order matters.
// ------------------------------------------------------------------
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseMiddleware<GlobalExceptionMiddleware>();

app.UseSerilogRequestLogging();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseRouting();
app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

// Health endpoints (liveness + readiness).
app.MapHealthChecks("/health/live");
app.MapHealthChecks("/health/ready");

// GraphQL endpoint.
app.MapGraphQL("/graphql");

// Graceful shutdown — let Kestrel finish in-flight requests.
app.Lifetime.ApplicationStopping.Register(() =>
    Log.Information("SerenAuth API shutting down."));

app.Run();

// Expose for WebApplicationFactory in integration tests.
public partial class Program;
