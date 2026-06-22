using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using TrackingService.Api;
using TrackingService.Application;
using TrackingService.Application.Ports;
using TrackingService.Infrastructure.Messaging;
using TrackingService.Infrastructure.Outbox;
using TrackingService.Infrastructure.Persistence;

using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);
var serviceName = builder.Environment.ApplicationName;
var otlpEndpoint = builder.Configuration["OpenTelemetry:OtlpEndpoint"] ?? "http://localhost:5107";

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService(serviceName))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddOtlpExporter(options => options.Endpoint = new Uri(otlpEndpoint)))
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddRuntimeInstrumentation()
        .AddPrometheusExporter());

builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
builder.Services.Configure<KafkaOptions>(builder.Configuration.GetSection(KafkaOptions.SectionName));

builder.Services.AddDbContext<TrackingDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("TrackingDb"));
});

builder.Services.AddScoped<TrackingEventHandler>();
builder.Services.AddSingleton<TrackingStatusTransitionPolicy>();

var useMockTrackingRepository = builder.Configuration.GetValue<bool>(MockTrackingRepositoryOptions.SectionName + ":Enabled");
if (useMockTrackingRepository)
{
    builder.Services.AddScoped<ITrackingRepository, MockTrackingRepository>();
}
else
{
    builder.Services.AddScoped<ITrackingRepository, TrackingRepository>();
}
builder.Services.AddScoped<IOutboxWriter, OutboxWriter>();

builder.Services.AddSingleton<ITrackingMessageConsumer, KafkaTrackingMessageConsumer>();
builder.Services.AddSingleton<IIntegrationEventBus, KafkaIntegrationEventBus>();

builder.Services.AddHostedService<TrackingConsumerWorker>();
builder.Services.AddHostedService<OutboxDispatcher>();

builder.Services.AddHealthChecks()
    .AddDbContextCheck<TrackingDbContext>();

var app = builder.Build();

app.UseOpenTelemetryPrometheusScrapingEndpoint("/metrics");

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapHealthChecks("/health");
app.MapHealthChecks("/health/live");
app.MapHealthChecks("/health/ready");
app.MapTrackingEndpoints();

app.Run();
