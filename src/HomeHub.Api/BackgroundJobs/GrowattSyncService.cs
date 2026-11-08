using HomeHub.Api.Data;
using HomeHub.Api.Models;
using HomeHub.Api.Services;

namespace HomeHub.Api.BackgroundJobs;

public class GrowattSyncService(
    IServiceScopeFactory scopeFactory,
    ILogger<GrowattSyncService> logger) : BackgroundService
{
    private static readonly TimeSpan SyncInterval = TimeSpan.FromHours(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("GrowattSyncService starting. Sync interval: {Interval}", SyncInterval);

        // Initial sync shortly after startup (30 seconds delay)
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        using var timer = new PeriodicTimer(SyncInterval);

        do
        {
            await SyncOnceAsync(stoppingToken);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    internal async Task SyncOnceAsync(CancellationToken ct)
    {
        var syncedAt = DateTimeOffset.UtcNow;
        logger.LogInformation("Growatt sync starting at {Time}", syncedAt);

        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var growattClient = scope.ServiceProvider.GetRequiredService<IGrowattClient>();

        try
        {
            var plantInfo = await growattClient.GetPlantInfoAsync(ct);
            var reading = await growattClient.GetCurrentReadingAsync(
                plantInfo.PlantId, plantInfo.InverterSn, ct);

            db.SolarReadings.Add(reading);

            db.SyncLogs.Add(new SyncLog
            {
                SyncedAt = syncedAt,
                Success = true,
                RecordsSaved = 1
            });

            await db.SaveChangesAsync(ct);
            logger.LogInformation(
                "Growatt sync complete. Power: {Power}W, Today: {Today}kWh",
                reading.PowerW, reading.EnergyTodayKwh);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Growatt sync failed");

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
                logger.LogError(logEx, "Failed to write error to sync_log");
            }
        }
    }
}
