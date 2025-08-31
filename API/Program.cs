using System.Text.Json;
using System.Text.Json.Serialization;
using API.Workers;
using Core.Interfaces;
using Core.Services;
using Infrastructure.DataAccess;
using Infrastructure.Options;
using Infrastructure.Services.Massaging;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Swagger (for minimal endpoints)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// DB
builder.Services.AddDbContext<TradingDatabase>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString(nameof(TradingDatabase))));

/*builder.Services.AddDbContext<TradingDatabase>(opt =>
{
    opt.UseNpgsql(builder.Configuration.GetConnectionString("TradingDb"));
    opt.EnableDetailedErrors();
    opt.EnableSensitiveDataLogging(); // DEV ONLY
});*/

// JSON enums as strings
builder.Services.ConfigureHttpJsonOptions(o =>
    o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// RabbitMQ BUS
builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection("RabbitMq"));
builder.Services.AddSingleton<IMessageBusService, MassageBusService>();

// ===== Risk DI =====
// Read port (Infra adapter) + rules service (Core)
builder.Services.AddScoped<IRiskDecisionService, RiskDecisionService>();

// Background worker (subscribes orders.submitted -> publishes orders.decided)
builder.Services.AddHostedService<TradeGuardWorker>();

var app = builder.Build();

// Swagger UI
app.UseSwagger();
app.UseSwaggerUI();

// ---------------- Minimal test endpoints (no controllers) ----------------

// Publish a test message to the bus (routing key required)
app.MapPost("/bus/publish", async (string routingKey, object payload, IMessageBusService bus, CancellationToken ct) =>
{
    await bus.PublishAsync<object>(routingKey, payload, ct);
    return Results.Accepted($"/bus/publish?rk={routingKey}", new { routingKey, payload });
});

// Quick health
app.MapGet("/health", () => Results.Ok(new { ok = true }));

app.Run();