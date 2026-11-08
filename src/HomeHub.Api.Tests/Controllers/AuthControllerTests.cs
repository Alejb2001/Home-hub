using System.Net;
using System.Net.Http.Json;
using HomeHub.Api.Controllers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using HomeHub.Api.Data;
using Xunit;

namespace HomeHub.Api.Tests.Controllers;

public class AuthControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public AuthControllerTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("environment", "Testing");
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ADMIN_USER"] = "testuser",
                    ["ADMIN_PASSWORD"] = "testpass",
                    ["JWT_SECRET"] = "super-secret-key-for-testing-purposes-only-32chars",
                    ["GROWATT_USER"] = "test@test.com",
                    ["GROWATT_PASSWORD"] = "password"
                });
            });
            builder.ConfigureServices(services =>
            {
                // Replace real DB with in-memory for integration tests
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (descriptor != null) services.Remove(descriptor);

                services.AddDbContext<AppDbContext>(options =>
                    options.UseInMemoryDatabase("AuthTests"));
            });
        }).CreateClient();
    }

    [Fact]
    public async Task Login_WithCorrectCredentials_ReturnsTokenAndOk()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest("testuser", "testpass"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        Assert.NotNull(body);
        Assert.True(body.ContainsKey("token"));
        Assert.False(string.IsNullOrEmpty(body["token"]));
    }

    [Fact]
    public async Task Login_WithWrongPassword_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest("testuser", "wrongpass"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithWrongUsername_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest("wronguser", "testpass"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithoutToken_Returns401()
    {
        var response = await _client.GetAsync("/api/solar/summary");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
