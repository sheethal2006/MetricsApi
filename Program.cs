using MetricsApi.Data;
using MetricsApi.DTOs;
using MetricsApi.Services;
using Microsoft.EntityFrameworkCore;
using MetricsApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole(); // log to console
// Add configuration, dbcontext, services
builder.Services.AddDbContext<MetricsDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<AlertingService>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Apply migrations at startup (for dev convenience). In production prefer explicit migration approach.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<MetricsDbContext>();
    db.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapPost("/api/metrics", async (MetricReadingDto dto, MetricsDbContext db, AlertingService alertService) =>
{
    if (string.IsNullOrWhiteSpace(dto.SensorId) || string.IsNullOrWhiteSpace(dto.MetricType))
        return Results.BadRequest(new { message = "SensorId and MetricType are required." });

    var reading = new MetricReading
    {
        SensorId = dto.SensorId,
        MetricType = dto.MetricType.ToLowerInvariant(),
        Value = dto.Value,
        Timestamp = DateTime.UtcNow//dto.Timestamp?.ToUniversalTime() ?? DateTime.UtcNow
    };

    db.MetricReadings.Add(reading);
    await db.SaveChangesAsync();

    // run alerting check (consecutive rule implemented)
    await alertService.CheckAndCreateAlertAsync(reading);

    return Results.Created($"/api/metrics/{reading.Id}", reading);
});

app.MapGet("/api/metrics/summary", async (string metricType, string interval, MetricsDbContext db) =>
{
    if (string.IsNullOrWhiteSpace(metricType) || string.IsNullOrWhiteSpace(interval))
        return Results.BadRequest(new { message = "metricType and interval are required (e.g. 1h, 30m)." });

    // parse interval: accepts values like "1h", "30m", "2h"
    var dt = ParseIntervalToUtcStart(interval);
    if (dt == null) return Results.BadRequest(new { message = "Could not parse interval. Use e.g. 1h or 30m." });

    var start = dt.Value;
    var list = await db.MetricReadings
        .Where(r => r.MetricType.ToLower() == metricType.ToLower() && r.Timestamp >= start)
        .ToListAsync();

    if (!list.Any())
    {
        return Results.Ok(new
        {
            metric = metricType,
            startUtc = start,
            lastIntervalCount = 0,
            average = (double?)null,
            min = (double?)null,
            max = (double?)null,
            alertsTriggered = await db.Alerts.CountAsync(a => a.MetricType == metricType.ToLower() && a.Timestamp >= start)
        });
    }

    return Results.Ok(new
    {
        metric = metricType,
        startUtc = start,
        lastIntervalCount = list.Count,
        average = list.Average(x => x.Value),
        min = list.Min(x => x.Value),
        max = list.Max(x => x.Value),
        alertsTriggered = await db.Alerts.CountAsync(a => a.MetricType == metricType.ToLower() && a.Timestamp >= start)
    });
});

app.MapGet("/api/alerts", async (MetricsDbContext db, int limit = 100) =>
{
    var alerts = await db.Alerts.OrderByDescending(a => a.Timestamp).Take(limit).ToListAsync();
    return Results.Ok(alerts);
});

app.Run();

static DateTime? ParseIntervalToUtcStart(string interval)
{
    interval = interval.Trim().ToLowerInvariant();
    try
    {
        if (interval.EndsWith("h"))
        {
            var n = int.Parse(interval[..^1]);
            return DateTime.UtcNow.AddHours(-n);
        }
        if (interval.EndsWith("m"))
        {
            var n = int.Parse(interval[..^1]);
            return DateTime.UtcNow.AddMinutes(-n);
        }
        // fallback: treat as hours
        if (int.TryParse(interval, out var h))
            return DateTime.UtcNow.AddHours(-h);
    }
    catch { }
    return null;
}