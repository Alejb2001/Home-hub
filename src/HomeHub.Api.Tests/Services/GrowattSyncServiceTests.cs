using System.Text.Json;
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

    private static IServiceScopeFactory CreateScopeFactory(AppDbContext db)
    {
        var services = new ServiceCollection();
        services.AddSingleton(db);
        services.AddSingleton<AppDbContext>(db);
        var provider = services.BuildServiceProvider();

        var scopeFactoryMock = new Mock<IServiceScopeFactory>();
        var scopeMock = new Mock<IServiceScope>();
        scopeMock.Setup(s => s.ServiceProvider).Returns(provider);
        scopeFactoryMock.Setup(f => f.CreateScope()).Returns(scopeMock.Object);
        return scopeFactoryMock.Object;
    }

    private static async Task<ServiceConfig> SeedServiceConfigAsync(AppDbContext db)
    {
        var user = new User { Email = "test@test.com", PasswordHash = "hash" };
        var home = new Home { Name = "Test", User = user };
        db.Users.Add(user);
        db.Homes.Add(home);
        await db.SaveChangesAsync();

        var creds = JsonSerializer.Serialize(new { username = "u", password = "p" });
        var svc = new ServiceConfig
        {
            HomeId = home.Id,
            Type = "growatt",
            DisplayName = "Test",
            CredentialsJson = creds,
            Enabled = true
        };
        db.ServiceConfigs.Add(svc);
        await db.SaveChangesAsync();
        return svc;
    }

    [Fact]
    public async Task SyncAll_WithGrowattService_SavesReadingAndSuccessLog()
    {
        var db = CreateInMemoryDb();
        await SeedServiceConfigAsync(db);

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
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(reading);

        var factory = new GrowattClientFactory((_, _) => clientMock.Object);
        var equipmentStore = new EquipmentStore();
        var scopeFactory = CreateScopeFactory(db);
        var service = new GrowattSyncService(
            scopeFactory, equipmentStore, factory,
            NullLogger<GrowattSyncService>.Instance);

        await service.SyncAllAsync(CancellationToken.None);

        Assert.Equal(1, await db.SolarReadings.CountAsync());
        var log = await db.SyncLogs.FirstAsync();
        Assert.True(log.Success);
        Assert.Equal(1, log.RecordsSaved);
    }

    [Fact]
    public async Task SyncAll_ClientThrows_SavesErrorLog_DoesNotRethrow()
    {
        var db = CreateInMemoryDb();
        await SeedServiceConfigAsync(db);

        var clientMock = new Mock<IGrowattClient>();
        clientMock.Setup(c => c.GetPlantInfoAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("connection refused"));

        var factory = new GrowattClientFactory((_, _) => clientMock.Object);
        var equipmentStore = new EquipmentStore();
        var scopeFactory = CreateScopeFactory(db);
        var service = new GrowattSyncService(
            scopeFactory, equipmentStore, factory,
            NullLogger<GrowattSyncService>.Instance);

        // Should not throw
        await service.SyncAllAsync(CancellationToken.None);

        Assert.Equal(0, await db.SolarReadings.CountAsync());
        var log = await db.SyncLogs.FirstAsync();
        Assert.False(log.Success);
        Assert.Contains("connection refused", log.ErrorMessage);
    }

    [Fact]
    public async Task SyncAll_NoServices_DoesNothing()
    {
        var db = CreateInMemoryDb();
        var clientMock = new Mock<IGrowattClient>();
        var factory = new GrowattClientFactory((_, _) => clientMock.Object);
        var equipmentStore = new EquipmentStore();
        var scopeFactory = CreateScopeFactory(db);
        var service = new GrowattSyncService(
            scopeFactory, equipmentStore, factory,
            NullLogger<GrowattSyncService>.Instance);

        await service.SyncAllAsync(CancellationToken.None);

        Assert.Equal(0, await db.SolarReadings.CountAsync());
        Assert.Equal(0, await db.SyncLogs.CountAsync());
        clientMock.Verify(c => c.GetPlantInfoAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
