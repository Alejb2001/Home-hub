using System.Text;
using HomeHub.Api.BackgroundJobs;
using HomeHub.Api.Data;
using HomeHub.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Database — read config inside lambda so test overrides are applied first
builder.Services.AddDbContext<AppDbContext>((sp, options) =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var connectionString = config["DATABASE_URL"]
        ?? throw new InvalidOperationException("DATABASE_URL is not configured.");
    options.UseNpgsql(connectionString);
});

// Auth — read JWT_SECRET inside options callback
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwtSecret = builder.Configuration["JWT_SECRET"]
            ?? "default-dev-secret-change-in-production-32chars";

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateIssuer = false,
            ValidateAudience = false,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// Application services
builder.Services.AddHttpClient<IGrowattClient, GrowattClient>();
builder.Services.AddScoped<ISolarService, SolarService>();
builder.Services.AddHostedService<GrowattSyncService>();

builder.Services.AddControllers();

var app = builder.Build();

// Apply pending migrations on startup (skip in test environment)
if (!app.Environment.IsEnvironment("Testing"))
{
    try
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.Migrate();
    }
    catch (Exception ex)
    {
        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Database migration failed on startup");
    }
}

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// SPA fallback: any unmatched route serves index.html
app.MapFallbackToFile("index.html");

app.Run();

// Make Program class accessible for integration tests
public partial class Program { }
