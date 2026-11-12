using System.Text.Json;
using System.Text.Json.Serialization;
using HomeHub.Api.Data;
using HomeHub.Api.Models;
using HomeHub.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace HomeHub.Api.BackgroundJobs;

public class GrowattSyncService(
    IServiceScopeFactory scopeFactory,
    EquipmentStore equipmentStore,
    GrowattClientFactory growattFactory,
    ILogger<GrowattSyncService> logger) : BackgroundService
{
    private static readonly TimeSpan SyncInterval = TimeSpan.FromHours(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("GrowattSyncService starting. Sync interval: {Interval}", SyncInterval);

        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

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
                .Where(s => s.Type == "growatt" && s.Enabled)
                .Select(s => s.Id)
                .ToListAsync(ct);
        }

        foreach (var id in serviceIds)
            await SyncOneAsync(id, ct);
    }

    private async Task SyncOneAsync(long serviceConfigId, CancellationToken ct)
    {
        var syncedAt = DateTimeOffset.UtcNow;
        logger.LogInformation("Growatt sync starting for service {Id} at {Time}", serviceConfigId, syncedAt);

        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var svc = await db.ServiceConfigs.FirstOrDefaultAsync(s => s.Id == serviceConfigId, ct);
        if (svc is null) return;

        try
        {
            GrowattCredentials creds;
            try
            {
                creds = JsonSerializer.Deserialize<GrowattCredentials>(svc.CredentialsJson)
                    ?? throw new InvalidOperationException("Invalid credentials JSON.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to parse credentials for service {Id}", serviceConfigId);
                return;
            }

            var client = growattFactory.GetOrCreate(serviceConfigId, creds.Username, creds.Password);
            var plantInfo = await client.GetPlantInfoAsync(ct);

            if (svc.PlantId != plantInfo.PlantId)
                svc.PlantId = plantInfo.PlantId;

            var reading = await client.GetCurrentReadingAsync(plantInfo.PlantId, plantInfo.InverterSn, ct);

            equipmentStore.Update(serviceConfigId, new EquipmentSnapshot(
                reading.DeviceModel ?? "",
                reading.NominalPowerW ?? 0,
                reading.PlantName ?? "",
                reading.EnergyMonthKwh ?? 0
            ));

            db.SolarReadings.Add(reading);
            db.SyncLogs.Add(new SyncLog
            {
                SyncedAt = syncedAt,
                Success = true,
                RecordsSaved = 1
            });

            await db.SaveChangesAsync(ct);
            logger.LogInformation(
                "Growatt sync complete for service {Id}. Power: {Power}W, Today: {Today}kWh",
                serviceConfigId, reading.PowerW, reading.EnergyTodayKwh);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Growatt sync failed for service {Id}", serviceConfigId);
            try
            {
                db.SyncLogs.Add(new SyncLog
                {
                    SyncedAt = syncedAt,
                    Success = false,
                    RecordsSaved = 0,
                    ErrorMessage = ex.Message
                });
                await db.SaveChangesAsync(CancellationToken.None);
            }
            catch (Exception logEx)
            {
                logger.LogError(logEx, "Failed to write error to sync_log for service {Id}", serviceConfigId);
            }
        }
    }
}

file record GrowattCredentials(
    [property: JsonPropertyName("username")] string Username,
    [property: JsonPropertyName("password")] string Password
);
