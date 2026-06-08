using Microsoft.EntityFrameworkCore;
using TrackingService.Api;
using TrackingService.Application;
using TrackingService.Application.Ports;
using TrackingService.Infrastructure.Messaging;
using TrackingService.Infrastructure.Outbox;
using TrackingService.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<TrackingDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("TrackingDb"));
});

builder.Services.AddScoped<TrackingEventHandler>();
builder.Services.AddSingleton<TrackingStatusTransitionPolicy>();

builder.Services.AddScoped<ITrackingRepository, TrackingRepository>();
builder.Services.AddScoped<IOutboxWriter, OutboxWriter>();

builder.Services.AddSingleton<ITrackingMessageConsumer, KafkaTrackingMessageConsumer>();
builder.Services.AddSingleton<IIntegrationEventBus, KafkaIntegrationEventBus>();

builder.Services.AddHostedService<TrackingConsumerWorker>();
builder.Services.AddHostedService<OutboxDispatcher>();

builder.Services.AddHealthChecks()
    .AddDbContextCheck<TrackingDbContext>();

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapHealthChecks("/health");
app.MapTrackingEndpoints();

app.Run();
