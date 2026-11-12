using System.Net;
using System.Net.Http.Json;
using HomeHub.Api.Controllers;
using HomeHub.Api.Data;
using HomeHub.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
                    ["JWT_SECRET"] = "super-secret-key-for-testing-purposes-only-32chars",
                });
            });
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (descriptor != null) services.Remove(descriptor);

                services.AddDbContext<AppDbContext>(options =>
                    options.UseInMemoryDatabase("AuthTests"));

                // Seed a known test user
                var sp = services.BuildServiceProvider();
                using var scope = sp.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                if (!db.Users.Any())
                {
                    var hasher = new PasswordHasher<User>();
                    var user = new User { Email = "test@test.com", PasswordHash = "" };
                    user.PasswordHash = hasher.HashPassword(user, "testpass");
                    db.Users.Add(user);
                    db.Homes.Add(new Home { Name = "Test Home", User = user });
                    db.SaveChanges();
                }
            });
        }).CreateClient();
    }

    [Fact]
    public async Task Login_WithCorrectCredentials_ReturnsTokenAndOk()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest("test@test.com", "testpass"));

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
            new LoginRequest("test@test.com", "wrongpass"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithWrongUsername_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest("wrong@test.com", "testpass"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Register_WithNewEmail_ReturnsOk()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest("newuser@test.com", "newpassword123"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_Returns409()
    {
        // Register once
        await _client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest("dup@test.com", "password123"));

        // Register again with same email
        var response = await _client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest("dup@test.com", "password456"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithoutToken_Returns401()
    {
        var response = await _client.GetAsync("/api/solar/summary");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
