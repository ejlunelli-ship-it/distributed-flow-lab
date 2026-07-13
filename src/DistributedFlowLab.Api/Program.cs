using DistributedFlowLab.Api.Endpoints;
using DistributedFlowLab.Api.Middleware;
using DistributedFlowLab.Application;
using DistributedFlowLab.Infrastructure;
using DistributedFlowLab.Infrastructure.Realtime;

var builder = WebApplication.CreateBuilder(args);

// Composition root: Application (MediatR, validators) + Infrastructure
// (in-memory persistence, sequenced event pipeline, simulation engine).
builder.Services.AddApplication();
builder.Services.AddInfrastructure();

// Realtime transport (ADR-002): SimulationHub pushes the authoritative event
// stream to per-simulationId groups.
builder.Services.AddSignalR();

// RFC 7807 problem+json for known exceptions; default handler covers the rest.
builder.Services.AddExceptionHandler<ProblemDetailsExceptionHandler>();
builder.Services.AddProblemDetails();

// CORS for the Vite dev server / SPA. Origins are configurable per environment.
const string SpaCorsPolicy = "spa";
builder.Services.AddCors(options =>
    options.AddPolicy(SpaCorsPolicy, policy =>
        policy.WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [])
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()));

var app = builder.Build();

app.UseExceptionHandler();
app.UseCors(SpaCorsPolicy);

// Liveness probe used by Docker Compose healthchecks and the CI smoke test.
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

// REST surface (canon §9): /api/v1.
var api = app.MapGroup("/api/v1");
api.MapScenarioEndpoints();
api.MapSimulationEndpoints();

// Realtime surface (canon §8): the SimulationHub.
app.MapHub<SimulationHub>("/hubs/simulation");

app.Run();

// Exposed so DistributedFlowLab.Integration.Tests can bootstrap the API with WebApplicationFactory.
public partial class Program;