using System.Text;
using System.Web;
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
    options.UseNpgsql(ConvertConnectionString(connectionString));
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

// Application services — Growatt needs a persistent cookie jar (session via JSESSIONID)
builder.Services.AddHttpClient<IGrowattClient, GrowattClient>()
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        CookieContainer = new System.Net.CookieContainer(),
        UseCookies = true,
        AllowAutoRedirect = true,
    })
    .ConfigureHttpClient(client =>
    {
        client.DefaultRequestHeaders.Add("User-Agent",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
        client.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");
    });
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

// Converts postgresql:// URI to Npgsql key=value format.
// Npgsql 8+ dropped URI support; PaaS platforms (Render, Railway, Heroku) emit URIs.
static string ConvertConnectionString(string cs)
{
    if (!cs.StartsWith("postgresql://") && !cs.StartsWith("postgres://"))
        return cs;

    var uri = new Uri(cs);
    var userInfo = uri.UserInfo.Split(':', 2);
    var username = Uri.UnescapeDataString(userInfo[0]);
    var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : "";
    var host = uri.Host;
    var port = uri.IsDefaultPort ? 5432 : uri.Port;
    var database = uri.AbsolutePath.TrimStart('/');

    var query = HttpUtility.ParseQueryString(uri.Query);
    var sslMode = query["sslmode"] switch
    {
        "require" => "Require",
        "disable" => "Disable",
        "prefer"  => "Prefer",
        "verify-full" => "VerifyFull",
        _ => "Require"
    };

    return $"Host={host};Port={port};Database={database};Username={username};Password={password};SSL Mode={sslMode}";
}

app.Run();

// Make Program class accessible for integration tests
public partial class Program { }
