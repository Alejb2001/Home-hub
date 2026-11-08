using HomeHub.Api.BackgroundJobs;
using HomeHub.Api.Data;
using HomeHub.Api.Models;
using HomeHub.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace HomeHub.Api.Tests.Services;

public class GrowattSyncServiceTests
{
    private static AppDbContext CreateInMemoryDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static IServiceScopeFactory CreateScopeFactory(AppDbContext db, IGrowattClient client)
    {
        var services = new ServiceCollection();
        services.AddSingleton(db);
        services.AddSingleton(client);
        var provider = services.BuildServiceProvider();

        var scopeFactoryMock = new Mock<IServiceScopeFactory>();
        var scopeMock = new Mock<IServiceScope>();
        scopeMock.Setup(s => s.ServiceProvider).Returns(provider);
        scopeFactoryMock.Setup(f => f.CreateScope()).Returns(scopeMock.Object);
        return scopeFactoryMock.Object;
    }

    [Fact]
    public async Task SyncOnce_Success_SavesReadingAndSuccessLog()
    {
        var db = CreateInMemoryDb();
        var plantInfo = new GrowattPlantInfo("plant1", "My Plant", "INV001");
        var reading = new SolarReading
        {
            PlantId = "plant1",
            InverterSn = "INV001",
            TimestampUtc = DateTimeOffset.UtcNow,
            PowerW = 1500,
            EnergyTodayKwh = 10.5m,
            EnergyTotalKwh = 1234.5m,
            Status = 1
        };

        var clientMock = new Mock<IGrowattClient>();
        clientMock.Setup(c => c.GetPlantInfoAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(plantInfo);
        clientMock.Setup(c => c.GetCurrentReadingAsync(
                plantInfo.PlantId, plantInfo.InverterSn, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reading);

        var scopeFactory = CreateScopeFactory(db, clientMock.Object);
        var service = new GrowattSyncService(scopeFactory, NullLogger<GrowattSyncService>.Instance);

        await service.SyncOnceAsync(CancellationToken.None);

        Assert.Equal(1, await db.SolarReadings.CountAsync());
        var log = await db.SyncLogs.FirstAsync();
        Assert.True(log.Success);
        Assert.Equal(1, log.RecordsSaved);
    }

    [Fact]
    public async Task SyncOnce_ClientThrows_SavesErrorLog_DoesNotRethrow()
    {
        var db = CreateInMemoryDb();
        var clientMock = new Mock<IGrowattClient>();
        clientMock.Setup(c => c.GetPlantInfoAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("connection refused"));

        var scopeFactory = CreateScopeFactory(db, clientMock.Object);
        var service = new GrowattSyncService(scopeFactory, NullLogger<GrowattSyncService>.Instance);

        // Should not throw
        await service.SyncOnceAsync(CancellationToken.None);

        Assert.Equal(0, await db.SolarReadings.CountAsync());
        var log = await db.SyncLogs.FirstAsync();
        Assert.False(log.Success);
        Assert.Contains("connection refused", log.ErrorMessage);
    }
}
