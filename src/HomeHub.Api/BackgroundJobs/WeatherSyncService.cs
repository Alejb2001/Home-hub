using System.Text.Json;
using System.Text.Json.Serialization;
using HomeHub.Api.Data;
using HomeHub.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace HomeHub.Api.BackgroundJobs;

public class WeatherSyncService(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<WeatherSyncService> logger) : BackgroundService
{
    private static readonly TimeSpan SyncInterval = TimeSpan.FromHours(1);
    private readonly HttpClient _httpClient = new();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("WeatherSyncService starting. Sync interval: {Interval}", SyncInterval);

        await Task.Delay(TimeSpan.FromSeconds(45), stoppingToken);

        using var timer = new PeriodicTimer(SyncInterval);
        do
        {
            await SyncAllAsync(stoppingToken);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    internal async Task SyncAllAsync(CancellationToken ct)
    {
        List<long> serviceIds;
        using (var scope = scopeFactory.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            serviceIds = await db.ServiceConfigs
                .Where(s => s.Type == "weather" && s.Enabled)
                .Select(s => s.Id)
                .ToListAsync(ct);
        }

        foreach (var id in serviceIds)
            await SyncOneAsync(id, ct);
    }

    private async Task SyncOneAsync(long serviceConfigId, CancellationToken ct)
    {
        logger.LogInformation("Weather sync starting for service {Id}", serviceConfigId);

        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();

        var svc = await db.ServiceConfigs.FirstOrDefaultAsync(s => s.Id == serviceConfigId, ct);
        if (svc is null) return;

        try
        {
            var apiKey = configuration["OPENWEATHERMAP_API_KEY"];
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                logger.LogWarning("OPENWEATHERMAP_API_KEY not configured. Skipping weather sync for service {Id}.", serviceConfigId);
                return;
            }

            WeatherCredentials creds;
            try
            {
                creds = JsonSerializer.Deserialize<WeatherCredentials>(svc.CredentialsJson)
                    ?? throw new InvalidOperationException("Invalid weather credentials JSON.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to parse weather credentials for service {Id}", serviceConfigId);
                return;
            }

            var client = new WeatherClient(_httpClient, apiKey, creds.Lat, creds.Lon,
                loggerFactory.CreateLogger<WeatherClient>());

            var reading = await client.GetCurrentAsync(serviceConfigId, ct);

            db.WeatherReadings.Add(reading);
            await db.SaveChangesAsync(ct);

            logger.LogInformation(
                "Weather sync complete for service {Id}. Temp: {Temp}C, Clouds: {Clouds}%",
                serviceConfigId, reading.TempC, reading.CloudsPct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Weather sync failed for service {Id}", serviceConfigId);
        }
    }
}

file record WeatherCredentials(
    [property: JsonPropertyName("lat")] string Lat,
    [property: JsonPropertyName("lon")] string Lon
);
