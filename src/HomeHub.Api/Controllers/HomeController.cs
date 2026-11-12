using System.Security.Claims;
using System.Text.Json;
using HomeHub.Api.Data;
using HomeHub.Api.Models;
using HomeHub.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HomeHub.Api.Controllers;

[ApiController]
[Route("api/home")]
[Authorize]
public class HomeController(AppDbContext db, GrowattClientFactory growattFactory) : ControllerBase
{
    private long GetUserId() =>
        long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

    [HttpGet]
    public async Task<IActionResult> GetHome(CancellationToken ct)
    {
        var userId = GetUserId();
        var home = await db.Homes
            .Include(h => h.Services)
            .FirstOrDefaultAsync(h => h.UserId == userId, ct);

        if (home is null)
            return Ok(new { name = "Mi Hogar", services = Array.Empty<object>() });

        return Ok(MapHome(home));
    }

    [HttpPut("name")]
    public async Task<IActionResult> UpdateName([FromBody] UpdateNameRequest request, CancellationToken ct)
    {
        var userId = GetUserId();
        var home = await db.Homes.FirstOrDefaultAsync(h => h.UserId == userId, ct);
        if (home is null) return NotFound();

        home.Name = request.Name.Trim();
        await db.SaveChangesAsync(ct);
        return Ok(new { name = home.Name });
    }

    [HttpPost("services")]
    public async Task<IActionResult> AddService([FromBody] ServiceRequest request, CancellationToken ct)
    {
        var userId = GetUserId();
        var home = await db.Homes.FirstOrDefaultAsync(h => h.UserId == userId, ct);
        if (home is null) return NotFound();

        var credJson = JsonSerializer.Serialize(new { username = request.Username, password = request.Password });

        var svc = new ServiceConfig
        {
            HomeId = home.Id,
            Type = request.Type,
            DisplayName = request.DisplayName,
            CredentialsJson = credJson,
            Enabled = true
        };
        db.ServiceConfigs.Add(svc);
        await db.SaveChangesAsync(ct);

        return Ok(MapService(svc));
    }

    [HttpPut("services/{id:long}")]
    public async Task<IActionResult> UpdateService(long id, [FromBody] ServiceRequest request, CancellationToken ct)
    {
        var userId = GetUserId();
        var svc = await db.ServiceConfigs
            .Include(s => s.Home)
            .FirstOrDefaultAsync(s => s.Id == id && s.Home.UserId == userId, ct);
        if (svc is null) return NotFound();

        svc.DisplayName = request.DisplayName;

        if (!string.IsNullOrEmpty(request.Password))
        {
            svc.CredentialsJson = JsonSerializer.Serialize(
                new { username = request.Username, password = request.Password });
            svc.PlantId = null;
            growattFactory.Invalidate(svc.Id);
        }
        else if (!string.IsNullOrEmpty(request.Username))
        {
            using var existingDoc = JsonDocument.Parse(svc.CredentialsJson);
            var existingPwd = existingDoc.RootElement.GetProperty("password").GetString() ?? "";
            svc.CredentialsJson = JsonSerializer.Serialize(
                new { username = request.Username, password = existingPwd });
            growattFactory.Invalidate(svc.Id);
        }

        await db.SaveChangesAsync(ct);
        return Ok(MapService(svc));
    }

    [HttpPut("services/{id:long}/toggle")]
    public async Task<IActionResult> ToggleService(long id, CancellationToken ct)
    {
        var userId = GetUserId();
        var svc = await db.ServiceConfigs
            .Include(s => s.Home)
            .FirstOrDefaultAsync(s => s.Id == id && s.Home.UserId == userId, ct);
        if (svc is null) return NotFound();

        svc.Enabled = !svc.Enabled;
        await db.SaveChangesAsync(ct);
        return Ok(MapService(svc));
    }

    [HttpDelete("services/{id:long}")]
    public async Task<IActionResult> DeleteService(long id, CancellationToken ct)
    {
        var userId = GetUserId();
        var svc = await db.ServiceConfigs
            .Include(s => s.Home)
            .FirstOrDefaultAsync(s => s.Id == id && s.Home.UserId == userId, ct);
        if (svc is null) return NotFound();

        growattFactory.Invalidate(svc.Id);
        db.ServiceConfigs.Remove(svc);
        await db.SaveChangesAsync(ct);
        return Ok(new { });
    }

    private static object MapHome(Home home) => new
    {
        id = home.Id,
        name = home.Name,
        services = home.Services.Select(MapService).ToArray()
    };

    private static object MapService(ServiceConfig svc)
    {
        string credUsername = "";
        try
        {
            using var doc = JsonDocument.Parse(svc.CredentialsJson);
            credUsername = doc.RootElement.GetProperty("username").GetString() ?? "";
        }
        catch { }

        return new
        {
            id = svc.Id,
            type = svc.Type,
            displayName = svc.DisplayName,
            plantId = svc.PlantId,
            enabled = svc.Enabled,
            credentialsUsername = credUsername
        };
    }
}

public record UpdateNameRequest(string Name);
public record ServiceRequest(string Type, string DisplayName, string Username, string Password);
